using potetofly25.KeyManager2.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace potetofly25.KeyManager2.Services
{
    public static class ExportImportService
    {
        // Export all to path.
        // If AdvancedEncryptionService.IsMasterSet, wrap fileKey with session key;
        // otherwise require exportPassword.
        public static void ExportAll(string path, string? exportPassword = null)
        {
            var fileKey = new byte[32];
            RandomNumberGenerator.Fill(fileKey);
            byte[] wrapped;
            if (AdvancedEncryptionService.IsMasterSet)
            {
                var b64 = Convert.ToBase64String(fileKey);
                var wrappedStr = AdvancedEncryptionService.EncryptString(b64);
                wrapped = Encoding.UTF8.GetBytes(wrappedStr);
            }
            else
            {
                if (string.IsNullOrEmpty(exportPassword)) throw new InvalidOperationException("Export password required");
                // wrap with user password
                var salt = RandomNumberGenerator.GetBytes(16);
                using var derive = new Rfc2898DeriveBytes(exportPassword, salt, 200_000, HashAlgorithmName.SHA256);
                var key = derive.GetBytes(32);
                var iv = RandomNumberGenerator.GetBytes(12);
                var tag = new byte[16];
                byte[] cipher;
                using (var aes = new AesGcm(key, 16))
                {
                    cipher = new byte[fileKey.Length];
                    aes.Encrypt(iv, fileKey, cipher, tag);
                }
                var wrappedBuf = new byte[salt.Length + iv.Length + cipher.Length + tag.Length];
                Buffer.BlockCopy(salt, 0, wrappedBuf, 0, salt.Length);
                Buffer.BlockCopy(iv, 0, wrappedBuf, salt.Length, iv.Length);
                Buffer.BlockCopy(cipher, 0, wrappedBuf, salt.Length + iv.Length, cipher.Length);
                Buffer.BlockCopy(tag, 0, wrappedBuf, salt.Length + iv.Length + cipher.Length, tag.Length);
                wrapped = wrappedBuf;
            }

            var pkg = new ExportPackage { WrappedFileKeyBase64 = Convert.ToBase64String(wrapped) };
            var svc = new CredentialService();
            var records = svc.GetAll(tryDecrypt: AdvancedEncryptionService.IsMasterSet);
            foreach (var r in records)
            {
                var plain = Encoding.UTF8.GetBytes(r.Password ?? string.Empty);
                var iv = RandomNumberGenerator.GetBytes(12);
                var tag = new byte[16];
                byte[] cipher;
                using (var aes = new AesGcm(fileKey, 16))
                {
                    cipher = new byte[plain.Length];
                    aes.Encrypt(iv, plain, cipher, tag);
                }
                var payload = new byte[iv.Length + cipher.Length + tag.Length];
                Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
                Buffer.BlockCopy(cipher, 0, payload, iv.Length, cipher.Length);
                Buffer.BlockCopy(tag, 0, payload, iv.Length + cipher.Length, tag.Length);
                pkg.Records.Add(new Record
                {
                    Id = r.Id,
                    LoginId = r.LoginId,
                    Password = Convert.ToBase64String(payload),
                    Description = r.Description,
                    Category = r.Category,
                    Tags = r.Tags,
                    IsEncrypted = r.IsEncrypted
                });
            }
            File.WriteAllText(path, JsonSerializer.Serialize(pkg, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        // Import
        public static void ImportAll(string path, string? importPassword = null)
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var pkg = JsonSerializer.Deserialize<ExportPackage>(json) ?? throw new InvalidOperationException("Invalid package");
            var wrapped = Convert.FromBase64String(pkg.WrappedFileKeyBase64);
            byte[] fileKey;
            if (AdvancedEncryptionService.IsMasterSet)
            {
                var wrappedStr = Encoding.UTF8.GetString(wrapped);
                var b64 = AdvancedEncryptionService.DecryptString(wrappedStr);
                fileKey = Convert.FromBase64String(b64);
            }
            else
            {
                if (string.IsNullOrEmpty(importPassword)) throw new InvalidOperationException("Import password required");
                var salt = wrapped.Take(16).ToArray();
                var iv = wrapped.Skip(16).Take(12).ToArray();
                var tag = wrapped.Skip(wrapped.Length - 16).Take(16).ToArray();
                var cipher = wrapped.Skip(16 + iv.Length).Take(wrapped.Length - 16 - iv.Length - tag.Length).ToArray();
                using var derive = new Rfc2898DeriveBytes(importPassword, salt, 200_000, HashAlgorithmName.SHA256);
                var key = derive.GetBytes(32);
                fileKey = new byte[cipher.Length];
                using (var aes = new AesGcm(key, 16))
                {
                    aes.Decrypt(iv, cipher, tag, fileKey);
                }
            }

            var svc = new CredentialService();
            foreach (var rec in pkg.Records)
            {
                var payload = Convert.FromBase64String(rec.Password);
                var iv = payload.Take(12).ToArray();
                var tag = payload.Skip(payload.Length - 16).Take(16).ToArray();
                var cipher = payload.Skip(12).Take(payload.Length - 12 - 16).ToArray();
                var plain = new byte[cipher.Length];
                using (var aes = new AesGcm(fileKey, 16))
                {
                    aes.Decrypt(iv, cipher, tag, plain);
                }
                var pwd = Encoding.UTF8.GetString(plain);
                var c = new Models.Credential
                {
                    LoginId = rec.LoginId,
                    Password = pwd,
                    Description = rec.Description,
                    Category = rec.Category,
                    Tags = rec.Tags,
                    IsEncrypted = rec.IsEncrypted
                };
                svc.Add(c, encryptPassword: false);
            }
        }

    }
}

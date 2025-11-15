using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace potetofly25.KeyManager2.Services
{
    public static class AdvancedEncryptionService
    {
        private const int KeyBytes = 32;
        private const int SaltBytes = 32;
        private const int Iterations = 200_000;
        private const int GcmTagLength = 16;
        private static readonly string SaltFile = Path.Combine(Directory.GetCurrentDirectory(), "KeyManager2_adv.salt");
        private static readonly string WrappedRootKeyFile = Path.Combine(Directory.GetCurrentDirectory(), "KeyManager2_root.wrapped");
        private static byte[]? _rootKey;

        public static bool IsMasterSet => _rootKey != null;

        private static byte[] EnsureSalt()
        {
            if (File.Exists(SaltFile)) return File.ReadAllBytes(SaltFile);
            var s = new byte[SaltBytes];
            RandomNumberGenerator.Fill(s);
            File.WriteAllBytes(SaltFile, s);
            return s;
        }

        public static void InitializeMasterPassword(string masterPassword)
        {
            var root = new byte[KeyBytes];
            RandomNumberGenerator.Fill(root);
            var wrapped = WrapRootKeyWithPassword(root, masterPassword);
            // try platform protection (Windows)
            try
            {
#if NET7_0_OR_GREATER
                if (OperatingSystem.IsWindows())
                {
                    var protectedBytes = ProtectedData.Protect(wrapped, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(WrappedRootKeyFile, protectedBytes);
                    _rootKey = root;
                    return;
                }
#endif
            }
            catch { }
            File.WriteAllBytes(WrappedRootKeyFile, wrapped);
            _rootKey = root;
        }

        public static void SetMasterPassword(string masterPassword)
        {
            if (!File.Exists(WrappedRootKeyFile)) throw new InvalidOperationException("No wrapped root key stored.");
            var wrapped = File.ReadAllBytes(WrappedRootKeyFile);
            // try unprotect
            try
            {
#if NET7_0_OR_GREATER
                if (OperatingSystem.IsWindows())
                {
                    var unprotected = ProtectedData.Unprotect(wrapped, null, DataProtectionScope.CurrentUser);
                    wrapped = unprotected;
                }
#endif
            }
            catch { }
            var root = UnwrapRootKeyWithPassword(wrapped, masterPassword);
            _rootKey = root;
        }

        public static void ClearMasterPassword()
        {
            if (_rootKey != null) { Array.Clear(_rootKey, 0, _rootKey.Length); _rootKey = null; }
        }

        private static byte[] WrapRootKeyWithPassword(byte[] rootKey, string password)
        {
            var salt = EnsureSalt();
            using var derive = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeyBytes);
            var iv = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[rootKey.Length];
            var tag = new byte[GcmTagLength];
            using (var aes = new AesGcm(key, GcmTagLength))
            {
                aes.Encrypt(iv, rootKey, cipher, tag);
            }
            var wrapped = new byte[salt.Length + iv.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(salt, 0, wrapped, 0, salt.Length);
            Buffer.BlockCopy(iv, 0, wrapped, salt.Length, iv.Length);
            Buffer.BlockCopy(cipher, 0, wrapped, salt.Length + iv.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, wrapped, salt.Length + iv.Length + cipher.Length, tag.Length);
            return wrapped;
        }

        private static byte[] UnwrapRootKeyWithPassword(byte[] wrapped, string password)
        {
            var salt = wrapped.Take(SaltBytes).ToArray();
            var iv = wrapped.Skip(SaltBytes).Take(12).ToArray();
            var tag = wrapped.Skip(wrapped.Length - GcmTagLength).Take(GcmTagLength).ToArray();
            var cipher = wrapped.Skip(SaltBytes + iv.Length).Take(wrapped.Length - SaltBytes - iv.Length - tag.Length).ToArray();
            using var derive = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeyBytes);
            var root = new byte[cipher.Length];
            using (var aes = new AesGcm(key, GcmTagLength))
            {
                aes.Decrypt(iv, cipher, tag, root);
            }
            return root;
        }

        private static (byte[] encKey, byte[] hmacKey) DeriveSubKeys(byte[] root)
        {
            byte[] Derive(byte[] info)
            {
                using var hmac = new HMACSHA256(root);
                return hmac.ComputeHash(info).Take(KeyBytes).ToArray();
            }
            var encKey = Derive(Encoding.UTF8.GetBytes("enc"));
            var hmacKey = Derive(Encoding.UTF8.GetBytes("hmac"));
            return (encKey, hmacKey);
        }

        public static string EncryptString(string plain)
        {
            if (_rootKey == null) throw new InvalidOperationException("Master not set.");
            var (encKey, hmacKey) = DeriveSubKeys(_rootKey);
            var plainBytes = Encoding.UTF8.GetBytes(plain ?? string.Empty);
            var iv = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[GcmTagLength];
            using (var aes = new AesGcm(encKey, GcmTagLength))
            {
                aes.Encrypt(iv, plainBytes, cipher, tag);
            }
            var payload = new byte[iv.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, payload, iv.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, payload, iv.Length + cipher.Length, tag.Length);
            byte[] hmac;
            using (var mac = new HMACSHA256(hmacKey)) hmac = mac.ComputeHash(payload);
            var full = new byte[payload.Length + hmac.Length];
            Buffer.BlockCopy(payload, 0, full, 0, payload.Length);
            Buffer.BlockCopy(hmac, 0, full, payload.Length, hmac.Length);
            return Convert.ToBase64String(full);
        }

        public static string DecryptString(string b64)
        {
            if (_rootKey == null) throw new InvalidOperationException("Master not set.");
            var (encKey, hmacKey) = DeriveSubKeys(_rootKey);
            var full = Convert.FromBase64String(b64);
            if (full.Length < 12 + GcmTagLength + 32) throw new CryptographicException("Invalid payload");
            var hmac = full.Skip(full.Length - 32).Take(32).ToArray();
            var payload = full.Take(full.Length - 32).ToArray();
            using (var mac = new HMACSHA256(hmacKey))
            {
                var expected = mac.ComputeHash(payload);
                if (!CryptographicOperations.FixedTimeEquals(expected, hmac))
                    throw new CryptographicException("HMAC mismatch");
            }
            var iv = payload.Take(12).ToArray();
            var tag = payload.Skip(payload.Length - GcmTagLength).Take(GcmTagLength).ToArray();
            var cipher = payload.Skip(12).Take(payload.Length - 12 - GcmTagLength).ToArray();
            var plain = new byte[cipher.Length];
            using (var aes = new AesGcm(encKey, GcmTagLength))
            {
                aes.Decrypt(iv, cipher, tag, plain);
            }
            return Encoding.UTF8.GetString(plain);
        }

    }
}

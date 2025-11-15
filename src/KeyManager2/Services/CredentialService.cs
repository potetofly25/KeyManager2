using Microsoft.EntityFrameworkCore;
using potetofly25.KeyManager2.Data;
using potetofly25.KeyManager2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace potetofly25.KeyManager2.Services
{
    public class CredentialService
    {
        private readonly KeyManagerDbContext _db = new();

        public CredentialService()
        {
            _db.Database.EnsureCreated();
        }

        public List<Credential> GetAll(bool tryDecrypt = true)
        {
            var list = _db.Credentials.AsNoTracking().OrderBy(c => c.Id).ToList();
            if (tryDecrypt && AdvancedEncryptionService.IsMasterSet)
            {
                foreach (var c in list)
                {
                    if (c.IsEncrypted)
                    {
                        try { c.Password = AdvancedEncryptionService.DecryptString(c.Password); }
                        catch { }
                    }
                }
            }
            return list;
        }

        public void Add(Credential c, bool encryptPassword = false)
        {
            if (encryptPassword)
            {
                if (!AdvancedEncryptionService.IsMasterSet) throw new InvalidOperationException("Master not set");
                c.Password = AdvancedEncryptionService.EncryptString(c.Password);
                c.IsEncrypted = true;
            }
            _db.Credentials.Add(c);
            _db.SaveChanges();
        }

        public void Update(Credential c, bool encryptPassword = false)
        {
            if (encryptPassword)
            {
                if (!AdvancedEncryptionService.IsMasterSet) throw new InvalidOperationException("Master not set");
                c.Password = AdvancedEncryptionService.EncryptString(c.Password);
                c.IsEncrypted = true;
            }
            else
            {
                c.IsEncrypted = false;
            }
            _db.Credentials.Update(c);
            _db.SaveChanges();
        }

        public void Delete(Credential c)
        {
            _db.Credentials.Remove(c);
            _db.SaveChanges();
        }
    }
}

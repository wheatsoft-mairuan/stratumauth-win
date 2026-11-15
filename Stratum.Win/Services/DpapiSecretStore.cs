// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Stores the SQLCipher database key protected with the Windows DPAPI
    /// (current user scope), so the database can only be read by this account.
    /// </summary>
    public sealed class DpapiSecretStore
    {
        private const string Entropy = "Stratum.Windows.DatabaseKey";
        private readonly string _path;

        public DpapiSecretStore(string path)
        {
            _path = path;
        }

        public string Load()
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            var protectedBytes = File.ReadAllBytes(_path);
            var plain = ProtectedData.Unprotect(protectedBytes, Encoding.UTF8.GetBytes(Entropy),
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }

        public void Save(string secret)
        {
            var plain = Encoding.UTF8.GetBytes(secret);
            var protectedBytes = ProtectedData.Protect(plain, Encoding.UTF8.GetBytes(Entropy),
                DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_path, protectedBytes);
        }

        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        public string LoadOrCreate()
        {
            var existing = Load();

            if (!string.IsNullOrEmpty(existing))
            {
                return existing;
            }

            var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            Save(generated);
            return generated;
        }
    }
}

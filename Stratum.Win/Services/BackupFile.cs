// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text;
using System.Threading.Tasks;
using Stratum.Core.Backup;
using Stratum.Core.Backup.Encryption;

namespace Stratum.Win.Services
{
    /// <summary>Reads and writes Stratum backup files (<c>.stratum</c>), compatible with the Android app.</summary>
    public static class BackupFile
    {
        public const string Extension = "stratum";

        private static readonly IBackupEncryption[] Encryptions =
        {
            new StrongBackupEncryption(),
            new LegacyBackupEncryption(),
            new NoBackupEncryption()
        };

        public static async Task<byte[]> EncryptAsync(Backup backup, string password)
        {
            return string.IsNullOrEmpty(password)
                ? await new NoBackupEncryption().EncryptAsync(backup, null)
                : await new StrongBackupEncryption().EncryptAsync(backup, password);
        }

        public static bool IsBackup(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return false;
            }

            foreach (var encryption in Encryptions)
            {
                if (encryption.CanBeDecrypted(data))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEncrypted(byte[] data)
        {
            return !new NoBackupEncryption().CanBeDecrypted(data);
        }

        public static async Task<Backup> DecryptAsync(byte[] data, string password)
        {
            foreach (var encryption in Encryptions)
            {
                if (!encryption.CanBeDecrypted(data))
                {
                    continue;
                }

                if (encryption is not NoBackupEncryption && string.IsNullOrEmpty(password))
                {
                    throw new BackupPasswordException("该备份已加密，请输入备份密码");
                }

                try
                {
                    return await encryption.DecryptAsync(data, password);
                }
                catch (Exception exception) when (encryption is not NoBackupEncryption)
                {
                    throw new BackupPasswordException("备份密码错误，或文件已损坏", exception);
                }
            }

            throw new ArgumentException("不是有效的 Stratum 备份文件");
        }

        public static string DefaultFileName()
        {
            return $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.{Extension}";
        }

        public static byte[] ToUtf8(string text)
        {
            return new UTF8Encoding(false).GetBytes(text);
        }
    }
}

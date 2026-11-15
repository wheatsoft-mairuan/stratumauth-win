// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Well known directories used by the Windows application.
    /// </summary>
    public static class AppPaths
    {
        private const string OverrideVariable = "STRATUM_DATA_DIR";

        public static string AppDataDirectory
        {
            get
            {
                var overridden = Environment.GetEnvironmentVariable(OverrideVariable);

                if (!string.IsNullOrEmpty(overridden))
                {
                    Directory.CreateDirectory(overridden);
                    return overridden;
                }

                var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var directory = Path.Combine(baseDirectory, "Stratum");
                Directory.CreateDirectory(directory);
                return directory;
            }
        }

        public static string DatabasePath => Path.Combine(AppDataDirectory, "authenticator.db3");

        public static string DatabaseSecretPath => Path.Combine(AppDataDirectory, "database.key");

        public static string CustomIconsDirectory
        {
            get
            {
                var directory = Path.Combine(AppDataDirectory, "custom-icons");
                Directory.CreateDirectory(directory);
                return directory;
            }
        }

        public static string LogsDirectory
        {
            get
            {
                var directory = Path.Combine(AppDataDirectory, "logs");
                Directory.CreateDirectory(directory);
                return directory;
            }
        }

        public static string AssetDirectory(string name)
        {
            return Path.Combine(AppContext.BaseDirectory, "Assets", name);
        }
    }
}

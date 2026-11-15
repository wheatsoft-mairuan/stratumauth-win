// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using Stratum.Core;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Serves bundled assets (icon files, HTML templates) from the Assets directory.
    /// </summary>
    public sealed class FileAssetProvider : IAssetProvider
    {
        public Task<byte[]> ReadBytesAsync(string path)
        {
            var fullPath = Resolve(path);
            return File.Exists(fullPath)
                ? File.ReadAllBytesAsync(fullPath)
                : throw new FileNotFoundException($"Asset {path} not found", path);
        }

        public Task<string> ReadStringAsync(string path)
        {
            var fullPath = Resolve(path);
            return File.Exists(fullPath)
                ? File.ReadAllTextAsync(fullPath)
                : throw new FileNotFoundException($"Asset {path} not found", path);
        }

        private static string Resolve(string path)
        {
            var normalized = path.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(AppContext.BaseDirectory, "Assets", normalized);
        }
    }
}

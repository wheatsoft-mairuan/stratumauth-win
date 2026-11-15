// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Stratum.Core;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Windows replacement for <c>IconMap</c>/<c>IconResolver</c>: maps icon keys
    /// (file names without extension) to PNG files shipped in <c>Assets/icons</c>.
    /// </summary>
    public sealed partial class IconCatalog : IIconResolver
    {
        public const string Default = "default";

        private readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);
        private readonly List<string> _keys = new();

        public static IconCatalog Instance { get; } = new();

        private IconCatalog()
        {
            var directory = AppPaths.AssetDirectory("icons");

            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.png"))
            {
                var key = Path.GetFileNameWithoutExtension(file);

                if (_paths.TryAdd(key, file))
                {
                    _keys.Add(key);
                }
            }

            _keys.Sort(StringComparer.Ordinal);
        }

        public IReadOnlyList<string> Keys => _keys;

        public int Count => _keys.Count;

        public bool Contains(string key)
        {
            return key != null && _paths.ContainsKey(key);
        }

        public string ResolvePath(string key, bool dark = false)
        {
            if (key == null)
            {
                return null;
            }

            if (dark)
            {
                if (_paths.TryGetValue(key + "_dark", out var darkPath))
                {
                    return darkPath;
                }

                if (key.EndsWith("_dark", StringComparison.Ordinal) && _paths.TryGetValue(key, out var path))
                {
                    return path;
                }
            }

            return _paths.TryGetValue(key, out var regular)
                ? regular
                : _paths.TryGetValue(key + "_dark", out var fallback) ? fallback : null;
        }

        public IReadOnlyList<string> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return _keys;
            }

            var lower = query.ToLowerInvariant();
            return _keys.Where(k => k.Contains(lower, StringComparison.Ordinal)).ToList();
        }

        /// <summary>
        /// Finds the icon key matching a service name (port of IconResolver.FindServiceKeyByName).
        /// </summary>
        public string FindServiceKeyByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var key = Simplify(name);

            if (_paths.ContainsKey(key))
            {
                return key;
            }

            var firstWord = Simplify(name.Split(new[] { ' ', '.' }, 2)[0]);

            return _paths.ContainsKey(firstWord) ? firstWord : null;
        }

        private static string Simplify(string input)
        {
            return SimplifyRegex().Replace(input.ToLowerInvariant(), "").Trim();
        }

        [GeneratedRegex("[^a-z0-9]")]
        private static partial Regex SimplifyRegex();
    }
}

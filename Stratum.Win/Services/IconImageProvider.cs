// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using Stratum.Core.Entity;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Resolves icon keys ("github", "@&lt;id&gt;" for custom icons) to frozen bitmaps and caches them.
    /// </summary>
    public static class IconImageProvider
    {
        private static readonly Dictionary<string, BitmapImage> Cache = new();
        private static readonly object Sync = new();

        /// <summary>Custom icon data keyed by id, refreshed after imports and icon edits.</summary>
        public static Dictionary<string, byte[]> CustomIcons { get; } = new();

        public static bool IsCustom(string key)
        {
            return !string.IsNullOrEmpty(key) && key.StartsWith(CustomIcon.Prefix);
        }

        public static BitmapImage Load(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Load(IconCatalog.Default);
            }

            lock (Sync)
            {
                if (Cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            BitmapImage image = null;

            try
            {
                if (IsCustom(key))
                {
                    var id = key.Substring(1);

                    if (CustomIcons.TryGetValue(id, out var data))
                    {
                        image = FromBytes(data);
                    }
                }
                else
                {
                    var path = IconCatalog.Instance.ResolvePath(key, false);

                    if (path == null && key != IconCatalog.Default)
                    {
                        path = IconCatalog.Instance.ResolvePath(IconCatalog.Default, false);
                    }

                    if (path != null)
                    {
                        image = FromUri(path);
                    }
                }
            }
            catch (Exception)
            {
                image = null;
            }

            if (image != null)
            {
                lock (Sync)
                {
                    Cache[key] = image;
                }
            }

            return image;
        }

        public static BitmapImage FromBytes(byte[] data, int decodePixelWidth = 0)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            using var stream = new MemoryStream(data);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;

            if (decodePixelWidth > 0)
            {
                image.DecodePixelWidth = decodePixelWidth;
            }

            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static BitmapImage FromUri(string path)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static void Invalidate()
        {
            lock (Sync)
            {
                Cache.Clear();
            }
        }
    }
}

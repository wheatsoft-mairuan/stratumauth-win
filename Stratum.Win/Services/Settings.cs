// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stratum.Win.Services
{
    public sealed class AppSettings
    {
        [JsonPropertyName("codeGroupSize")]
        public int CodeGroupSize { get; set; } = 3;

        [JsonPropertyName("showUsernames")]
        public bool ShowUsernames { get; set; } = true;

        [JsonPropertyName("sortMode")]
        public string SortMode { get; set; } = "custom";

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "dark";

        [JsonPropertyName("viewMode")]
        public string ViewMode { get; set; } = "list";
    }

    /// <summary>
    /// Application preferences, persisted as JSON in the app data directory.
    /// </summary>
    public static class Settings
    {
        private static readonly Lazy<AppSettings> Lazy = new(Load);
        private static readonly object Sync = new();

        public static AppSettings Current => Lazy.Value;

        public static int CodeGroupSize => Current.CodeGroupSize;

        private static string Path => System.IO.Path.Combine(AppPaths.AppDataDirectory, "settings.json");

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    var json = File.ReadAllText(Path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                // Fall back to defaults when the settings file is unreadable.
            }

            return new AppSettings();
        }

        public static void Save()
        {
            lock (Sync)
            {
                try
                {
                    var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(Path, json);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}

// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Minimal file logger replacing Serilog on the desktop port.
    /// </summary>
    public sealed class AppLog
    {
        private static readonly object Sync = new();
        private readonly string _category;

        private AppLog(string category)
        {
            _category = category;
        }

        public static AppLog For<T>()
        {
            return new AppLog(typeof(T).Name);
        }

        public static AppLog ForContext(string category)
        {
            return new AppLog(category);
        }

        public void Verbose(string message)
        {
            Write("VERBOSE", message);
        }

        public void Debug(string message)
        {
            Write("DEBUG", message);
        }

        public void Debug(string template, params object[] values)
        {
            Write("DEBUG", Format(template, values));
        }

        public void Information(string template, params object[] values)
        {
            Write("INFO", Format(template, values));
        }

        public void Warning(string template, params object[] values)
        {
            Write("WARN", Format(template, values));
        }

        public void Error(Exception exception, string template, params object[] values)
        {
            Write("ERROR", Format(template, values) + Environment.NewLine + exception);
        }

        private static string Format(string template, object[] values)
        {
            if (values == null || values.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, values);
            }
            catch (FormatException)
            {
                return template + " " + string.Join(", ", values);
            }
        }

        private void Write(string level, string message)
        {
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {_category}: {message}";

            System.Diagnostics.Debug.WriteLine(line);

            try
            {
                lock (Sync)
                {
                    var path = Path.Combine(AppPaths.LogsDirectory, $"stratum-{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch (IOException)
            {
                // Logging must never take the application down.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}

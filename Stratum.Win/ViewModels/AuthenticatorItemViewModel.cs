// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text;
using System.Windows.Media.Imaging;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Util;
using Stratum.Win.Services;

namespace Stratum.Win.ViewModels
{
    public sealed class AuthenticatorItemViewModel : ObservableObject
    {
        private readonly Authenticator _auth;
        private readonly int _groupSize;

        private string _code;
        private string _codeDisplay;
        private double _progress = 1;
        private int _secondsRemaining;
        private long _lastSlot = -1;

        public AuthenticatorItemViewModel(Authenticator auth, int codeGroupSize)
        {
            _auth = auth;
            _groupSize = codeGroupSize <= 0 ? 3 : codeGroupSize;
            IconImage = LoadIcon();
            Tick();
        }

        public Authenticator Auth => _auth;

        public string Issuer => _auth.Issuer;

        public string Username => _auth.Username;

        public bool HasUsername => !string.IsNullOrEmpty(_auth.Username);

        public string IconKey => string.IsNullOrEmpty(_auth.Icon) ? IconCatalog.Default : _auth.Icon;

        public BitmapImage IconImage { get; }

        public string Code
        {
            get => _code;
            private set => Set(ref _code, value);
        }

        /// <summary>Formatted code with grouping spaces; hidden until known.</summary>
        public string CodeDisplay
        {
            get => _codeDisplay;
            private set => Set(ref _codeDisplay, value);
        }

        /// <summary>Fraction of the period that is still remaining (1 鈫?full ring).</summary>
        public double Progress
        {
            get => _progress;
            private set => Set(ref _progress, value);
        }

        public int SecondsRemaining
        {
            get => _secondsRemaining;
            private set => Set(ref _secondsRemaining, value);
        }

        public bool IsCounter => _auth.Type.GetGenerationMethod() == GenerationMethod.Counter;

        public bool IsTimeBased => !IsCounter;

        public void Tick()
        {
            if (IsCounter)
            {
                UpdateCode(_auth.GetCode());
                Progress = 1;
                SecondsRemaining = 0;
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var period = Math.Max(1, _auth.Period);
            var slot = now - now % period;
            var remaining = period - now % period;

            Progress = (double) remaining / period;
            SecondsRemaining = (int) remaining;

            if (slot != _lastSlot)
            {
                _lastSlot = slot;
                UpdateCode(_auth.GetCode(slot));
            }
        }

        public void RefreshCounter()
        {
            if (IsCounter)
            {
                _auth.Counter++;
                UpdateCode(_auth.GetCode());
            }
        }

        private void UpdateCode(string code)
        {
            Code = code;
            CodeDisplay = FormatCode(code, _groupSize);
        }

        private static string FormatCode(string code, int groupSize)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            if (groupSize <= 1 || code.Length <= groupSize)
            {
                return code;
            }

            var builder = new StringBuilder(code.Length + code.Length / groupSize);

            for (var i = 0; i < code.Length; i++)
            {
                if (i > 0 && i % groupSize == 0)
                {
                    builder.Append(' ');
                }

                builder.Append(code[i]);
            }

            return builder.ToString();
        }

        private BitmapImage LoadIcon()
        {
            var catalog = IconCatalog.Instance;
            var path = catalog.ResolvePath(IconKey, false)
                       ?? catalog.ResolvePath(IconCatalog.Default, false);

            if (path == null)
            {
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

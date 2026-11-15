// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Stratum.Win.Services;

namespace Stratum.Win.ViewModels
{
    /// <summary>One tile of the icon picker; the bitmap is decoded the first time it is shown.</summary>
    public sealed class IconEntry : INotifyPropertyChanged
    {
        private BitmapImage _image;
        private bool _isSelected;

        public IconEntry(string key)
        {
            Key = key;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Key { get; }

        public string Display => Key.Replace('_', ' ');

        public string Path => IconCatalog.Instance.ResolvePath(Key);

        public BitmapImage Image => _image ??= Load();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        private BitmapImage Load()
        {
            var path = Path;

            if (path == null)
            {
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.DecodePixelWidth = 40;
                image.CacheOption = BitmapCacheOption.OnLoad;
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

    /// <summary>A horizontal band of icon tiles; the picker virtualizes over these rows.</summary>
    public sealed class IconRow
    {
        public IconRow(IReadOnlyList<IconEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<IconEntry> Entries { get; }
    }
}

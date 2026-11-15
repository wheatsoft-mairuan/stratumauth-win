// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stratum.Win.Services;
using Stratum.Win.ViewModels;

namespace Stratum.Win.Views
{
    public partial class IconPickerWindow : Window
    {
        private const int Columns = 8;

        private readonly List<IconEntry> _all;
        private IconEntry _selected;

        public IconPickerWindow(string selectedKey = null)
        {
            InitializeComponent();

            _all = IconCatalog.Instance.Keys.Select(key => new IconEntry(key)).ToList();
            Apply(string.Empty);

            var selected = _all.FirstOrDefault(entry => entry.Key == selectedKey) ??
                           _all.FirstOrDefault(entry => entry.Key == IconCatalog.Default);

            if (selected != null)
            {
                Select(selected);
                IconList.ScrollIntoView(IconList.Items[IndexOf(selected) / Columns]);
            }

            Loaded += (_, _) => SearchBox.Focus();
        }

        public string SelectedKey { get; private set; }

        private int IndexOf(IconEntry entry)
        {
            var index = _all.IndexOf(entry);
            return index < 0 ? 0 : index;
        }

        private void Apply(string query)
        {
            var matches = string.IsNullOrEmpty(query)
                ? _all
                : _all.Where(entry => entry.Key.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            var rows = new List<IconRow>();

            for (var i = 0; i < matches.Count; i += Columns)
            {
                rows.Add(new IconRow(matches.Skip(i).Take(Columns).ToList()));
            }

            IconList.ItemsSource = rows;
            CountText.Text = $"{matches.Count} / {_all.Count} 个图标";
        }

        private void Select(IconEntry entry)
        {
            if (_selected != null)
            {
                _selected.IsSelected = false;
            }

            _selected = entry;

            if (_selected != null)
            {
                _selected.IsSelected = true;
                SelectedKey = _selected.Key;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Apply(SearchBox.Text);
        }

        private void Tile_Click(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not IconEntry entry)
            {
                return;
            }

            Select(entry);

            if (e.ClickCount >= 2)
            {
                DialogResult = true;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedKey == null)
            {
                return;
            }

            DialogResult = true;
        }
    }
}

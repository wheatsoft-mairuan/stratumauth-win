// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Windows;
using Stratum.Win.Services;

namespace Stratum.Win.Views
{
    public partial class SettingsWindow : Window
    {
        private static readonly int[] GroupSizes = { 0, 3, 4 };

        private static readonly string[] SortModes = { "custom", "asc", "desc", "copydesc", "copyasc" };

        public SettingsWindow()
        {
            InitializeComponent();

            var settings = Settings.Current;
            GroupSizeBox.SelectedIndex = System.Array.IndexOf(GroupSizes, settings.CodeGroupSize) is var index && index >= 0
                ? index
                : 1;
            SortBox.SelectedIndex = System.Array.IndexOf(SortModes, settings.SortMode) is var sortIndex && sortIndex >= 0
                ? sortIndex
                : 0;
            ShowUsernamesBox.IsChecked = settings.ShowUsernames;
            ConfirmDeleteBox.IsChecked = settings.ConfirmDelete;

            DataPathText.Text = AppPaths.AppDataDirectory;
            SecretText.Text = "数据库密钥由 Windows DPAPI 保护，只有当前用户能解密。";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = Settings.Current;
            settings.CodeGroupSize = GroupSizes[GroupSizeBox.SelectedIndex < 0 ? 1 : GroupSizeBox.SelectedIndex];
            settings.SortMode = SortModes[SortBox.SelectedIndex < 0 ? 0 : SortBox.SelectedIndex];
            settings.ShowUsernames = ShowUsernamesBox.IsChecked == true;
            settings.ConfirmDelete = ConfirmDeleteBox.IsChecked == true;
            Settings.Save();

            DialogResult = true;
        }
    }
}

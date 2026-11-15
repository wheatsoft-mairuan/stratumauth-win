// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Stratum.Core.Backup;
using Stratum.Win.Services;

namespace Stratum.Win.Views
{
    public partial class BackupWindow : Window
    {
        public BackupWindow()
        {
            InitializeComponent();
            FormatBox.SelectedIndex = 0;
        }

        private void FormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PasswordInput == null)
            {
                return;
            }

            var isBackup = FormatBox.SelectedIndex == 0;
            PasswordInput.IsEnabled = isBackup;
            PasswordLabel.Opacity = isBackup ? 1 : 0.5;

            var extension = FormatBox.SelectedIndex switch
            {
                1 => "html",
                2 => "txt",
                _ => BackupFile.Extension
            };

            if (string.IsNullOrEmpty(PathBox.Text) || PathBox.Text.EndsWith(".stratum") || PathBox.Text.EndsWith(".html") ||
                PathBox.Text.EndsWith(".txt"))
            {
                PathBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    BackupFile.DefaultFileName().Replace(".stratum", "." + extension));
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var (filter, extension) = FormatBox.SelectedIndex switch
            {
                1 => ("网页备份|*.html", "html"),
                2 => ("URI 列表|*.txt", "txt"),
                _ => ("Stratum 备份|*.stratum", "stratum")
            };

            var dialog = new SaveFileDialog
            {
                Title = "导出备份",
                Filter = filter,
                DefaultExt = extension,
                FileName = Path.GetFileName(PathBox.Text) is { Length: > 0 } name ? name : $"backup.{extension}"
            };

            if (dialog.ShowDialog(this) == true)
            {
                PathBox.Text = dialog.FileName;
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var path = PathBox.Text?.Trim();

            if (string.IsNullOrEmpty(path))
            {
                ShowStatus("请先选择保存位置", true);
                return;
            }

            try
            {
                ShowStatus("正在导出…", false);

                switch (FormatBox.SelectedIndex)
                {
                    case 1:
                    {
                        var html = await AppServices.BackupService.CreateHtmlBackupAsync();
                        await File.WriteAllTextAsync(path, html.ToString(), new UTF8Encoding(false));
                        break;
                    }
                    case 2:
                    {
                        var list = await AppServices.BackupService.CreateUriListBackupAsync();
                        await File.WriteAllTextAsync(path, list.ToString(), new UTF8Encoding(false));
                        break;
                    }
                    default:
                    {
                        var backup = await AppServices.BackupService.CreateBackupAsync();
                        var data = await BackupFile.EncryptAsync(backup, PasswordInput.Password);
                        await File.WriteAllBytesAsync(path, data);
                        break;
                    }
                }

                ShowStatus($"已导出到 {path}", false);
            }
            catch (Exception exception)
            {
                ShowStatus($"导出失败：{exception.Message}", true);
            }
        }

        private void ShowStatus(string text, bool isError)
        {
            StatusText.Foreground = (System.Windows.Media.Brush) FindResource(isError ? "Danger" : "TextSecondary");
            StatusText.Text = text;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

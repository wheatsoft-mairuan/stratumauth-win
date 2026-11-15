// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Stratum.Core.Backup;
using Stratum.Win.Services;

namespace Stratum.Win.Views
{
    public partial class ImportWindow : Window
    {
        public ImportWindow()
        {
            InitializeComponent();

            SourceBox.Items.Add("自动检测");
            foreach (var source in ImportSources.All)
            {
                SourceBox.Items.Add(source.Name);
            }

            SourceBox.SelectedIndex = 0;
        }

        public event EventHandler Imported;

        private string _path;

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择要导入的备份文件",
                Filter = "备份文件|*.stratum;*.json;*.txt;*.html;*.htm;*.zip;*.xml;*.csv|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                _path = dialog.FileName;
                PathBox.Text = _path;
                StatusText.Text = string.Empty;
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
            {
                StatusText.Foreground = (System.Windows.Media.Brush) FindResource("Danger");
                StatusText.Text = "请先选择要导入的文件";
                return;
            }

            try
            {
                StatusText.Foreground = (System.Windows.Media.Brush) FindResource("TextSecondary");
                StatusText.Text = "正在导入…";

                var data = await File.ReadAllBytesAsync(_path);
                var password = PasswordInput.Password;
                var summary = await ImportAsync(data, password);

                StatusText.Foreground = (System.Windows.Media.Brush) FindResource("TextSecondary");
                StatusText.Text = summary;
                Imported?.Invoke(this, EventArgs.Empty);
            }
            catch (BackupPasswordException exception)
            {
                StatusText.Foreground = (System.Windows.Media.Brush) FindResource("Danger");
                StatusText.Text = exception.Message;
            }
            catch (Exception exception)
            {
                StatusText.Foreground = (System.Windows.Media.Brush) FindResource("Danger");
                StatusText.Text = $"导入失败：{exception.Message}";
            }
        }

        private async Task<string> ImportAsync(byte[] data, string password)
        {
            if (BackupFile.IsBackup(data))
            {
                var backup = await BackupFile.DecryptAsync(data, password);
                var result = await AppServices.RestoreService.RestoreAsync(backup);
                return Describe(result);
            }

            var explicitIndex = SourceBox.SelectedIndex;

            if (explicitIndex > 0)
            {
                var source = ImportSources.All[explicitIndex - 1];
                var result = await RunAsync(source, data, password);
                return Describe(result.Item2) + DescribeFailures(result.Item1);
            }

            var errors = new System.Text.StringBuilder();

            foreach (var source in ImportSources.All)
            {
                try
                {
                    var result = await RunAsync(source, data, password);
                    return $"{source.Name}：{Describe(result.Item2)}{DescribeFailures(result.Item1)}";
                }
                catch (BackupPasswordException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    errors.AppendLine($"{source.Name}: {exception.Message}");
                }
            }

            throw new InvalidOperationException($"没有可识别的格式。尝试了 {ImportSources.All.Count} 种来源。\n{errors}");
        }

        private static Task<(ConversionResult, RestoreResult)> RunAsync(ImportSource source, byte[] data,
            string password)
        {
            return AppServices.ImportService.ImportAsync(source.Create(), data, password);
        }

        private static string Describe(RestoreResult result)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (result.AddedAuthenticatorCount > 0)
            {
                parts.Add($"新增 {result.AddedAuthenticatorCount} 个验证器");
            }

            if (result.UpdatedAuthenticatorCount > 0)
            {
                parts.Add($"更新 {result.UpdatedAuthenticatorCount} 个验证器");
            }

            if (result.AddedCategoryCount > 0)
            {
                parts.Add($"新增 {result.AddedCategoryCount} 个分类");
            }

            if (result.AddedCustomIconCount > 0)
            {
                parts.Add($"新增 {result.AddedCustomIconCount} 个自定义图标");
            }

            return parts.Count == 0 ? "没有需要导入的内容" : string.Join("，", parts);
        }

        private static string DescribeFailures(ConversionResult result)
        {
            if (result.Failures == null || result.Failures.Count == 0)
            {
                return string.Empty;
            }

            var descriptions = result.Failures.Take(3)
                .Select(failure => $"{failure.Description}: {failure.Error}");

            var suffix = result.Failures.Count > 3 ? $" 等 {result.Failures.Count} 项" : string.Empty;
            return $"\n未能转换：{string.Join("；", descriptions)}{suffix}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

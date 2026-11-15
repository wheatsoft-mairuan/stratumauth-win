// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Win.Services;
using Stratum.Win.ViewModels;

namespace Stratum.Win.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += OnLoaded;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadAsync();
            _viewModel.StartTicking();
        }

        private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (List.SelectedItem is AuthenticatorItemViewModel item)
                {
                    await _viewModel.CopyAsync(item);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete && List.SelectedItem is AuthenticatorItemViewModel selected)
            {
                await DeleteAsync(selected);
                e.Handled = true;
            }
        }

        private async void Card_Click(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AuthenticatorItemViewModel item)
            {
                await _viewModel.CopyAsync(item);
            }
        }

        private async void Copy_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AuthenticatorItemViewModel item)
            {
                await _viewModel.CopyAsync(item);
            }
        }

        private async void RefreshCounter_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AuthenticatorItemViewModel item)
            {
                await _viewModel.IncrementCounterAsync(item);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AuthenticatorItemViewModel item)
            {
                await DeleteAsync(item);
            }
        }

        private async Task DeleteAsync(AuthenticatorItemViewModel item)
        {
            var result = MessageBox.Show(this, $"确定删除「{item.Issuer}」吗？", "删除验证器",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await _viewModel.DeleteAsync(item);
            }
            catch (Exception exception)
            {
                ShowError("无法删除", exception);
            }
        }

        private async void AddManual_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthenticatorEditorWindow { Owner = this };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                await TryAddAsync(dialog.Result);
            }
        }

        private void AddUri_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputWindow("从 URI 添加", "粘贴 otpauth:// 或 motp:// 链接：")
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                AddFromLinkAsync(dialog.Value);
            }
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择包含二维码的图片",
                Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                var text = QrDecoder.DecodeFile(dialog.FileName);
                AddFromLinkAsync(text);
            }
            catch (Exception exception)
            {
                ShowError("无法读取二维码", exception);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var window = new ImportWindow { Owner = this };
            window.Imported += async (_, _) => await _viewModel.ReloadAsync();
            window.ShowDialog();
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            var window = new BackupWindow { Owner = this };
            window.ShowDialog();
        }

        private async void AddFromLinkAsync(string link)        {
            try
            {
                var auth = ParseLink(link);
                await TryAddAsync(auth);
            }
            catch (Exception exception)
            {
                ShowError("无法添加", exception);
            }
        }

        private async Task TryAddAsync(Authenticator auth)
        {
            try
            {
                await _viewModel.AddAsync(auth);

                if (AppServices.Database is not null)
                {
                    AppServices.AuthenticatorView.Search = _viewModel.Search;
                }
            }
            catch (Exception exception)
            {
                ShowError("无法添加", exception);
            }
        }

        private static Authenticator ParseLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                throw new ArgumentException("链接为空");
            }

            link = link.Trim();

            if (link.StartsWith("otpauth-migration://", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("这是批量迁移码，请使用「导入」功能");
            }

            var result = Stratum.Core.UriParser.ParseStandardUri(link, IconCatalog.Instance);
            var auth = result.Authenticator ?? throw new ArgumentException("无法识别的链接");

            if (auth.Type.HasPin() && string.IsNullOrEmpty(auth.Pin))
            {
                throw new ArgumentException($"{auth.Type} 需要 PIN，请用「添加」手动录入");
            }

            if (string.IsNullOrEmpty(auth.Icon))
            {
                auth.Icon = IconCatalog.Instance.FindServiceKeyByName(auth.Issuer) ?? IconCatalog.Default;
            }

            auth.Validate();
            return auth;
        }

        private void ShowError(string title, Exception exception)
        {
            MessageBox.Show(this, exception.Message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

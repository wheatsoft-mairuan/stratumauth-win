// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            if (Services.Settings.ConfirmDelete)
            {
                var result = MessageBox.Show(this, $"确定删除「{item.Issuer}」吗？", "删除验证器",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
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

        private void Card_RightClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not AuthenticatorItemViewModel item)
            {
                return;
            }

            var menu = new ContextMenu();

            var copy = new MenuItem { Header = "复制验证码" };
            copy.Click += async (_, _) => await _viewModel.CopyAsync(item);

            var edit = new MenuItem { Header = "编辑…" };
            edit.Click += async (_, _) => await EditAsync(item);

            var categories = new MenuItem { Header = "分类…" };
            categories.Click += async (_, _) => await AssignCategoriesAsync(item);

            var up = new MenuItem { Header = "上移" };
            up.Click += async (_, _) => await MoveAsync(item, -1);

            var down = new MenuItem { Header = "下移" };
            down.Click += async (_, _) => await MoveAsync(item, 1);

            var delete = new MenuItem { Header = "删除" };
            delete.Click += async (_, _) => await DeleteAsync(item);

            menu.Items.Add(copy);
            menu.Items.Add(edit);
            menu.Items.Add(categories);
            menu.Items.Add(new Separator());
            menu.Items.Add(up);
            menu.Items.Add(down);
            menu.Items.Add(new Separator());
            menu.Items.Add(delete);
            menu.PlacementTarget = (UIElement) sender;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private async Task EditAsync(AuthenticatorItemViewModel item)
        {
            var dialog = new AuthenticatorEditorWindow(item.Auth) { Owner = this };

            if (dialog.ShowDialog() != true || dialog.Result == null)
            {
                return;
            }

            try
            {
                await _viewModel.UpdateAsync(dialog.Result);
                await ApplyCustomIconAsync(dialog.Result, dialog.CustomIcon);
                await _viewModel.ReloadAsync();
            }
            catch (Exception exception)
            {
                ShowError("无法保存", exception);
            }
        }

        private async Task MoveAsync(AuthenticatorItemViewModel item, int delta)
        {
            try
            {
                await _viewModel.MoveAsync(item, delta);
            }
            catch (Exception exception)
            {
                ShowError("无法调整顺序", exception);
            }
        }

        private async Task AssignCategoriesAsync(AuthenticatorItemViewModel item)        {
            try
            {
                var all = await AppServices.CategoryService.GetAllCategoriesAsync();
                var assigned = await _viewModel.GetAssignedCategoriesAsync(item.Auth);
                var dialog = new CategoryAssignWindow(all, assigned) { Owner = this };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                await _viewModel.SetAssignedCategoriesAsync(item.Auth, dialog.Selected);
            }
            catch (Exception exception)
            {
                ShowError("无法保存分类", exception);
            }
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputWindow("新建分类", "分类名称：", "例如：工作、个人")
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var name = dialog.Value?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            try
            {
                await _viewModel.AddCategoryAsync(name);
            }
            catch (Exception exception)
            {
                ShowError("无法新建分类", exception);
            }
        }

        private void CategoryList_RightClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as ListBox)?.SelectedItem is not CategoryNode node || !node.IsEditable)
            {
                return;
            }

            var menu = new ContextMenu();

            var rename = new MenuItem { Header = "重命名…" };
            rename.Click += async (_, _) => await RenameCategoryAsync(node);

            var delete = new MenuItem { Header = "删除" };
            delete.Click += async (_, _) => await DeleteCategoryAsync(node);

            menu.Items.Add(rename);
            menu.Items.Add(new Separator());
            menu.Items.Add(delete);
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private async Task RenameCategoryAsync(CategoryNode node)
        {
            var dialog = new TextInputWindow("重命名分类", "分类名称：", null, node.Name)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var name = dialog.Value?.Trim();

            if (string.IsNullOrEmpty(name) || name == node.Name)
            {
                return;
            }

            try
            {
                await _viewModel.RenameCategoryAsync(node.Category, name);
            }
            catch (Exception exception)
            {
                ShowError("无法重命名分类", exception);
            }
        }

        private async Task DeleteCategoryAsync(CategoryNode node)
        {
            var result = MessageBox.Show(this,
                $"删除分类「{node.Name}」？其中的验证器不会被删除，只会失去这个分类。",
                "删除分类", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            try
            {
                await _viewModel.DeleteCategoryAsync(node.Category);
            }
            catch (Exception exception)
            {
                ShowError("无法删除分类", exception);
            }
        }

        private static async Task ApplyCustomIconAsync(Authenticator auth, CustomIcon icon)
        {
            if (icon == null)
            {
                return;
            }

            await AppServices.AuthenticatorService.SetCustomIconAsync(auth, icon);
            await AppServices.ReloadCustomIconsAsync();
        }

        private async void AddManual_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthenticatorEditorWindow { Owner = this };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                await TryAddAsync(dialog.Result);
                await ApplyCustomIconAsync(dialog.Result, dialog.CustomIcon);
                await _viewModel.ReloadAsync();
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

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsWindow { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.ApplySettings();
            }
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

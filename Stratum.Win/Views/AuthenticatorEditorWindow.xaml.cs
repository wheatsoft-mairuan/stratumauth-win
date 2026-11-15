// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Util;
using Stratum.Win.Services;

namespace Stratum.Win.Views
{
    /// <summary>Adds a new authenticator, or edits an existing one when created with <see cref="Existing" />.</summary>
    public partial class AuthenticatorEditorWindow : Window
    {
        private string _iconKey;

        public AuthenticatorEditorWindow()
        {
            InitializeComponent();
            AlgorithmBox.SelectedIndex = 0;
            TypeBox.SelectedIndex = 0;
            UpdateIcon();
        }

        public AuthenticatorEditorWindow(Authenticator existing) : this()
        {
            Existing = existing;
            Title = "编辑验证器";
            HeaderText.Text = "编辑验证器";
            SubtitleText.Text = "密钥不可修改；如需更换密钥，请删除后重新添加。";

            TypeBox.SelectedIndex = existing.Type switch
            {
                AuthenticatorType.Hotp => 1,
                AuthenticatorType.SteamOtp => 2,
                AuthenticatorType.YandexOtp => 3,
                AuthenticatorType.MobileOtp => 4,
                _ => 0
            };

            AlgorithmBox.SelectedIndex = existing.Algorithm switch
            {
                HashAlgorithm.Sha256 => 1,
                HashAlgorithm.Sha512 => 2,
                _ => 0
            };

            IssuerBox.Text = existing.Issuer;
            UsernameBox.Text = existing.Username;
            SecretBox.Text = existing.Secret;
            DigitsBox.Text = existing.Digits.ToString();
            PeriodBox.Text = existing.Period.ToString();
            CounterBox.Text = existing.Counter.ToString();
            PinBox.Text = existing.Pin;

            _iconKey = existing.Icon;
            UpdateIcon();
        }

        public Authenticator Existing { get; }

        public Authenticator Result { get; private set; }

        /// <summary>Newly decoded custom icon, persisted by the caller after the record is saved.</summary>
        public Stratum.Core.Entity.CustomIcon CustomIcon { get; private set; }

        private AuthenticatorType SelectedType => TypeBox.SelectedIndex switch
        {
            1 => AuthenticatorType.Hotp,
            2 => AuthenticatorType.SteamOtp,
            3 => AuthenticatorType.YandexOtp,
            4 => AuthenticatorType.MobileOtp,
            _ => AuthenticatorType.Totp
        };

        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeBox == null)
            {
                return;
            }

            var type = SelectedType;
            var hasPin = type.HasPin();

            if (PinBox != null)
            {
                PinBox.IsEnabled = hasPin;
            }

            if (CounterBox != null)
            {
                CounterBox.IsEnabled = type.GetGenerationMethod() == GenerationMethod.Counter;
            }

            if (SecretBox != null)
            {
                SecretBox.IsEnabled = type != AuthenticatorType.MobileOtp && Existing == null;
            }
        }

        private void IssuerBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_iconKey == null)
            {
                UpdateIcon();
            }
        }

        private void PickIcon_Click(object sender, RoutedEventArgs e)
        {
            var picker = new IconPickerWindow(ResolveIcon()) { Owner = this };

            if (picker.ShowDialog() == true)
            {
                _iconKey = picker.SelectedKey;
                CustomIcon = null;
                UpdateIcon();
            }
        }

        private async void PickImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择图标图片",
                Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                var data = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
                var icon = await AppServices.CustomIconDecoder.DecodeAsync(data, true);

                if (icon == null)
                {
                    MessageBox.Show(this, "无法读取这张图片。", "图标", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                CustomIcon = icon;
                _iconKey = null;
                UpdateIcon();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "图标", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetIcon_Click(object sender, RoutedEventArgs e)
        {
            _iconKey = null;
            CustomIcon = null;
            UpdateIcon();
        }

        private string ResolveIcon()
        {
            if (CustomIcon != null)
            {
                return CustomIcon.Prefix + CustomIcon.Id;
            }

            if (_iconKey != null)
            {
                return _iconKey;
            }

            return IconCatalog.Instance.FindServiceKeyByName(IssuerBox.Text) ?? IconCatalog.Default;
        }

        private void UpdateIcon()
        {
            if (IconPreview == null)
            {
                return;
            }

            if (CustomIcon != null)
            {
                IconPreview.Source = IconImageProvider.FromBytes(CustomIcon.Data, 48);
                return;
            }

            var key = ResolveIcon();

            if (IconImageProvider.IsCustom(key))
            {
                IconPreview.Source = IconImageProvider.Load(key);
                return;
            }

            var path = IconCatalog.Instance.ResolvePath(key);

            if (path == null)
            {
                IconPreview.Source = null;
                return;
            }

            try
            {
                var image = IconImageProvider.FromUri(path);
                IconPreview.Source = image;
            }
            catch (Exception)
            {
                IconPreview.Source = null;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var type = SelectedType;
                var auth = new Authenticator
                {
                    Type = type,
                    Issuer = IssuerBox.Text.Trim(),
                    Username = string.IsNullOrWhiteSpace(UsernameBox.Text) ? null : UsernameBox.Text.Trim(),
                    Algorithm = AlgorithmBox.SelectedIndex switch
                    {
                        1 => HashAlgorithm.Sha256,
                        2 => HashAlgorithm.Sha512,
                        _ => HashAlgorithm.Sha1
                    },
                    Digits = ParseInt(DigitsBox.Text, type.GetDefaultDigits(), "位数"),
                    Period = ParseInt(PeriodBox.Text, type.GetDefaultPeriod(), "周期"),
                    Counter = ParseLong(CounterBox.Text, 0, "计数器"),
                    Pin = string.IsNullOrWhiteSpace(PinBox.Text) ? null : PinBox.Text,
                    Icon = ResolveIcon()
                };

                var secret = SecretBox.Text.Trim();

                if (auth.Type == AuthenticatorType.MobileOtp)
                {
                    auth.Secret = secret;
                }
                else
                {
                    auth.Secret = SecretUtil.Normalise(secret, type);
                }

                if (Existing != null)
                {
                    auth.CopyCount = Existing.CopyCount;
                    auth.Ranking = Existing.Ranking;
                }

                auth.Validate();

                Result = auth;
                DialogResult = true;
            }
            catch (Exception exception)
            {
                ErrorText.Text = exception.Message;
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private static int ParseInt(string text, int fallback, string field)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            return int.TryParse(text.Trim(), out var value)
                ? value
                : throw new ArgumentException($"{field}必须是数字");
        }

        private static long ParseLong(string text, long fallback, string field)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            return long.TryParse(text.Trim(), out var value)
                ? value
                : throw new ArgumentException($"{field}必须是数字");
        }
    }
}

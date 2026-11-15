// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Util;
using Stratum.Win.Services;

namespace Stratum.Win.Views
{
    public partial class AuthenticatorEditorWindow : Window
    {
        public AuthenticatorEditorWindow()
        {
            InitializeComponent();
            AlgorithmBox.SelectedIndex = 0;
            TypeBox.SelectedIndex = 0;
        }

        public Authenticator Result { get; private set; }

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
                SecretBox.IsEnabled = type != AuthenticatorType.MobileOtp;
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
                    Pin = string.IsNullOrWhiteSpace(PinBox.Text) ? null : PinBox.Text
                };

                var secret = SecretBox.Text.Trim();

                if (type != AuthenticatorType.MobileOtp)
                {
                    auth.Secret = SecretUtil.Normalise(secret, type);
                }
                else
                {
                    auth.Secret = secret;
                }

                if (string.IsNullOrEmpty(auth.Icon))
                {
                    auth.Icon = IconCatalog.Instance.FindServiceKeyByName(auth.Issuer) ?? IconCatalog.Default;
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

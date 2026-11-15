// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Windows;

namespace Stratum.Win.Views
{
    public partial class TextInputWindow : Window
    {
        public TextInputWindow(string title, string hint, string placeholder = null, string initial = null)
        {
            InitializeComponent();
            Title = title;
            HintText.Text = hint;

            if (!string.IsNullOrEmpty(placeholder))
            {
                Input.ToolTip = placeholder;
            }

            if (!string.IsNullOrEmpty(initial))
            {
                Input.Text = initial;
            }

            Input.Focus();
            Input.SelectAll();
        }

        public string Value => Input.Text;

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Input.Text))
            {
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

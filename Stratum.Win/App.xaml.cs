// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stratum.Win.Services;

namespace Stratum.Win
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await AppServices.Initializer;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"无法打开数据库：\n\n{exception.Message}", "Stratum",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            var smokeDialogIndex = Array.IndexOf(e.Args, "--smoke-dialog");

            if (smokeDialogIndex >= 0)
            {
                var kind = e.Args.ElementAtOrDefault(smokeDialogIndex + 1) ?? "editor";
                var dialogPath = e.Args.ElementAtOrDefault(smokeDialogIndex + 2)
                                 ?? Path.Combine(Environment.CurrentDirectory, "dialog.png");

                Window dialog;

                if (kind == "assign")
                {
                    var categories = await AppServices.CategoryRepository.GetAllAsync();
                    dialog = new Views.CategoryAssignWindow(categories,
                        new System.Collections.Generic.List<Stratum.Core.Entity.Category>());
                }
                else
                {
                    dialog = kind switch
                    {
                        "uri" => new Views.TextInputWindow("从 URI 添加", "粘贴 otpauth:// 或 motp:// 链接："),
                        "import" => new Views.ImportWindow(),
                        "backup" => new Views.BackupWindow(),
                        "icons" => new Views.IconPickerWindow("github"),
                        "edit" => new Views.AuthenticatorEditorWindow(AppServices.AuthenticatorView.FirstOrDefault()),
                        "settings" => new Views.SettingsWindow(),
                        _ => new Views.AuthenticatorEditorWindow()
                    };
                }

                dialog.Show();
                await RenderSmokeAsync(dialog, dialogPath, 700);
                dialog.Close();
                Shutdown(0);
                return;
            }

            var window = new Views.MainWindow();
            MainWindow = window;
            window.Show();

            if (e.Args.Contains("--smoke"))
            {
                var path = e.Args.SkipWhile(arg => arg != "--smoke").Skip(1).FirstOrDefault()
                           ?? Path.Combine(Environment.CurrentDirectory, "smoke.png");

                await RenderSmokeAsync(window, path, 1800);
                Shutdown(0);
            }
        }

        private static async Task RenderSmokeAsync(Window window, string path, int delayMs)
        {
            // Let the list load and the first tick paint before capturing.
            await Task.Delay(delayMs);
            await window.Dispatcher.InvokeAsync(() =>
            {
                var width = (int) Math.Max(window.ActualWidth, 1);
                var height = (int) Math.Max(window.ActualHeight, 1);
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var stream = File.Create(path);
                encoder.Save(stream);
            });
        }
    }
}

// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Stratum.Core.Entity;
using Stratum.Win.Services;

namespace Stratum.Win.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly DispatcherTimer _timer;
        private string _search;
        private bool _isLoading;
        private string _error;

        public MainViewModel()
        {
            Items.CollectionChanged += (_, __) => OnPropertyChanged(nameof(IsEmpty));
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += (_, __) =>
            {
                foreach (var item in Items)
                {
                    item.Tick();
                }
            };
        }

        public ObservableCollection<AuthenticatorItemViewModel> Items { get; } = new();

        public bool IsEmpty => Items.Count == 0;

        public bool IsLoading
        {
            get => _isLoading;
            private set => Set(ref _isLoading, value);
        }

        public string Error
        {
            get => _error;
            private set
            {
                if (Set(ref _error, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(Error);

        public string Search
        {
            get => _search;
            set
            {
                if (Set(ref _search, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string StatusText
        {
            get
            {
                var count = Items.Count;

                if (IsEmpty && string.IsNullOrEmpty(Search))
                {
                    return "尚无验证器";
                }

                return $"{count} 个验证器";
            }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;

            try
            {
                await AppServices.Initializer;
                await AppServices.AuthenticatorView.LoadFromPersistenceAsync();
                ApplyFilter();
                Error = null;
            }
            catch (Exception exception)
            {
                Error = exception.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void StartTicking()
        {
            _timer.Start();
        }

        private void ApplyFilter()
        {
            var view = AppServices.AuthenticatorView;
            view.Search = Search;

            Items.Clear();

            foreach (var auth in view)
            {
                Items.Add(new AuthenticatorItemViewModel(auth, Settings.CodeGroupSize));
            }

            OnPropertyChanged(nameof(StatusText));
        }

        public async Task<Authenticator> AddAsync(Authenticator auth)
        {
            await AppServices.AuthenticatorService.AddAsync(auth);
            await ReloadAsync();
            return auth;
        }

        public async Task DeleteAsync(AuthenticatorItemViewModel item)
        {
            await AppServices.AuthenticatorService.DeleteWithCategoryBindingsAsync(item.Auth);
            await ReloadAsync();
        }

        public async Task CopyAsync(AuthenticatorItemViewModel item)
        {
            try
            {
                Clipboard.SetText(item.Code);
            }
            catch (Exception)
            {
                // Clipboard can be locked by another process; ignore.
            }

            await AppServices.AuthenticatorService.IncrementCopyCountAsync(item.Auth);
        }

        public async Task IncrementCounterAsync(AuthenticatorItemViewModel item)
        {
            if (!item.IsCounter)
            {
                return;
            }

            await AppServices.AuthenticatorService.IncrementCounterAsync(item.Auth);
            item.RefreshCounter();
        }

        public async Task ReloadAsync()
        {
            await AppServices.AuthenticatorView.LoadFromPersistenceAsync();
            ApplyFilter();
        }
    }
}

// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ObservableCollection<CategoryNode> Categories { get; } = new();

        private CategoryNode _selectedCategory;

        public CategoryNode SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                var previous = _selectedCategory;

                if (!Set(ref _selectedCategory, value) || value == null)
                {
                    return;
                }

                if (previous != null)
                {
                    previous.IsSelected = false;
                }

                value.IsSelected = true;
                AppServices.AuthenticatorView.CategorySelector = value.Selector;
                AppServices.AuthenticatorView.Update();
                ApplyFilter();
            }
        }

        /// <summary>Reloads the sidebar and keeps the current selection when it still exists.</summary>
        public async Task LoadCategoriesAsync()
        {
            var categories = await AppServices.CategoryService.GetAllCategoriesAsync();
            categories.Sort((a, b) =>
            {
                var byRanking = a.Ranking.CompareTo(b.Ranking);
                return byRanking != 0 ? byRanking : string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
            });

            var selected = SelectedCategory;
            _selectedCategory = null;
            Categories.Clear();

            Categories.Add(CategoryNode.All());

            // 未分类 only earns a row when something actually sits in it.
            var authenticators = await AppServices.AuthenticatorRepository.GetAllAsync();
            var bindings = await AppServices.CategoryService.GetAllBindingsAsync();
            var boundSecrets = new HashSet<string>(bindings.Select(binding => binding.AuthenticatorSecret));

            if (authenticators.Any(auth => !boundSecrets.Contains(auth.Secret)))
            {
                Categories.Add(CategoryNode.Uncategorised());
            }

            foreach (var category in categories)
            {
                Categories.Add(CategoryNode.Of(category));
            }

            var restored = selected == null
                ? null
                : Categories.FirstOrDefault(node => node.Selector.Equals(selected.Selector));

            SelectedCategory = restored ?? Categories.First();
            SelectedCategory.IsSelected = true;
            AppServices.AuthenticatorView.CategorySelector = SelectedCategory.Selector;
        }

        public async Task<Category> AddCategoryAsync(string name)
        {
            var category = new Category(name);
            await AppServices.CategoryService.AddCategoryAsync(category);
            await LoadCategoriesAsync();
            return category;
        }

        public async Task RenameCategoryAsync(Category category, string name)
        {
            var next = new Category(name) { Ranking = category.Ranking };

            // TransferAsync keeps bindings when the (hashed) id changes.
            await AppServices.CategoryService.TransferAsync(category, next);
            await LoadCategoriesAsync();
            ApplyFilter();
        }

        public async Task DeleteCategoryAsync(Category category)
        {
            await AppServices.CategoryService.DeleteWithCategoryBindingsASync(category);
            await LoadCategoriesAsync();
            ApplyFilter();
        }

        public async Task<List<Category>> GetAssignedCategoriesAsync(Authenticator auth)
        {
            var bindings = await AppServices.CategoryService.GetBindingsForAuthenticatorAsync(auth);
            var all = await AppServices.CategoryService.GetAllCategoriesAsync();
            var ids = new HashSet<string>(bindings.Select(binding => binding.CategoryId));
            return all.Where(category => ids.Contains(category.Id)).ToList();
        }

        public async Task SetAssignedCategoriesAsync(Authenticator auth, IReadOnlyList<Category> assigned)
        {
            var current = await GetAssignedCategoriesAsync(auth);
            var currentIds = new HashSet<string>(current.Select(category => category.Id));
            var assignedIds = new HashSet<string>(assigned.Select(category => category.Id));

            foreach (var category in assigned.Where(category => !currentIds.Contains(category.Id)))
            {
                await AppServices.CategoryService.AddBindingAsync(auth, category);
            }

            foreach (var category in current.Where(category => !assignedIds.Contains(category.Id)))
            {
                await AppServices.CategoryService.RemoveBindingAsync(auth, category);
            }

            await AppServices.AuthenticatorView.LoadFromPersistenceAsync();
            ApplyFilter();
        }

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
                await LoadCategoriesAsync();
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

        /// <summary>Moves an entry within the current custom order and persists the ranking.</summary>
        public async Task MoveAsync(AuthenticatorItemViewModel item, int delta)
        {
            var view = AppServices.AuthenticatorView;

            if (view.SortMode != Stratum.Core.SortMode.Custom)
            {
                return;
            }

            var index = view.IndexOf(item.Auth);
            var target = index + delta;

            if (index < 0 || target < 0 || target >= view.Count)
            {
                return;
            }

            if (!view.CategorySelector.IsCategory(out _)
                && view.CategorySelector.IsMetaCategory(out var meta)
                && meta == MetaCategory.Uncategorised)
            {
                return;
            }

            view.Swap(index, target);
            view.CommitRanking();

            if (view.CategorySelector.IsCategory(out _))
            {
                await AppServices.CategoryService.UpdateManyBindingsAsync(view.GetCurrentBindings().ToList());
            }
            else
            {
                await AppServices.AuthenticatorService.UpdateManyAsync(view.ToList());
            }

            await ReloadAsync();
        }

        /// <summary>Re-applies preferences (group size, sorting, usernames) after the settings dialog.</summary>
        public void ApplySettings()
        {
            var view = AppServices.AuthenticatorView;
            view.SortMode = Settings.SortMode;
            view.Update();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var view = AppServices.AuthenticatorView;
            view.Search = Search;
            view.SortMode = Settings.SortMode;

            Items.Clear();

            foreach (var auth in view)
            {
                Items.Add(new AuthenticatorItemViewModel(auth, Settings.CodeGroupSize));
            }

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(EmptyTitle));
            OnPropertyChanged(nameof(EmptyHint));
        }

        public string EmptyTitle
        {
            get
            {
                var view = AppServices.AuthenticatorView;

                if (view == null || !view.AnyWithoutFilter())
                {
                    return "尚无验证器";
                }

                return string.IsNullOrEmpty(Search) ? "该分类下暂无验证器" : "没有匹配的验证器";
            }
        }

        public string EmptyHint
        {
            get
            {
                var view = AppServices.AuthenticatorView;

                if (view == null || !view.AnyWithoutFilter())
                {
                    return "点击右上角「添加」手动录入，或用 URI / 二维码图片导入";
                }

                if (!string.IsNullOrEmpty(Search))
                {
                    return "换一个发行方或用户名再试";
                }

                return "在验证器上点右键可以分配分类";
            }
        }

        public async Task<Authenticator> AddAsync(Authenticator auth)
        {
            await AppServices.AuthenticatorService.AddAsync(auth);
            await ReloadAsync();
            return auth;
        }

        public async Task UpdateAsync(Authenticator auth)
        {
            await AppServices.AuthenticatorService.UpdateAsync(auth);
            await ReloadAsync();
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
            await LoadCategoriesAsync();
            ApplyFilter();
        }
    }
}

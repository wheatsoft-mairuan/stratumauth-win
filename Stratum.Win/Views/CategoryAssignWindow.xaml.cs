// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stratum.Core.Entity;

namespace Stratum.Win.Views
{
    public partial class CategoryAssignWindow : Window
    {
        public sealed class Pick
        {
            public Pick(Category category, bool isChecked)
            {
                Category = category;
                IsChecked = isChecked;
            }

            public Category Category { get; }

            public string Name => Category.Name;

            public bool IsChecked { get; set; }
        }

        public CategoryAssignWindow(IEnumerable<Category> all, IEnumerable<Category> assigned)
        {
            InitializeComponent();

            var assignedIds = new HashSet<string>(assigned.Select(category => category.Id));
            var picks = all.Select(category => new Pick(category, assignedIds.Contains(category.Id))).ToList();

            CategoryList.ItemsSource = picks;
            CategoryList.Visibility = picks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            EmptyText.Visibility = picks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Selected = new List<Category>();
        }

        public List<Category> Selected { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Selected = (CategoryList.ItemsSource as IEnumerable<Pick> ?? Enumerable.Empty<Pick>())
                .Where(pick => pick.IsChecked)
                .Select(pick => pick.Category)
                .ToList();

            DialogResult = true;
        }
    }
}

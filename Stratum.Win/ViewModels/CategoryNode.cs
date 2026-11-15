// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Entity;
using Stratum.Win.Services;

namespace Stratum.Win.ViewModels
{
    /// <summary>An entry of the category sidebar: 全部, 未分类, or a user category.</summary>
    public sealed class CategoryNode : ObservableObject
    {
        private bool _isSelected;

        private CategoryNode(string name, CategorySelector selector, Category category)
        {
            Name = name;
            Selector = selector;
            Category = category;
        }

        public string Name { get; }

        public CategorySelector Selector { get; }

        /// <summary>Underlying category, or null for 全部 / 未分类.</summary>
        public Category Category { get; }

        public string Id => Category?.Id;

        public bool IsEditable => Category != null;

        public string Glyph => Category == null
            ? Selector.Is(MetaCategory.Uncategorised) ? "◇" : "▤"
            : string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        public static CategoryNode All()
        {
            return new CategoryNode("全部", CategorySelector.Of(MetaCategory.All), null);
        }

        public static CategoryNode Uncategorised()
        {
            return new CategoryNode("未分类", CategorySelector.Of(MetaCategory.Uncategorised), null);
        }

        public static CategoryNode Of(Category category)
        {
            return new CategoryNode(category.Name, CategorySelector.Of(category.Id), category);
        }
    }
}

// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stratum.Win.Services;

namespace Stratum.Win.Data.Persistence.View.Impl
{
    public class DefaultIconView : IDefaultIconView
    {
        private Dictionary<string, string> _view = new();

        private string _search;

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                Update();
            }
        }

        public bool UseDarkTheme { get; set; }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _view.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _view).GetEnumerator();
        }

        public int Count => _view.Count;

        public KeyValuePair<string, string> this[int index] => _view.ElementAt(index);

        public void Update()
        {
            var catalog = IconCatalog.Instance;
            var keys = catalog.Search(_search);
            _view = new Dictionary<string, string>(keys.Count);

            foreach (var key in keys)
            {
                _view[key] = catalog.ResolvePath(key, UseDarkTheme);
            }
        }
    }
}

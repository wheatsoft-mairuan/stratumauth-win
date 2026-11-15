// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Win.Data.Persistence.View
{
    public interface IReorderableView<out T> : IView<T>
    {
        void Swap(int oldPosition, int newPosition);
    }
}
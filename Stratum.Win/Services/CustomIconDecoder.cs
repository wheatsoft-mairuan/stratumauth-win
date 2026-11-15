// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Util;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Turns imported image bytes into a stored custom icon (port of the Android
    /// <c>CustomIconDecoder</c>, using WPF imaging instead of <c>Android.Graphics</c>).
    /// </summary>
    public class CustomIconDecoder : ICustomIconDecoder
    {
        public Task<CustomIcon> DecodeAsync(byte[] data, bool shouldPreProcess)
        {
            return Task.Run(() =>
            {
                var bitmap = ImageUtil.Decode(data);

                if (shouldPreProcess)
                {
                    bitmap = ImageUtil.TrimTransparent(bitmap);
                    bitmap = ImageUtil.ToSquare(bitmap);
                }

                var size = Math.Min(CustomIcon.MaxSize, Math.Max(1, Math.Min(bitmap.PixelWidth, bitmap.PixelHeight)));
                bitmap = ImageUtil.ScaleTo(bitmap, size);

                var png = ImageUtil.EncodePng(bitmap);
                var hash = HashUtil.Sha1(Convert.ToBase64String(png));

                return new CustomIcon
                {
                    Id = hash.Substring(0, 8),
                    Data = png
                };
            });
        }
    }
}

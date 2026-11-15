// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Stratum.Test
{
    /// <summary>
    /// Windows counterpart of the Android-only <c>Stratum.ZXing</c> reader used by the tests.
    /// Decodes a PNG into a ZXing luminance source through SkiaSharp.
    /// </summary>
    public static class TestQrReader
    {
        public static string ReadPng(byte[] png)
        {
            using var decoded = SKBitmap.Decode(png) ?? throw new InvalidOperationException("Not a valid image");
            using var bitmap = decoded.ColorType == SKColorType.Bgra8888
                ? null
                : decoded.Copy(SKColorType.Bgra8888);

            var source = decoded.ColorType == SKColorType.Bgra8888 ? decoded : bitmap;
            var pixels = source.GetPixelSpan().ToArray();
            var luminance = new RGBLuminanceSource(pixels, source.Width, source.Height,
                RGBLuminanceSource.BitmapFormat.BGRA32);

            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions { TryHarder = true }
            };

            var result = reader.Decode(luminance);
            if (result == null)
            {
                throw new InvalidOperationException("No QR code found in the image");
            }

            return result.Text;
        }
    }
}

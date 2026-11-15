// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using ZXing;
using ZXing.Common;

namespace Stratum.Win.Services
{
    /// <summary>Reads a QR code out of an image file (image import of an otpauth link).</summary>
    public static class QrDecoder
    {
        public static string DecodeFile(string path)
        {
            return Decode(ImageUtil.DecodeFile(path));
        }

        public static string Decode(byte[] data)
        {
            return Decode(ImageUtil.Decode(data));
        }

        private static string Decode(System.Windows.Media.Imaging.BitmapSource bitmap)
        {
            var pixels = ImageUtil.GetBgra32(bitmap, out var width, out var height);
            var source = new RGBLuminanceSource(pixels, width, height, RGBLuminanceSource.BitmapFormat.BGRA32);

            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions { TryHarder = true }
            };

            var result = reader.Decode(source);

            if (result?.Text == null)
            {
                throw new InvalidDataException("图片中找不到可识别的二维码");
            }

            return result.Text;
        }
    }
}

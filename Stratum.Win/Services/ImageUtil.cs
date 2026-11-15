// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Stratum.Win.Services
{
    /// <summary>
    /// Small WPF imaging helpers: everything the app needs from images is decoded,
    /// cropped, padded and re-encoded with WIC, so no extra native dependency is required.
    /// </summary>
    public static class ImageUtil
    {
        public static BitmapSource Decode(byte[] data)
        {
            using var stream = new MemoryStream(data);
            return Decode(stream);
        }

        public static BitmapSource DecodeFile(string path)
        {
            using var stream = File.OpenRead(path);
            return Decode(stream);
        }

        private static BitmapSource Decode(Stream stream)
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count == 0)
            {
                throw new ArgumentException("图片中没有可用的画面");
            }

            BitmapSource frame = decoder.Frames[0];

            if (frame.Format != PixelFormats.Bgra32)
            {
                frame = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            }

            if (frame.CanFreeze)
            {
                frame.Freeze();
            }

            return frame;
        }

        /// <summary>Raw BGRA32 pixels of a bitmap.</summary>
        public static byte[] GetBgra32(BitmapSource source, out int width, out int height)
        {
            BitmapSource bitmap = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            width = bitmap.PixelWidth;
            height = bitmap.PixelHeight;

            var stride = width * 4;
            var buffer = new byte[stride * height];
            bitmap.CopyPixels(buffer, stride, 0);
            return buffer;
        }

        public static byte[] EncodePng(BitmapSource source)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        /// <summary>Crops fully transparent borders, if there are any.</summary>
        public static BitmapSource TrimTransparent(BitmapSource source)
        {
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var pixels = GetBgra32(source, out _, out _);

            var left = width;
            var right = -1;
            var top = height;
            var bottom = -1;

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    if (pixels[(y * width + x) * 4 + 3] == 0)
                    {
                        continue;
                    }

                    if (x < left)
                    {
                        left = x;
                    }

                    if (x > right)
                    {
                        right = x;
                    }

                    if (y < top)
                    {
                        top = y;
                    }

                    bottom = y;
                }
            }

            if (right < 0 || bottom < 0)
            {
                return source;
            }

            var cropWidth = right - left + 1;
            var cropHeight = bottom - top + 1;

            if (cropWidth == width && cropHeight == height)
            {
                return source;
            }

            var cropped = new CroppedBitmap(source, new Int32Rect(left, top, cropWidth, cropHeight));
            cropped.Freeze();
            return cropped;
        }

        /// <summary>Centres the image on a transparent square.</summary>
        public static BitmapSource ToSquare(BitmapSource source)
        {
            if (source.PixelWidth == source.PixelHeight)
            {
                return source;
            }

            var side = Math.Max(source.PixelWidth, source.PixelHeight);
            var x = (side - source.PixelWidth) / 2.0;
            var y = (side - source.PixelHeight) / 2.0;

            return Render(side, side, context => context.DrawImage(source, new Rect(x, y, source.PixelWidth,
                source.PixelHeight)));
        }

        /// <summary>Scales the image down so that its longest side is at most <paramref name="maxSize" />.</summary>
        public static BitmapSource ScaleTo(BitmapSource source, int maxSize)
        {
            var longest = Math.Max(source.PixelWidth, source.PixelHeight);

            if (longest <= maxSize)
            {
                return source;
            }

            var ratio = (double) maxSize / longest;
            var width = Math.Max(1, (int) Math.Round(source.PixelWidth * ratio));
            var height = Math.Max(1, (int) Math.Round(source.PixelHeight * ratio));

            return Render(width, height, context => context.DrawImage(source, new Rect(0, 0, width, height)));
        }

        private static BitmapSource Render(int width, int height, Action<DrawingContext> draw)
        {
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                draw(context);
            }

            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            converted.Freeze();
            return converted;
        }
    }
}

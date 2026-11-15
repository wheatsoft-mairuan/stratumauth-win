// Copyright (C) 2026 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Stratum.Win.Converters
{
    /// <summary>
    /// Converts a 0..1 remaining fraction into a circular arc geometry.
    /// ConverterParameter is the diameter in pixels.
    /// </summary>
    public sealed class ArcGeometryConverter : IValueConverter
    {
        private const double Thickness = 3;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var progress = value is double fraction ? Math.Clamp(fraction, 0, 1) : 0;
            var size = 24d;

            if (parameter != null &&
                double.TryParse(parameter.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                size = parsed;
            }

            var center = size / 2;
            var radius = center - Thickness / 2;

            if (progress >= 0.9999)
            {
                return new EllipseGeometry(new Point(center, center), radius, radius);
            }

            if (progress <= 0.0001)
            {
                return Geometry.Empty;
            }

            var start = new Point(center, center - radius);
            var angle = progress * 2 * Math.PI;
            var end = new Point(
                center + radius * Math.Sin(angle),
                center - radius * Math.Cos(angle));

            var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = progress > 0.5
            });

            return new PathGeometry(new[] { figure });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

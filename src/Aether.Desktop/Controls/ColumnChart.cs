using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Aether.Core.Models;

namespace Aether.Desktop.Controls;

/// <summary>Vertical bar chart for a time series (for example incidents per month).</summary>
public class ColumnChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable<LabelCount>), typeof(ColumnChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        nameof(BarBrush), typeof(Brush), typeof(ColumnChart),
        new FrameworkPropertyMetadata(Brushes.Teal, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<LabelCount>? Items { get => (IEnumerable<LabelCount>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public Brush BarBrush { get => (Brush)GetValue(BarBrushProperty); set => SetValue(BarBrushProperty, value); }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 200 : availableSize.Height);

    protected override void OnRender(DrawingContext dc)
    {
        var items = Items?.ToList() ?? new List<LabelCount>();
        if (items.Count == 0 || ActualWidth < 40 || ActualHeight < 60) return;

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = (Brush)(Application.Current.TryFindResource("TextBrush") ?? Brushes.Black);
        var muted = (Brush)(Application.Current.TryFindResource("MutedBrush") ?? Brushes.Gray);
        var grid = new Pen((Brush)(Application.Current.TryFindResource("BorderBrushSoft") ?? Brushes.LightGray), 1);

        const double leftPad = 30, bottomPad = 34, topPad = 14;
        var plotW = ActualWidth - leftPad;
        var plotH = ActualHeight - bottomPad - topPad;
        var max = Math.Max(1, items.Max(i => i.Count));

        dc.DrawLine(grid, new Point(leftPad, topPad + plotH), new Point(ActualWidth, topPad + plotH));
        var maxLabel = Format(max.ToString(CultureInfo.CurrentCulture), muted, dpi, 11);
        dc.DrawText(maxLabel, new Point(leftPad - maxLabel.Width - 4, topPad - 6));
        dc.DrawLine(grid, new Point(leftPad, topPad), new Point(ActualWidth, topPad));

        var slot = plotW / items.Count;
        var barW = Math.Max(4, slot * 0.6);
        for (var i = 0; i < items.Count; i++)
        {
            var h = plotH * items[i].Count / max;
            var x = leftPad + i * slot + (slot - barW) / 2;
            dc.DrawRectangle(BarBrush, null, new Rect(x, topPad + plotH - h, barW, h));

            var value = Format(items[i].Count.ToString(CultureInfo.CurrentCulture), text, dpi, 11);
            if (slot >= 22)
                dc.DrawText(value, new Point(x + (barW - value.Width) / 2, topPad + plotH - h - value.Height - 1));

            // Month labels are thinned out when there is not enough room.
            var every = (int)Math.Ceiling(44 / slot);
            if (i % every == 0)
            {
                var label = Format(ShortMonth(items[i].Label), muted, dpi, 11);
                dc.DrawText(label, new Point(x + barW / 2 - label.Width / 2, topPad + plotH + 6));
            }
        }
    }

    /// <summary>"2026-04" becomes "Apr 26".</summary>
    private static string ShortMonth(string yyyyMm) =>
        DateTime.TryParseExact(yyyyMm + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("MMM yy", CultureInfo.CurrentCulture)
            : yyyyMm;

    private static FormattedText Format(string s, Brush brush, double dpi, double size) =>
        new(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, dpi);
}

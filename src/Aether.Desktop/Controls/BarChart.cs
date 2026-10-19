using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Aether.Core.Models;

namespace Aether.Desktop.Controls;

/// <summary>
/// Horizontal bar chart drawn directly (no chart library). Each row is a label, a bar and the value.
/// Set <see cref="Items"/> from query results only.
/// </summary>
public class BarChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable<LabelCount>), typeof(BarChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        nameof(BarBrush), typeof(Brush), typeof(BarChart),
        new FrameworkPropertyMetadata(Brushes.Teal, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth), typeof(double), typeof(BarChart),
        new FrameworkPropertyMetadata(140.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private const double RowHeight = 26;

    public IEnumerable<LabelCount>? Items { get => (IEnumerable<LabelCount>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public Brush BarBrush { get => (Brush)GetValue(BarBrushProperty); set => SetValue(BarBrushProperty, value); }
    public double LabelWidth { get => (double)GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var rows = Items?.Count() ?? 0;
        var width = double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width;
        return new Size(width, Math.Max(rows, 1) * RowHeight);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var items = Items?.ToList() ?? new List<LabelCount>();
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = (Brush)(Application.Current.TryFindResource("TextBrush") ?? Brushes.Black);
        var max = items.Count == 0 ? 0 : items.Max(i => i.Count);
        const double valueWidth = 48;
        var barArea = Math.Max(10, ActualWidth - LabelWidth - valueWidth - 8);

        for (var i = 0; i < items.Count; i++)
        {
            var y = i * RowHeight;
            var label = Format(items[i].Label, text, dpi, 12);
            label.MaxTextWidth = LabelWidth - 8;
            label.MaxLineCount = 1;
            label.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(label, new Point(0, y + (RowHeight - label.Height) / 2));

            var w = max == 0 ? 0 : barArea * items[i].Count / max;
            dc.DrawRoundedRectangle(BarBrush, null, new Rect(LabelWidth, y + 5, Math.Max(w, items[i].Count > 0 ? 2 : 0), RowHeight - 10), 2, 2);

            var value = Format(items[i].Count.ToString("N0", CultureInfo.CurrentCulture), text, dpi, 12);
            dc.DrawText(value, new Point(LabelWidth + Math.Max(w, 0) + 6, y + (RowHeight - value.Height) / 2));
        }
    }

    private static FormattedText Format(string s, Brush brush, double dpi, double size) =>
        new(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, brush, dpi);
}

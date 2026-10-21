using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Aether.Desktop.Controls;

public record ChartPoint(DateTime X, double Y);

/// <summary>
/// Line chart of recorded measurements with optional alarm and trip limit lines.
/// Draws only the points it is given; nothing is interpolated or extrapolated.
/// </summary>
public class LineChart : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IEnumerable<ChartPoint>), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty AlarmLimitProperty = DependencyProperty.Register(
        nameof(AlarmLimit), typeof(double?), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TripLimitProperty = DependencyProperty.Register(
        nameof(TripLimit), typeof(double?), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(LineChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty AreaBrushProperty = DependencyProperty.Register(
        nameof(AreaBrush), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<ChartPoint>? Points { get => (IEnumerable<ChartPoint>?)GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public double? AlarmLimit { get => (double?)GetValue(AlarmLimitProperty); set => SetValue(AlarmLimitProperty, value); }
    public double? TripLimit { get => (double?)GetValue(TripLimitProperty); set => SetValue(TripLimitProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public Brush? AreaBrush { get => (Brush?)GetValue(AreaBrushProperty); set => SetValue(AreaBrushProperty, value); }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 500 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 260 : availableSize.Height);

    protected override void OnRender(DrawingContext dc)
    {
        var pts = Points?.OrderBy(p => p.X).ToList() ?? new List<ChartPoint>();
        if (pts.Count == 0 || ActualWidth < 120 || ActualHeight < 80) return;

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        Brush Res(string key, Brush fallback) => (Brush)(Application.Current.TryFindResource(key) ?? fallback);
        var muted = Res("MutedBrush", Brushes.Gray);
        var accent = Res(AreaBrush is null ? "AccentBrush" : "VioletBrush", Brushes.Teal);
        var high = Res("AmberBrush", Brushes.OrangeRed);
        var critical = Res("DangerBrush", Brushes.Red);
        var gridPen = new Pen(Res("BorderBrushSoft", Brushes.LightGray), 1);

        const double left = 56, right = 14, top = 12, bottom = 30;
        var plotW = ActualWidth - left - right;
        var plotH = ActualHeight - top - bottom;

        var values = pts.Select(p => p.Y).ToList();
        if (AlarmLimit is { } a) values.Add(a);
        if (TripLimit is { } t) values.Add(t);
        var min = values.Min();
        var max = values.Max();
        if (max - min < 1e-9) { max += 1; min -= 1; }
        var pad = (max - min) * 0.08;
        if (AreaBrush is null) { min -= pad; max += pad; }
        else { min = 0; max = Math.Max(5, Math.Ceiling(max / 5) * 5); }

        var t0 = pts[0].X;
        var span = (pts[^1].X - t0).TotalDays;
        if (span <= 0) span = 1;

        double X(DateTime d) => left + plotW * (d - t0).TotalDays / span;
        double Y(double v) => top + plotH * (1 - (v - min) / (max - min));

        // Horizontal grid with value labels
        for (var i = 0; i <= 4; i++)
        {
            var v = min + (max - min) * i / 4;
            var y = Y(v);
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotW, y));
            var label = Format(v.ToString("0.##", CultureInfo.CurrentCulture), muted, dpi, 11);
            dc.DrawText(label, new Point(left - label.Width - 6, y - label.Height / 2));
        }

        DrawLimit(dc, AlarmLimit, "Alarm", high, left, plotW, Y, dpi);
        DrawLimit(dc, TripLimit, "Trip", critical, left, plotW, Y, dpi);

        if (AreaBrush is not null)
        {
            var area = new StreamGeometry();
            using (var ctx = area.Open())
            {
                ctx.BeginFigure(new Point(X(pts[0].X), top + plotH), true, true);
                foreach (var p in pts) ctx.LineTo(new Point(X(p.X), Y(p.Y)), true, false);
                ctx.LineTo(new Point(X(pts[^1].X), top + plotH), true, false);
            }
            dc.DrawGeometry(AreaBrush, null, area);
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(X(pts[0].X), Y(pts[0].Y)), false, false);
            foreach (var p in pts.Skip(1)) ctx.LineTo(new Point(X(p.X), Y(p.Y)), true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(accent, 2), geometry);
        foreach (var p in pts)
            dc.DrawEllipse(AreaBrush is null ? accent : Brushes.White,
                AreaBrush is null ? null : new Pen(Brushes.DimGray, 2), new Point(X(p.X), Y(p.Y)), 3, 3);

        if (AreaBrush is not null)
        {
            var every = Math.Max(1, (int)Math.Ceiling(pts.Count * 62 / plotW));
            for (var i = 0; i < pts.Count; i += every)
            {
                var label = Format(pts[i].X.ToString("MMM yy", CultureInfo.CurrentCulture), muted, dpi, 11);
                dc.DrawText(label, new Point(Math.Clamp(X(pts[i].X) - label.Width / 2, left, left + plotW - label.Width), top + plotH + 8));
            }
            return;
        }

        var first = Format(pts[0].X.ToString("d MMM yyyy", CultureInfo.CurrentCulture), muted, dpi, 11);
        var last = Format(pts[^1].X.ToString("d MMM yyyy", CultureInfo.CurrentCulture), muted, dpi, 11);
        dc.DrawText(first, new Point(left, top + plotH + 8));
        dc.DrawText(last, new Point(left + plotW - last.Width, top + plotH + 8));
        if (!string.IsNullOrEmpty(Unit))
        {
            var unit = Format(Unit, muted, dpi, 11);
            dc.DrawText(unit, new Point(left + plotW / 2 - unit.Width / 2, top + plotH + 8));
        }
    }

    private static void DrawLimit(DrawingContext dc, double? limit, string name, Brush brush,
        double left, double plotW, Func<double, double> y, double dpi)
    {
        if (limit is not { } v) return;
        var pen = new Pen(brush, 1.5) { DashStyle = DashStyles.Dash };
        var yy = y(v);
        dc.DrawLine(pen, new Point(left, yy), new Point(left + plotW, yy));
        var label = Format($"{name} {v.ToString("0.##", CultureInfo.CurrentCulture)}", brush, dpi, 11);
        dc.DrawText(label, new Point(left + plotW - label.Width - 2, yy - label.Height - 1));
    }

    private static FormattedText Format(string s, Brush brush, double dpi, double size) =>
        new(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, dpi);
}

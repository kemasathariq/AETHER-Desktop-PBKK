using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Aether.Core.Models;

namespace Aether.Desktop.Common;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (Invert) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Shows an element only when the text is not empty.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Maps a priority category or status text to a badge brush from the theme.</summary>
public sealed class BadgeBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value switch
        {
            PriorityCategory.Critical => "CriticalBrush",
            PriorityCategory.High => "HighBrush",
            PriorityCategory.Medium => "MediumBrush",
            PriorityCategory.Low => "LowBrush",
            PriorityCategory.NotRated => "NeutralBrush",
            ActionStatus.Open or IncidentStatuses.NewRegistered => "OpenBadgeBrush",
            ActionStatus.InProgress or IncidentStatuses.RcaProcess => "MediumBrush",
            IncidentStatuses.CaPaExecution => "HighBrush",
            IncidentStatuses.MonitoringResult => "MonitoringBadgeBrush",
            ActionStatus.Closed or IncidentStatuses.RiskClosed or "NORMAL" => "LowBrush",
            "TRIP" => "CriticalBrush",
            "ALARM" => "HighBrush",
            _ => "NeutralBrush",
        };
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

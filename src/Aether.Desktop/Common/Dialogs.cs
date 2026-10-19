using System.Windows;

namespace Aether.Desktop.Common;

/// <summary>Thin wrapper around message boxes so view models stay readable.</summary>
public static class Dialogs
{
    public static bool ConfirmDelete(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)
        == MessageBoxResult.Yes;
}

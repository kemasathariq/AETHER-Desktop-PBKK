using System.Windows;
using Aether.Core;
using Aether.Desktop.Common;
using Aether.Desktop.ViewModels;

namespace Aether.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Something unexpected went wrong:\n\n" + args.Exception.Message
                + "\n\nYour saved data is not affected. If this keeps happening, restart the application.",
                "AETHER Desktop", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppServices services;
        try
        {
            services = AppServices.Create();
        }
        catch (AetherException ex)
        {
            MessageBox.Show(
                "AETHER Desktop could not prepare its database.\n\n" + ex.Message
                + "\n\nSee the README (Troubleshooting) for how to reset the local database.",
                "AETHER Desktop", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var window = new MainWindow { DataContext = new MainViewModel(services) };
        MainWindow = window;
        window.Show();
    }
}

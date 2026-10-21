using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aether.Core.Models;
using Aether.Desktop.Common;
using Aether.Desktop.ViewModels;
using Aether.Desktop.Views;
using Aether.Infrastructure.Data;
using Aether.Infrastructure.Repositories;
using Aether.Infrastructure.Seed;
using Aether.Infrastructure.Tests;
using Xunit;

namespace Aether.Desktop.Tests;

public class DesktopAcceptanceTests
{
    private static AppServices Services(AetherDatabase db, bool seed = true) => new()
    {
        DatabasePath = db.DatabasePath,
        SeedReport = seed ? SourceDataImporter.EnsureSeeded(db) : SeedReport.AlreadyApplied,
        Incidents = new SqliteIncidentRepository(db),
        Actions = new SqliteActionRepository(db),
        Assets = new SqliteAssetRepository(db),
        Dashboard = new SqliteDashboardRepository(db),
    };

    [Fact]
    public void SelectingIncident_NotifiesTheEmptySelectionPanel()
    {
        using var db = new TestDb();
        var vm = new IncidentsViewModel(Services(db.Db));
        vm.Refresh();
        var changes = new List<string?>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        vm.Selected = vm.Items[0];
        Assert.Contains(nameof(vm.ShowNoSelection), changes);
        Assert.False(vm.ShowNoSelection);
    }

    [Fact]
    public void SavedIncidentHiddenByFilters_ReportsItEvenIfAnotherRowIsSelected()
    {
        using var db = new TestDb();
        var vm = new IncidentsViewModel(Services(db.Db));
        vm.Refresh();
        vm.SearchText = "KO-3201";
        vm.Selected = Assert.Single(vm.Items);
        vm.NewCommand.Execute(null);
        vm.Form!.Title = "Acceptance incident";
        vm.Form.TagNumber = "ACCEPT-1";
        vm.Form.Plant = "Test";
        vm.SaveCommand.Execute(null);
        Assert.False(vm.IsError);
        Assert.Contains("hidden by the current filters", vm.Message);
    }

    [Fact]
    public void WindowsPages_RenderAndFormsPersistUsingSQLite() => OnSta(() =>
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        var app = new App();
        app.InitializeComponent();
        using var db = new TestDb();
        var services = Services(db.Db);
        var errors = new StringWriter();
        var listener = new TextWriterTraceListener(errors);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        try
        {
            var main = new MainViewModel(services);
            var window = new MainWindow { DataContext = main };
            var root = (FrameworkElement)window.Content;
            ((Grid)root).Background = window.Background;
            PumpUntil(() => !((DashboardViewModel)main.CurrentPage).IsLoading);
            Assert.Equal("380", ((DashboardViewModel)main.CurrentPage).TotalIncidentsText);

            foreach (var nav in main.Items)
            {
                main.SelectedItem = nav;
                Layout(root, 1360, 820);
                Assert.False(nav.Page.IsError, nav.Page.Message);
                Snapshot(root, nav.Title.Replace(" ", ""));
                Layout(root, 1100, 680);
                Snapshot(root, nav.Title.Replace(" ", "") + "-minimum");
                foreach (var table in Find<DataGrid>(root))
                    foreach (var column in table.Columns.Where(c => c.Width.IsAbsolute))
                        Assert.True(column.ActualWidth >= column.Width.Value - 1,
                            $"{nav.Title}: {column.Header} shrank from {column.Width.Value} to {column.ActualWidth}.");
            }

            var incidents = (IncidentsViewModel)main.Items[1].Page;
            main.SelectedItem = main.Items[1];
            incidents.Selected = incidents.Items.First(i => i.TagNumber == "KO-3201");
            Layout(root);
            Assert.Equal(Visibility.Collapsed, Find<StackPanel>(root)
                .Single(p => p.Children.OfType<TextBlock>().Any(t => t.Text == "No incident selected")).Visibility);
            Snapshot(root, "IncidentDetails");
            incidents.NewCommand.Execute(null);
            incidents.SaveCommand.Execute(null);
            Assert.True(incidents.Errors.Any);
            Assert.Equal(380, services.Incidents.Search().Count);
            incidents.Form!.Title = "Acceptance incident";
            incidents.Form.TagNumber = "ACCEPT-1";
            incidents.Form.Plant = "Test";
            incidents.Form.DowntimeText = "abc";
            incidents.SaveCommand.Execute(null);
            Assert.NotEmpty(incidents.Errors[nameof(Incident.DowntimeHours)]);
            incidents.Form.DowntimeText = "-5";
            incidents.SaveCommand.Execute(null);
            Assert.NotEmpty(incidents.Errors[nameof(Incident.DowntimeHours)]);
            incidents.Form.DowntimeText = "10,5";
            incidents.Form.ActualLossText = "100";
            incidents.SaveCommand.Execute(null);
            Assert.False(incidents.IsError, incidents.Message);
            var saved = Assert.Single(services.Incidents.Search(), i => i.TagNumber == "ACCEPT-1");
            Assert.Equal(10.5, saved.DowntimeHours);
            incidents.EditCommand.Execute(null);
            incidents.Form!.Title = "Updated acceptance incident";
            incidents.SaveCommand.Execute(null);
            Assert.Equal("Updated acceptance incident", services.Incidents.GetById(saved.Id)!.Title);

            incidents.SearchText = "zzzz-no-match";
            Assert.True(incidents.IsListEmpty);
            Layout(root);
            Snapshot(root, "IncidentsEmptySearch");
            incidents.ClearFiltersCommand.Execute(null);
            Assert.Equal(381, incidents.Items.Count);

            var actions = (ActionsViewModel)main.Items[4].Page;
            main.SelectedItem = main.Items[4];
            actions.NewCommand.Execute(null);
            actions.SaveCommand.Execute(null);
            Assert.True(actions.Errors.Any);
            actions.Form!.IncidentId = saved.Id;
            actions.Form.Description = "Check bearing";
            actions.Form.Owner = "Acceptance";
            actions.SaveCommand.Execute(null);
            Assert.False(actions.IsError, actions.Message);
            var action = Assert.Single(actions.Items, a => a.IncidentId == saved.Id);
            actions.Selected = action;
            actions.EditCommand.Execute(null);
            actions.Form!.Status = ActionStatus.InProgress;
            actions.SaveCommand.Execute(null);
            Assert.Equal(ActionStatus.InProgress, services.Actions.GetById(action.Id)!.Status);
            actions.NewCommand.Execute(null);
            Layout(root);
            Snapshot(root, "ActionForm");
            actions.CancelCommand.Execute(null);

            var reopened = Services(new AetherDatabase(db.Path_));
            Assert.Equal(381, reopened.Incidents.Search().Count);
            Assert.Equal(ActionStatus.InProgress, reopened.Actions.GetById(action.Id)!.Status);
            var dashboard = (DashboardViewModel)main.Items[0].Page;
            main.SelectedItem = main.Items[0];
            PumpUntil(() => !dashboard.IsLoading);
            Assert.Equal("381", dashboard.TotalIncidentsText);
            Assert.Equal("21", dashboard.ActionsText);

            var assets = (AssetsViewModel)main.Items[2].Page;
            main.SelectedItem = main.Items[2];
            Assert.Equal(5, assets.Assets.Count);
            foreach (var asset in assets.Assets.ToArray())
            {
                assets.SelectedAsset = asset;
                foreach (var parameter in assets.Parameters.ToArray())
                {
                    assets.SelectedParameter = parameter;
                    Assert.Equal(26, assets.Records.Count);
                    Layout(root);
                }
            }
            Snapshot(root, "AssetMeasurements");

            var priority = (PriorityViewModel)main.Items[3].Page;
            main.SelectedItem = main.Items[3];
            priority.SearchText = "KO-3201";
            priority.Selected = Assert.Single(priority.Rows);
            Assert.Equal(PriorityCategory.Critical, priority.Selected.Category);
            Layout(root);
            Snapshot(root, "PriorityDetails");
            services.Actions.Delete(action.Id);
            Assert.Null(reopened.Actions.GetById(action.Id));
            Assert.Equal(0, services.Incidents.Delete(saved.Id));
            Assert.Null(reopened.Incidents.GetById(saved.Id));

            using var emptyDb = new TestDb();
            var empty = Services(emptyDb.Db, seed: false);
            foreach (var page in new PageViewModel[]
                     { new IncidentsViewModel(empty), new ActionsViewModel(empty), new AssetsViewModel(empty), new PriorityViewModel(empty) })
            {
                page.Refresh();
                Assert.False(page.IsError);
            }
            var emptyDashboard = new DashboardViewModel(empty);
            emptyDashboard.Refresh();
            PumpUntil(() => !emptyDashboard.IsLoading);
            Assert.True(emptyDashboard.ShowEmptyMessage);
            listener.Flush();
            Assert.True(string.IsNullOrEmpty(errors.ToString()), errors.ToString());
        }
        finally
        {
            PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            app.Shutdown();
        }
    });

    private static void OnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            try { action(); }
            catch (Exception ex) { error = ex; }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "WPF acceptance check timed out.");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static void PumpUntil(Func<bool> done)
    {
        var timeout = Stopwatch.StartNew();
        while (!done())
        {
            Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(10), "WPF operation timed out.");
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Thread.Sleep(10);
        }
    }

    private static void Layout(FrameworkElement root, double width = 1360, double height = 820)
    {
        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        root.UpdateLayout();
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }

    private static void Snapshot(FrameworkElement root, string name)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(folder, name + ".png"));
        encoder.Save(file);
    }
}

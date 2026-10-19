using Aether.Desktop.Common;

namespace Aether.Desktop.ViewModels;

public sealed record NavItem(string Title, string Glyph, PageViewModel Page);

public sealed class MainViewModel : ObservableObject
{
    private NavItem _selectedItem;

    public MainViewModel(AppServices services)
    {
        Items = new[]
        {
            new NavItem("Dashboard", "", new DashboardViewModel(services)),
            new NavItem("Incidents", "", new IncidentsViewModel(services)),
            new NavItem("Asset Monitoring", "", new AssetsViewModel(services)),
            new NavItem("Incident Priority", "", new PriorityViewModel(services)),
            new NavItem("Action Tracker", "", new ActionsViewModel(services)),
        };
        DataNote = "Local database:\n" + services.DatabasePath
            + "\n\nData is the supplied case data plus your own entries. This is not live plant monitoring.";
        _selectedItem = Items[0];
        _selectedItem.Page.Refresh();
    }

    public IReadOnlyList<NavItem> Items { get; }
    public string DataNote { get; }

    public NavItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value is null || !Set(ref _selectedItem, value)) return;
            value.Page.Refresh();           // every visit shows the stored data
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    public PageViewModel CurrentPage => _selectedItem.Page;
}

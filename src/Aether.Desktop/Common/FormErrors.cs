using System.ComponentModel;

namespace Aether.Desktop.Common;

/// <summary>
/// Field-name to message map that XAML can bind to with <c>{Binding Errors[Title]}</c>.
/// Missing fields return an empty string. Replace all errors at once with <see cref="Set"/>.
/// </summary>
public sealed class FormErrors : INotifyPropertyChanged
{
    private Dictionary<string, string> _errors = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string field] => _errors.TryGetValue(field, out var m) ? m : string.Empty;

    public bool Any => _errors.Count > 0;

    public void Set(IReadOnlyDictionary<string, string>? errors)
    {
        _errors = errors is null ? new() : new Dictionary<string, string>(errors);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Any)));
    }

    public void Clear() => Set(null);
}

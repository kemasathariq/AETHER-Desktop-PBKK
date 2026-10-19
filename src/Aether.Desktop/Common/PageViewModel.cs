namespace Aether.Desktop.Common;

/// <summary>Base for the five screens. Shows a status line with a success or error message.</summary>
public abstract class PageViewModel : ObservableObject
{
    private string _message = string.Empty;
    private bool _isError;

    public abstract string Title { get; }

    /// <summary>Called whenever the page is shown, so it always reflects the stored data.</summary>
    public abstract void Refresh();

    public string Message { get => _message; private set { Set(ref _message, value); OnPropertyChanged(nameof(HasMessage)); } }
    public bool IsError { get => _isError; private set => Set(ref _isError, value); }
    public bool HasMessage => !string.IsNullOrEmpty(_message);

    protected void ShowSuccess(string message) { IsError = false; Message = message; }
    protected void ShowError(string message) { IsError = true; Message = message; }
    protected void ClearMessage() { IsError = false; Message = string.Empty; }

    /// <summary>Runs a database action; converts known errors into the status line.</summary>
    protected bool TryRun(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Aether.Core.ValidationException ex)
        {
            ShowError(ex.Message);
            return false;
        }
        catch (Aether.Core.AetherException ex)
        {
            ShowError(ex.Message);
            return false;
        }
    }
}

namespace Aether.Core;

/// <summary>
/// Base type for errors whose message is safe and meant to be shown to the user.
/// Infrastructure wraps low-level database errors in this type.
/// </summary>
public class AetherException : Exception
{
    public AetherException(string message) : base(message) { }
    public AetherException(string message, Exception inner) : base(message, inner) { }
}

public class NotFoundException : AetherException
{
    public NotFoundException(string entity, int id)
        : base($"{entity} with ID {id} no longer exists. Refresh the list and try again.") { }
}

public class ValidationException : AetherException
{
    public IReadOnlyDictionary<string, string> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string> errors)
        : base("The data is not valid: " + string.Join("; ", errors.Values))
    {
        Errors = errors;
    }
}

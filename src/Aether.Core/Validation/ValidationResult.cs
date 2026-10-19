namespace Aether.Core.Validation;

/// <summary>Field name to message. An empty result is valid.</summary>
public class ValidationResult
{
    private readonly Dictionary<string, string> _errors = new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyDictionary<string, string> Errors => _errors;

    public void Add(string field, string message) => _errors.TryAdd(field, message);

    public void ThrowIfInvalid()
    {
        if (!IsValid) throw new ValidationException(_errors);
    }
}

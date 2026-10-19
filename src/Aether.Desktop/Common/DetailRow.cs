namespace Aether.Desktop.Common;

/// <summary>A label and value pair for read-only detail panels. Missing values show as "Not recorded".</summary>
public sealed record DetailRow(string Label, string Value)
{
    public const string NotRecorded = "Not recorded";

    public static DetailRow Of(string label, string? value) =>
        new(label, string.IsNullOrWhiteSpace(value) ? NotRecorded : value);

    public static DetailRow Of(string label, double? value, string unit = "", string format = "#,0.##") =>
        new(label, value is null ? NotRecorded : value.Value.ToString(format) + (unit.Length > 0 ? " " + unit : ""));

    public static DetailRow Of(string label, DateTime? value) =>
        new(label, value is null ? NotRecorded : value.Value.ToString("d MMM yyyy"));
}

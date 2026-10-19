using System.Globalization;
using Aether.Core;
using Microsoft.Data.Sqlite;

namespace Aether.Infrastructure.Data;

/// <summary>Small helpers for reading and writing nullable values and ISO dates.</summary>
internal static class SqlHelpers
{
    public const string DateFormat = "yyyy-MM-dd";

    public static object ToDb(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    public static object ToDb(double? value) => value.HasValue ? value.Value : DBNull.Value;

    public static object ToDb(int? value) => value.HasValue ? value.Value : DBNull.Value;

    public static object ToDb(DateTime? value) =>
        value.HasValue ? value.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : DBNull.Value;

    public static string? GetString(SqliteDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public static string GetRequiredString(SqliteDataReader r, string column) =>
        GetString(r, column) ?? string.Empty;

    public static double? GetDouble(SqliteDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetDouble(i);
    }

    public static int? GetInt(SqliteDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetInt32(i);
    }

    /// <summary>Returns null for NULL or unparseable text so a bad stored value cannot crash a screen.</summary>
    public static DateTime? GetDate(SqliteDataReader r, string column)
    {
        var text = GetString(r, column);
        return TryParseDate(text);
    }

    public static DateTime? TryParseDate(string? text) =>
        DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;

    /// <summary>Escapes %, _ and \ for a LIKE pattern that uses ESCAPE '\'.</summary>
    public static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>Runs a database operation and converts SQLite errors into user-readable exceptions.</summary>
    public static T Run<T>(string operation, Func<T> work)
    {
        try
        {
            return work();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // constraint violation
        {
            throw new AetherException($"{operation} was rejected by the database: {ex.Message}", ex);
        }
        catch (SqliteException ex)
        {
            throw new AetherException($"{operation} failed because of a database error: {ex.Message}", ex);
        }
    }

    public static void Run(string operation, Action work) =>
        Run<object?>(operation, () => { work(); return null; });
}

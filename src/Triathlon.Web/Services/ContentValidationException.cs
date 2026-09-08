namespace Triathlon.Web.Services;

/// <summary>
/// A dashboard save was refused because one field is not acceptable. <see cref="Key"/> names the
/// entry in <c>DashboardStrings.*.resx</c> the dashboard renders next to <see cref="Field"/>, with
/// <see cref="Arguments"/> as its format values; <see cref="Exception.Message"/> is the English
/// fallback for logs and tests.
/// </summary>
public sealed class ContentValidationException(string field, string key, params object[] arguments)
    : Exception(key + (arguments.Length == 0 ? "" : " (" + string.Join(", ", arguments) + ")"))
{
    public string Field { get; } = field;

    /// <summary>A key in <c>Resources/DashboardStrings.*.resx</c>, prefixed <c>Validation_</c>.</summary>
    public string Key { get; } = key;

    public IReadOnlyList<object> Arguments { get; } = arguments;
}

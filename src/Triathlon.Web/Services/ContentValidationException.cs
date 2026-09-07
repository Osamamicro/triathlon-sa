namespace Triathlon.Web.Services;

/// <summary>A dashboard save was refused because one field is not acceptable. The dashboard shows <see cref="Message"/> next to <see cref="Field"/>.</summary>
public sealed class ContentValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

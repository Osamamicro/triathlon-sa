namespace Triathlon.Web.Services;

/// <summary>
/// The other half of F1's fix: every bounded <c>HasMaxLength</c> column in <c>Data/Configurations/*</c>
/// needs a matching check in the write service that fills it, or a value one character too long
/// reaches <c>SaveChangesAsync</c> as an unhandled <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
/// instead of a <see cref="ContentValidationException"/> the editor can act on (see the Week 4 QA
/// report, finding F1: a 17-character <c>Event.Season</c> crashed the Blazor circuit). A static
/// helper rather than an instance method on <see cref="ContentGuard"/> because not every write
/// service that owns a bounded column also injects <see cref="ContentGuard"/> (e.g. <c>StatsService</c>).
/// </summary>
internal static class FieldLength
{
    /// <summary>
    /// Checked against <paramref name="value"/> as given — a required field's caller passes an
    /// already-non-null, already-trimmed string (from <c>Required</c>) and can suffix the call with
    /// <c>!</c>; an optional field's caller passes an already-<c>Blank</c>-normalised value and null
    /// passes through untouched. One overload rather than two: a nullable and non-nullable
    /// <see langword="string"/> parameter erase to the same CLR signature, so C# cannot distinguish
    /// them by nullability annotation alone.
    /// </summary>
    public static string? Check(string? value, int max, string field) =>
        value is null || value.Length <= max ? value : throw new ContentValidationException(field, "Validation_MaxLength", max);
}

using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Services;

/// <summary>
/// Flattens an aggregate to the fields an editor can change, so the activity log stores a readable
/// before/after diff rather than an object graph with its navigations and stamps.
/// <para>
/// A child collection (<c>Page.Blocks</c>, <c>TrainingGuide.Chapters</c>, an <c>Event</c>'s gallery,
/// results and registrations) never shows up as scalar fields, so editing only a block's body used to
/// snapshot identically before and after — an empty diff for a real change. Each such collection
/// instead contributes a <c>"{Name}Count"</c> and a <c>"{Name}Hash"</c> entry, so a change anywhere
/// inside the children still moves the snapshot.
/// </para>
/// </summary>
public static class Audit
{
    private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal)
    {
        nameof(BaseEntity.Id), nameof(BaseEntity.CreatedAt), nameof(BaseEntity.CreatedBy),
        nameof(BaseEntity.UpdatedAt), nameof(BaseEntity.UpdatedBy), nameof(BaseEntity.DeletedAt),
    };

    public static Dictionary<string, object?> Snapshot(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || Excluded.Contains(property.Name))
            {
                continue;
            }

            if (property.CanWrite && IsScalar(property.PropertyType))
            {
                result[property.Name] = property.GetValue(entity);
                continue;
            }

            var elementType = ChildCollectionElementType(property.PropertyType);
            if (elementType is null)
            {
                continue;
            }

            var children = property.GetValue(entity) is IEnumerable enumerable ? enumerable.Cast<object>().ToList() : [];
            result[property.Name + "Count"] = children.Count;
            result[property.Name + "Hash"] = ChildrenHash(children, elementType);
        }

        return result;
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)) return true;
        if (typeof(IEnumerable).IsAssignableFrom(type)) return false;
        return type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(Guid)
            || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(DateTimeOffset) || type == typeof(DateTime);
    }

    /// <summary>The element type of an <see cref="IEnumerable{T}"/> property whose items are themselves audited entities, or null for anything else (including <see cref="string"/>, which is technically enumerable).</summary>
    private static Type? ChildCollectionElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        foreach (var candidate in type.GetInterfaces().Prepend(type))
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = candidate.GetGenericArguments()[0];
                if (typeof(BaseEntity).IsAssignableFrom(elementType))
                {
                    return elementType;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// A stable-enough fingerprint of a child collection's content: every child's own scalar snapshot,
    /// JSON-serialised and concatenated in a fixed order — by <c>SortOrder</c>/<c>Position</c> when the
    /// element type carries one (every ordered child collection in this codebase does), else by <c>Id</c>
    /// — then hashed so the log stores a short token rather than the whole nested payload.
    /// </summary>
    private static string ChildrenHash(List<object> children, Type elementType)
    {
        var orderProperty = elementType.GetProperty("SortOrder") ?? elementType.GetProperty("Position");
        IEnumerable<object> ordered = orderProperty is not null
            ? children.OrderBy(child => orderProperty.GetValue(child))
            : children.OrderBy(child => ((BaseEntity)child).Id);

        var builder = new StringBuilder();
        foreach (var child in ordered)
        {
            builder.Append(JsonSerializer.Serialize(Snapshot(child)));
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))[..16];
    }
}

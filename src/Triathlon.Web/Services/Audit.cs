using System.Collections;
using System.Reflection;
using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Services;

/// <summary>
/// Flattens an aggregate to the fields an editor can change, so the activity log stores a readable
/// before/after diff rather than an object graph with its navigations and stamps.
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
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0 || Excluded.Contains(property.Name))
            {
                continue;
            }

            if (!IsScalar(property.PropertyType))
            {
                continue;
            }

            result[property.Name] = property.GetValue(entity);
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
}

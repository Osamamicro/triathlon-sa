using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Services;

/// <summary>ADR 0001: restoring a parent restores the children its delete cascaded — the ones whose DeletedAt fell in the same second.</summary>
public static class Restore
{
    public static void Aggregate<TChild>(BaseEntity parent, IEnumerable<TChild> children) where TChild : BaseEntity
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(children);
        var deletedAt = parent.DeletedAt ?? throw new InvalidOperationException("The parent is not deleted.");
        parent.DeletedAt = null;
        foreach (var child in children)
        {
            if (child.DeletedAt is { } at && Math.Abs((at - deletedAt).TotalSeconds) < 1)
            {
                child.DeletedAt = null;
            }
        }
    }
}

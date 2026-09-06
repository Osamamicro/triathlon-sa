namespace Triathlon.Web.Domain.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record PageQuery(int Page = 1, int PageSize = 20, string? Sort = null, string? Q = null)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
}

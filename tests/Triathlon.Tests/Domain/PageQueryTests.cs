using Triathlon.Web.Domain.Common;

namespace Triathlon.Tests.Domain;

public class PageQueryTests
{
    [Fact]
    public void Defaults_SkipZero_TakeTwenty()
    {
        var query = new PageQuery();

        Assert.Equal(0, query.Skip);
        Assert.Equal(20, query.Take);
    }

    [Fact]
    public void Page3_PageSize500_Skip200_TakeClampedTo100()
    {
        var query = new PageQuery(Page: 3, PageSize: 500);

        Assert.Equal(200, query.Skip);
        Assert.Equal(100, query.Take);
    }

    [Fact]
    public void Page2_PageSize0_Skip1_TakeClampedTo1()
    {
        var query = new PageQuery(Page: 2, PageSize: 0);

        Assert.Equal(1, query.Skip);
        Assert.Equal(1, query.Take);
    }
}

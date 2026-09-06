using Triathlon.Web.Areas.Dashboard.Account;

namespace Triathlon.Tests.Web;

/// <summary>
/// Pure unit tests for the open-redirect guard behind every staff-login redirect. No container,
/// no host — just the predicate, so the dangerous inputs are enumerated once and cheaply.
/// </summary>
public sealed class LocalUrlTests
{
    [Theory]
    [InlineData("/dashboard", true)]
    [InlineData("/dashboard/events?x=1", true)]
    [InlineData("//evil.com", false)]
    [InlineData("/\\evil.com", false)]
    [InlineData("https://evil.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsLocalUrl_accepts_only_same_origin_relative_paths(string? url, bool expected) =>
        Assert.Equal(expected, IdentityRedirectManager.IsLocalUrl(url));
}

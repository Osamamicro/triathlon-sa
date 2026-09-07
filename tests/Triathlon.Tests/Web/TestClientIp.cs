using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Triathlon.Tests.Web;

/// <summary>
/// TestServer has no remote address, so every request shares the rate limiter's "unknown" bucket.
/// Tests that post forms send X-Test-Client-Ip with a unique value and this filter turns it into
/// the connection's address before the limiter looks at it. Test host only.
/// </summary>
public sealed class TestClientIp : IStartupFilter
{
    public const string Header = "X-Test-Client-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(Header, out var value) && IPAddress.TryParse(value, out var ip))
                {
                    context.Connection.RemoteIpAddress = ip;
                }

                return nextMiddleware(context);
            });

            next(app);
        };
    }

    public static string Unique() =>
        $"10.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(1, 254)}";
}

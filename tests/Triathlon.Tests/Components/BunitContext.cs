using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Triathlon.Tests.Components;

/// <summary>
/// Base class for every dashboard-component test: wires MudBlazor's services (its components fail
/// to render without them — popovers, the dialog/snackbar providers, JS interop stubs), a real
/// <c>IStringLocalizer&lt;DashboardStrings&gt;</c> backed by the actual resx files (so a test that
/// asserts on "Required" is asserting on the same string a user would see, not a stand-in), and
/// pins the UI culture to English so tests are deterministic regardless of the machine running them.
/// </summary>
public abstract class BunitContext : Bunit.BunitContext, IAsyncLifetime
{
    protected BunitContext()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        Services.AddMudServices();
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
        Services.AddLogging();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // MudBlazor registers at least one service (KeyInterceptorService) that only implements
    // IAsyncDisposable, so tearing the container down through the base class's synchronous
    // Dispose() throws. Implementing xUnit's IAsyncLifetime makes xUnit tear the fixture down
    // through DisposeAsync instead, which bUnit's own container walk handles correctly.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();
}

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Triathlon.Web.Areas.Dashboard;
using Triathlon.Web.Areas.Dashboard.Account;
using Triathlon.Web.Areas.Public;
using Triathlon.Web.Data;
using Triathlon.Web.Data.Seed;
using Triathlon.Web.Domain.Identity;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Jobs;
using Triathlon.Web.Middleware;
using Triathlon.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// Razor Pages under /{culture} for the public website, plus the localization that segment drives.
builder.Services.AddPublicSite();

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.AddAppProxy(builder.Configuration);
builder.Services.AddAppCompression();
builder.Services.AddAppOutputCache();
builder.Services.AddAppRateLimiter();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/dashboard/login";
    options.LogoutPath = "/dashboard/logout";
    options.AccessDeniedPath = "/dashboard/access-denied";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddAppAuthorization();

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        // Staff accounts are created by an administrator, so there is no confirmation mail to wait on.
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 12;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        // Left at the default schema version: passkeys and their table were removed with the rest of
        // the scaffolded self-service account pages, and the design-time model must match this one or
        // every migration comes out with phantom pending changes.
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddAppMedia(builder.Configuration);
builder.Services.AddAppEmail(builder.Configuration);
builder.Services.AddAppJobs(builder.Configuration);

// One check, and the one that matters: if the database is unreachable, nothing on this site works.
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// ---------------------------------------------------------------------------------------------
// Pipeline order, and why:
//   forwarded headers  — first, so every later decision (client IP for the rate limiter, scheme
//                        for HTTPS redirection and absolute URLs) sees the visitor and not nginx.
//                        This is the one deviation from the task brief's stated order, which put
//                        the staging gate ahead of it; ASP.NET Core requires forwarded headers to
//                        run before anything that reads the address or scheme.
//   staging gate       — before anything can produce or replay a response body, so an unfinished
//                        site cannot leak through static assets or the output cache.
//   exception handling — outside compression, so a failure inside it still renders an error page.
//   compression        — before routing, so it wraps static assets and endpoints alike.
//   rate limiter       — after routing, because the policy is chosen from endpoint metadata.
//   output cache       — after authorization, so a cached response can never bypass an auth check.
// ---------------------------------------------------------------------------------------------

app.UseAppProxy();
app.UseStagingBasicAuth();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/dashboard/error", createScopeForErrors: true);
    // The default HSTS value is 30 days. See https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseResponseCompression();

app.UseStatusCodePagesWithReExecute("/dashboard/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Explicit so request localization can sit behind it: the culture comes out of the matched route.
app.UseRouting();
app.UseRateLimiter();
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPublicRoot();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();
app.MapAppJobsDashboard();

// Anonymous and never cached: the probe has to reflect the state of this instance right now, and
// it is the one path the staging gate lets through.
app.MapHealthChecks(StagingBasicAuthMiddleware.HealthPath).AllowAnonymous();

// Test hook. The integration tests need endpoints production does not have — a rate-limited POST,
// and a GET that changes on every render so a cache hit is provable. Null outside the tests.
Program.ConfigureTestEndpoints?.Invoke(app);

// Staging and production keep this off and migrate as a deployment step; developers and the
// integration tests turn it on so a fresh database is usable immediately.
if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await SeedIdentity.RunAsync(scope.ServiceProvider);
}

app.Run();

/// <summary>Named so the integration tests can boot the real application with WebApplicationFactory.</summary>
public partial class Program
{
    /// <summary>
    /// Extra endpoints for the integration tests, invoked once while the pipeline is being built.
    /// Never set in production, where it stays null and maps nothing. The class cannot be declared
    /// static — the compiler generates the other half of it from this file's top-level statements.
    /// </summary>
    public static Action<IEndpointRouteBuilder>? ConfigureTestEndpoints { get; set; }
}

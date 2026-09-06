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
using Triathlon.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// Razor Pages under /{culture} for the public website, plus the localization that segment drives.
builder.Services.AddPublicSite();

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

var app = builder.Build();

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

app.UseStatusCodePagesWithReExecute("/dashboard/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Explicit so request localization can sit behind it: the culture comes out of the matched route.
app.UseRouting();
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPublicRoot();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();

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
public partial class Program;

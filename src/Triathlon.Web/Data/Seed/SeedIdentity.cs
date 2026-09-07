using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Makes sure the three staff roles exist and, on a brand-new database only, creates the first
/// administrator from <c>Seed:AdminEmail</c> / <c>Seed:AdminPassword</c> (environment variables
/// <c>Seed__AdminEmail</c> and <c>Seed__AdminPassword</c> in production). The "no users yet" guard is
/// what keeps this from resurrecting or resetting an account someone deliberately removed or renamed.
/// Outside Development the committed developer password is refused outright — see
/// <see cref="DevelopmentPassword"/>.
/// </summary>
public static class SeedIdentity
{
    /// <summary>
    /// The password committed in <c>appsettings.Development.json</c>, so that a deployment which
    /// inherited that file cannot quietly stand up an administrator everyone can read the password of.
    /// </summary>
    public const string DevelopmentPassword = "ChangeMe-Local-2026!";

    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedIdentity).FullName!);

        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        // Checked before anything is created: a server that starts with the committed development
        // password would hand the dashboard to anyone who has read this repository, and refusing to
        // boot is the only response that cannot be ignored.
        if (password == DevelopmentPassword &&
            !services.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            throw new InvalidOperationException(
                "Seed:AdminPassword is the development password committed in appsettings.Development.json. "
                + "Set Seed__AdminPassword to a real secret for this deployment.");
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                Check(await roleManager.CreateAsync(new IdentityRole(role)), $"create role '{role}'");
            }
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.Users.AnyAsync(ct))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No users exist and Seed:AdminEmail / Seed:AdminPassword are not configured, so no administrator "
                + "was created. Set Seed__AdminEmail and Seed__AdminPassword and restart to bootstrap the dashboard.");
            return;
        }

        var admin = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator",
        };

        Check(await userManager.CreateAsync(admin, password), $"create the seed administrator '{email}'");
        Check(await userManager.AddToRoleAsync(admin, Roles.SuperAdmin), $"grant '{Roles.SuperAdmin}' to '{email}'");

        logger.LogInformation("Seeded the first administrator {Email} as {Role}.", email, Roles.SuperAdmin);
    }

    /// <summary>Seeding runs before the app serves traffic; a half-seeded identity store is worse than a hard stop.</summary>
    private static void Check(IdentityResult result, string what)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Identity seeding failed to {what}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}

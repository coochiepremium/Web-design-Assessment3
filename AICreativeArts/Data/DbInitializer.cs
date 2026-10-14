using AICreativeArts.Authorization;
using AICreativeArts.Infrastructure;
using AICreativeArts.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AICreativeArts.Data;

/// <summary>
/// Prepares the database when the site starts: applies EF Core migrations, creates
/// the Admin role, the seed accounts and copies the sample gallery images.
/// </summary>
public static class DbInitializer
{
    public static async Task InitialiseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        try
        {
            var db = provider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            await EnsureRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole>>(), logger);
            await EnsureAccountsAsync(
                provider.GetRequiredService<UserManager<IdentityUser>>(),
                provider.GetRequiredService<IOptions<SeedAccountOptions>>().Value,
                logger);

            var storedFilenames = await db.AIImages.Select(i => i.Filename).ToListAsync();
            CopySampleImages(storedFilenames, provider.GetRequiredService<IWebHostEnvironment>(), provider.GetRequiredService<IImageStorage>());
        }
        catch (Exception ex)
        {
            // The site still starts so the friendly error page can be shown,
            // and the log explains what went wrong.
            logger.LogCritical(ex, "The database could not be initialised.");
        }
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            LogResult(result, logger, $"role {Roles.Admin}");
        }
    }

    private static async Task EnsureAccountsAsync(UserManager<IdentityUser> userManager, SeedAccountOptions options, ILogger logger)
    {
        foreach (var account in options.Accounts.Where(a => !string.IsNullOrWhiteSpace(a.Email)))
        {
            var user = await userManager.FindByEmailAsync(account.Email);
            if (user is null)
            {
                user = new IdentityUser { UserName = account.Email, Email = account.Email, EmailConfirmed = true };
                var created = await userManager.CreateAsync(user, account.Password);
                LogResult(created, logger, $"account {account.Email}");
                if (!created.Succeeded)
                {
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(account.Role) && !await userManager.IsInRoleAsync(user, account.Role))
            {
                var added = await userManager.AddToRoleAsync(user, account.Role);
                LogResult(added, logger, $"role {account.Role} for {account.Email}");
            }
        }
    }

    /// <summary>Restores the sample files for sample rows that still exist (uploads are not kept in source control).</summary>
    private static void CopySampleImages(IEnumerable<string> storedFilenames, IWebHostEnvironment environment, IImageStorage storage)
    {
        var folder = Path.Combine(environment.WebRootPath, SampleData.SampleFolder);
        if (!Directory.Exists(folder))
        {
            return;
        }

        var stored = storedFilenames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var image in SampleData.Images.Where(i => stored.Contains(i.Filename)))
        {
            storage.EnsureCopied(Path.Combine(folder, image.Filename));
        }
    }

    private static void LogResult(IdentityResult result, ILogger logger, string what)
    {
        if (result.Succeeded)
        {
            logger.LogInformation("Created {What}", what);
        }
        else
        {
            logger.LogWarning("Could not create {What}: {Errors}", what,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

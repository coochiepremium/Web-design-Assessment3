using AICreativeArts.Authorization;
using AICreativeArts.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AICreativeArts.Data;

/// <summary>
/// Prepares the database when the site starts: applies EF Core migrations, then
/// creates the Admin role and the seed accounts listed in appsettings.json.
/// The AI image gallery starts empty - members add images through the Create page.
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
            await ReplaceOutdatedDevelopmentDatabaseAsync(db, provider.GetRequiredService<IWebHostEnvironment>(), logger);
            await db.Database.MigrateAsync();

            await EnsureRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole>>(), logger);
            await EnsureAccountsAsync(
                provider.GetRequiredService<UserManager<IdentityUser>>(),
                provider.GetRequiredService<IOptions<SeedAccountOptions>>().Value,
                logger);
        }
        catch (Exception ex)
        {
            // The site still starts so the friendly error page can be shown,
            // and the log explains what went wrong.
            logger.LogCritical(ex, "The database could not be initialised.");
        }
    }

    /// <summary>
    /// A database created by an earlier version of this project contains migrations this
    /// version no longer has, so applying the current migrations would fail. In Development
    /// the old SQLite file is renamed to app.db.bak-{time} (nothing is deleted) and a fresh
    /// database is created; in other environments the problem is only logged.
    /// </summary>
    private static async Task ReplaceOutdatedDevelopmentDatabaseAsync(ApplicationDbContext db, IWebHostEnvironment environment, ILogger logger)
    {
        if (!db.Database.IsSqlite() || !await db.Database.CanConnectAsync())
        {
            return;
        }

        var known = db.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
        var unknown = (await db.Database.GetAppliedMigrationsAsync()).Where(m => !known.Contains(m)).ToList();
        if (unknown.Count == 0)
        {
            return;
        }

        var dataSource = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (!environment.IsDevelopment() || string.IsNullOrEmpty(dataSource) || dataSource == ":memory:")
        {
            logger.LogCritical("The database was created by an older version of the site (unknown migrations: {Migrations}). Delete {DataSource} so it can be recreated.",
                string.Join(", ", unknown), dataSource);
            return;
        }

        await db.Database.CloseConnectionAsync();
        SqliteConnection.ClearAllPools();

        var path = Path.GetFullPath(dataSource);
        var backup = $"{path}.bak-{DateTime.Now:yyyyMMddHHmmss}";
        File.Move(path, backup);
        foreach (var suffix in new[] { "-shm", "-wal" })
        {
            if (File.Exists(path + suffix))
            {
                File.Move(path + suffix, backup + suffix);
            }
        }

        logger.LogWarning("The database was created by an older version of the site, so it was renamed to {Backup} and a new one will be created.", backup);
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

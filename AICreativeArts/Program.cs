using AICreativeArts.Data;
using AICreativeArts.Infrastructure;
using AICreativeArts.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Services

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Members can upload straight after registering (no e-mail confirmation step).
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.Name = ".MuseAI.Auth";

    // One like per image per login: forget the likes whenever the user signs in or out.
    options.Events.OnSignedIn = context =>
    {
        context.HttpContext.RequestServices.GetRequiredService<ILikeTracker>().Reset();
        return Task.CompletedTask;
    };
    options.Events.OnSigningOut = context =>
    {
        context.HttpContext.RequestServices.GetRequiredService<ILikeTracker>().Reset();
        return Task.CompletedTask;
    };
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MuseAI.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(4);
});

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<SeedAccountOptions>(builder.Configuration.GetSection(SeedAccountOptions.SectionName));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILikeTracker, SessionLikeTracker>();
builder.Services.AddScoped<IImageStorage, ImageStorage>();

builder.Services.AddControllersWithViews(options =>
{
    // Every POST/PUT/DELETE must carry a valid antiforgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();

var app = builder.Build();

// ---------------------------------------------------------------- Pipeline

// Unhandled exceptions and error status codes always get a friendly, on-brand page
// (details are written to the log instead of being shown to visitors).
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

await DbInitializer.InitialiseAsync(app.Services);

app.Run();

/// <summary>Exposed so the integration tests can host the site.</summary>
public partial class Program { }

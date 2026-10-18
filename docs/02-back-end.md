# Back-end reference

This document explains every C# file in the **AI Unmasked** web project (`AICreativeArts/`) outside `Views/` and `wwwroot/`. It covers what each file does, how a request travels from the browser to the database and back, who may open which page, how every kind of error is handled, what the database looks like and what the automated tests check. It is written for you, the owner of the project. It assumes you know basic C#, but it explains each ASP.NET Core idea the first time it comes up. Every class, route, setting and message quoted here is copied from the code. If you change the code, update this document as well.

Related documents: [../DOCUMENTATION.md](../DOCUMENTATION.md) (overview), [03-front-end.md](03-front-end.md) (views, CSS, JavaScript), [04-images-content-and-references.md](04-images-content-and-references.md) (adding images, credits and text), [05-vscode-guide.md](05-vscode-guide.md) (VS Code and moving into a new ASP.NET Core Web App).

## Contents

1. [At a glance](#1-at-a-glance)
2. [Program.cs: services and middleware](#2-programcs-services-and-middleware)
3. [Configuration and project files](#3-configuration-and-project-files)
4. [Roles and the access-control matrix](#4-roles-and-the-access-control-matrix)
5. [Controllers](#5-controllers)
6. [Data: DbContext, migrations, start-up initialiser and database schema](#6-data-dbcontext-migrations-start-up-initialiser-and-database-schema)
7. [Models and view models](#7-models-and-view-models)
8. [Services](#8-services)
9. [Filters: UploadSizeGuardAttribute](#9-filters-uploadsizeguardattribute)
10. [Infrastructure: options classes and URL helpers](#10-infrastructure-options-classes-and-url-helpers)
11. [Tag helpers and the view component](#11-tag-helpers-and-the-view-component)
12. [Content classes](#12-content-classes)
13. [Identity pages: Login, Register and AccessDenied](#13-identity-pages-login-register-and-accessdenied)
14. [Walkthroughs](#14-walkthroughs)
15. [Exception handling](#15-exception-handling)
16. [The test project](#16-the-test-project)
17. [Common changes](#17-common-changes)

---

## 1. At a glance

### How a request is handled

```text
 Browser
   |  e.g. GET /AIImages   or   POST /AIImages/Like/3
   v
+--------------------------- Program.cs middleware pipeline (runs top to bottom) ---------------------------+
|  1  UseExceptionHandler("/Home/Error")       catches any exception thrown further down                    |
|  2  UseStatusCodePagesWithReExecute(...)     turns an empty 400-599 response into a friendly page         |
|  3  UseHsts()                                (not in Development) tells browsers to use HTTPS only        |
|  4  UseHttpsRedirection()                    sends http:// requests to https:// when an HTTPS port exists |
|  5  security-headers middleware              adds nosniff / SAMEORIGIN / Referrer-Policy headers          |
|  6  UseStaticFiles()  -- file in wwwroot? -->  served immediately (CSS, JS, fonts, images, uploads)       |
|  7  UseRouting()                             chooses the endpoint (controller action or Razor Page)       |
|  8  UseAuthentication()                      reads the .AIUnmasked.Auth cookie and sets User              |
|  9  UseAuthorization()                       enforces [Authorize] / [Authorize(Roles = "Admin")]          |
+-----------------------------------------------------------------------------------------------------------+
   v
 MVC filters (POST uploads):  UploadSizeGuard -> RequestSizeLimit / RequestFormLimits -> antiforgery check
   v
 Model binding + validation   (form fields -> AIImageFormViewModel, DataAnnotations -> ModelState)
   v
 Controller action                                   Razor Page (Identity area)
 HomeController / AIImagesController                 Login / Register / AccessDenied (+ default Identity UI)
   |                    |                                      |
   v                    v                                      v
 Services            ApplicationDbContext (EF Core)         UserManager / SignInManager / RoleManager
 IImageStorage -> wwwroot/uploads/        |                            |
 ILikeTracker  -> ImageLikes table        +------> app.db (SQLite) <---+
   v
 View (.cshtml) rendered inside Views/Shared/_Layout.cshtml
   partials, tag helpers (<image-slot>, <background-slot-note>, nav-active), view component (UploadGallery)
   IMediaLibrary checks which wwwroot/img files exist and reads ImageCredits
   v
 HTML response -> browser
```

### Folder map

| Folder / file | Purpose | Section |
|---|---|---|
| `Program.cs` | Registers services and builds the middleware pipeline | [2](#2-programcs-services-and-middleware) |
| `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`, `AICreativeArts.csproj` | Settings, run profiles and NuGet packages | [3](#3-configuration-and-project-files) |
| `Authorization/Roles.cs` | The role name `"Admin"` | [4](#4-roles-and-the-access-control-matrix) |
| `Controllers/` | `HomeController` (public pages and error pages), `AIImagesController` (gallery CRUD and likes) | [5](#5-controllers) |
| `Data/` | `ApplicationDbContext`, the `InitialCreate` migration, `DbInitializer` | [6](#6-data-dbcontext-migrations-start-up-initialiser-and-database-schema) |
| `Models/`, `ViewModels/` | `AIImage`, `AIImageRules`, `ErrorViewModel`, `StatusCodeViewModel`, `AIImageFormViewModel` | [7](#7-models-and-view-models) |
| `Services/` | `ImageStorage` (uploads), `LoginLikeTracker` (one like per login, stored in the database), `MediaLibrary` (image slots) | [8](#8-services) |
| `Filters/` | `UploadSizeGuardAttribute` (oversized uploads) | [9](#9-filters-uploadsizeguardattribute) |
| `Infrastructure/` | Strongly typed options classes and URL helper extensions | [10](#10-infrastructure-options-classes-and-url-helpers) |
| `TagHelpers/`, `ViewComponents/` | `<image-slot>`, `<background-slot-note>`, `nav-active`, the home page upload gallery | [11](#11-tag-helpers-and-the-view-component) |
| `Content/` | The researched text and the list of image slots | [12](#12-content-classes) |
| `Areas/Identity/Pages/Account/` | Customised Login, Register and AccessDenied pages | [13](#13-identity-pages-login-register-and-accessdenied) |

### What the brief asks for, and where the back end does it

| Brief requirement | Where it is implemented |
|---|---|
| `AIImage` model with Entity Framework | `Models/AIImage.cs`, `Data/ApplicationDbContext.cs`, `Data/Migrations/20261018040612_InitialCreate.cs` |
| Visitor / Member / Admin accessibility | `[AllowAnonymous]`, `[Authorize]` and `[Authorize(Roles = Roles.Admin)]` in `Controllers/AIImagesController.cs` ([matrix](#43-access-control-matrix)) |
| Image uploaded to `wwwroot/uploads/` | `Services/ImageStorage.cs` (`Uploads:FolderName` = `uploads`) |
| Upload date set automatically, Upload Date and Like read-only | Set on the server in `AIImagesController.Create`/`Edit`. The browser's values are ignored |
| Like at most once per image, again only after logging off and on | `Services/LikeTracker.cs` and the cookie events in `Program.cs` |
| Lists sorted by likes, highest first | `OrderByDescending(i => i.Like)` in `AIImagesController.Index` and `UploadGalleryViewComponent` |
| Upload block shows login/register links or an upload link | `Views/Shared/_UploadCallToAction.cshtml`, which checks `User.Identity.IsAuthenticated` |
| Handle all runtime exceptions | [Section 15](#15-exception-handling) |
| Credit all media on the Contact page | `ImageCredits` in `appsettings.json` + `Services/MediaLibrary.cs` |

---

## 2. Program.cs: services and middleware

[`AICreativeArts/Program.cs`](../AICreativeArts/Program.cs) is the entry point. It has three parts:

1. it registers **services** with the dependency-injection (DI) container;
2. it builds the **middleware pipeline**;
3. it initialises the database and starts the web server.

"Dependency injection" means a class lists what it needs in its constructor, for example `AIImagesController(ApplicationDbContext context, IImageStorage storage, ...)`, and ASP.NET Core creates and passes in those objects. Each registration has a *lifetime*. **Scoped** means one instance per HTTP request. **Singleton** means one instance for the whole run of the site.

### 2.1 Service registrations (in the order they appear)

| # | Code | What it does | Why |
|---|---|---|---|
| 1 | `GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(...)` | Reads `ConnectionStrings:DefaultConnection` from `appsettings.json` | Fails immediately at start-up with a clear message if the setting is missing, instead of failing later on the first database call |
| 2 | `AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString))` | Registers the EF Core database context (scoped) using SQLite | One context per request is the EF Core recommendation. SQLite needs no database server, so the project runs anywhere |
| 3 | `AddDefaultIdentity<IdentityUser>(options => ...)` | Adds ASP.NET Core Identity: user accounts, password hashing, sign-in, the auth cookie, token providers and the **default Identity UI** pages (from the `Microsoft.AspNetCore.Identity.UI` package) | Gives Login, Register, Logout, Lockout, Manage and so on without writing them. Login, Register and AccessDenied are overridden in this project ([section 13](#13-identity-pages-login-register-and-accessdenied)) |
| | `options.SignIn.RequireConfirmedAccount = false` | Users can sign in straight after registering | There is no e-mail service, and members must be able to upload immediately |
| | `options.User.RequireUniqueEmail = true` | Two accounts cannot share an e-mail address | The e-mail is also the user name |
| | `options.Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 5 minutes` | After 5 wrong passwords the account is locked for 5 minutes | Slows down password guessing. Login uses `lockoutOnFailure: true` |
| | *(password rules are not changed)* | Identity's defaults apply: at least 6 characters, with an upper-case letter, a lower-case letter, a digit and a non-alphanumeric character | The seed passwords `Admin@123` and `Member@123` meet these rules |
| 4 | `.AddRoles<IdentityRole>()` | Enables roles (`RoleManager`, `User.IsInRole("Admin")`, `[Authorize(Roles = ...)]`) | The Admin role is required by the brief |
| 5 | `.AddEntityFrameworkStores<ApplicationDbContext>()` | Stores users and roles in the same SQLite database (`AspNet*` tables) | One database file for everything |
| 6 | `ConfigureApplicationCookie(...)` | Configures the sign-in cookie | See the next table |
| 7 | `Configure<SecurityStampValidatorOptions>(...)` | When Identity rebuilds the sign-in cookie (every 30 minutes) it copies the *login id* claim across | Keeps "one like per login" working for long visits |
| 8 | `Configure<SiteOptions>`, `Configure<UploadOptions>`, `Configure<SeedAccountOptions>`, `Configure<ImageCreditOptions>` | Binds the `Site`, `Uploads`, `SeedAccounts` and `ImageCredits` sections of `appsettings.json` to C# classes ([section 10](#10-infrastructure-options-classes-and-url-helpers)) | Code then receives typed settings (`IOptions<UploadOptions>`) instead of reading strings |
| 9 | `AddHttpContextAccessor()` | Lets a service reach the current request | `LoginLikeTracker` reads the signed-in user and their login id |
| 10 | `AddScoped<ILikeTracker, LoginLikeTracker>()` | One like tracker per request | It uses that request's database context and user |
| 11 | `AddScoped<IImageStorage, ImageStorage>()` | One upload service per request | Used by `AIImagesController` |
| 12 | `AddSingleton<IMediaLibrary, MediaLibrary>()` | One media library for the whole site | It keeps no per-user state, and it is used on every page (layout, tag helpers) |
| 13 | `AddControllersWithViews(...)` with `AutoValidateAntiforgeryTokenAttribute` and `IdentityPageGuardFilter` | Adds MVC, plus a **global antiforgery filter** and a guard for the built-in Identity pages (see section 9) | Every POST, PUT, PATCH and DELETE must carry the hidden `__RequestVerificationToken` that `<form method="post">` tag helpers add automatically. This blocks cross-site request forgery. GET requests are not checked |
| 14 | `AddRazorPages()` | Adds Razor Pages | The Identity pages are Razor Pages |

**Cookie settings** (`ConfigureApplicationCookie`):

| Setting | Value | Effect |
|---|---|---|
| `LoginPath` | `/Identity/Account/Login` | Where a visitor is sent when a page needs a signed-in user. `?ReturnUrl=...` is added automatically |
| `AccessDeniedPath` | `/Identity/Account/AccessDenied` | Where a signed-in member is sent when a page needs the Admin role |
| `Cookie.Name` | `.AIUnmasked.Auth` | Name of the sign-in cookie |
| `Events.OnSigningIn` | `LoginSession.AddLoginId(...)` | Gives every new sign-in a random **login id** claim inside the sign-in cookie |

Likes are stored against that login id (`ImageLikes` table), which gives the brief's rule: *"max one like per block... The user needs to logoff then login if the user wants to click the Like button again"*. A new login means a new id, so each image can be liked once more.

### 2.2 The middleware pipeline, in order, and why that order

Middleware are steps that every request passes through in the order they are added. Each step can answer the request itself or pass it on to the next step. Order matters: a step can only see or protect what comes *after* it.

| # | Code | What it does | Why it is here |
|---|---|---|---|
| 1 | `app.UseExceptionHandler("/Home/Error")` | If anything later throws an exception, it logs it, clears the response and re-runs the request as `/Home/Error` (HTTP 500) | It must be **first** so it wraps every later step. It is used in **all** environments, so visitors never see a stack trace. The details go to the log (the terminal or VS Code Debug Console) |
| 2 | `app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}")` | If a response finishes with status 400-599 and no body (for example `NotFound()`, a 405 or a 400 from the antiforgery check), it re-runs the request as `/Home/Status?code=404` | It keeps the address bar URL and the real status code, and shows the friendly `Views/Home/Status.cshtml` page. It is near the top so it covers controllers, Razor Pages and static files |
| 3 | `if (!app.Environment.IsDevelopment()) app.UseHsts();` | Sends the `Strict-Transport-Security` header (30 days by default) | Production only. On `localhost` it would make the browser remember "HTTPS only" and break the plain-HTTP profile |
| 4 | `app.UseHttpsRedirection()` | Redirects `http://` to `https://` | It does something only when an HTTPS port is known (the `https` profile, VS Code F5). With the `http` profile it logs the warning *"Failed to determine the https port for redirect."* and does nothing, which is harmless |
| 5 | `app.Use(async (context, next) => { ... })` | Adds `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN` and `Referrer-Policy: strict-origin-when-cross-origin` to every response | It runs **before static files**, so CSS, JS and uploaded images get the headers too. `nosniff` stops a browser treating an uploaded file as a script or HTML. `SAMEORIGIN` stops other sites framing this one (clickjacking) |
| 6 | `app.UseStaticFiles()` | Serves files from `wwwroot/` (CSS, JS, fonts, `img/`, `uploads/`) and ends the request | It runs before routing and authentication, so static files are fast and public. If a file is missing, the request continues and ends as a 404 (see `HomeController.Status`) |
| 7 | `app.UseRouting()` | Matches the URL to an endpoint: a controller action (`{controller=Home}/{action=Index}/{id?}`) or a Razor Page | Authorisation needs to know *which* endpoint was chosen, so routing must come before it |
| 8 | `app.UseAuthentication()` | Reads the `.AIUnmasked.Auth` cookie and fills `HttpContext.User` (name, roles) | It must come before authorisation, which checks who the user is |
| 9 | `app.UseAuthorization()` | Applies `[Authorize]` attributes on the chosen endpoint. An anonymous user gets a redirect to the login page, and a signed-in user without the role gets a redirect to the access-denied page | It must come **after routing** (to read the endpoint's attributes) and **after authentication** (to know the user). Requests it rejects never reach the controller |
| 11 | `app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")` | Defines MVC URLs, for example `/` → `Home/Index` and `/AIImages/Edit/5` → `AIImagesController.Edit(5)` | This is the standard MVC route |
| 12 | `app.MapRazorPages()` | Defines `/Identity/Account/...` URLs for the Identity pages | Needed for Login, Register, Logout and the other Identity pages |
| 13 | `await DbInitializer.InitialiseAsync(app.Services);` | Applies migrations, creates the Admin role and the seed accounts ([6.3](#63-dbinitializercs-preparing-the-database-at-start-up)) | It runs once, before the server accepts requests, so the database is ready on the first page view |
| 14 | `app.Run();` | Starts the web server (Kestrel) and blocks until it stops | |

The last line, `public partial class Program { }`, makes the auto-generated `Program` class public. The test project needs this to host the site in memory with `WebApplicationFactory<Program>` ([section 16](#16-the-test-project)).

---

## 3. Configuration and project files

### 3.1 `appsettings.json`: every key

[`AICreativeArts/appsettings.json`](../AICreativeArts/appsettings.json)

| Key | Value shipped | Read by | Meaning |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `DataSource=app.db;Cache=Shared` | `Program.cs` | SQLite file `app.db`, relative to the folder the site runs from (the `AICreativeArts` project folder with `dotnet run`, Visual Studio or VS Code F5, whose `launch.json` sets `cwd`). `Cache=Shared` lets connections share SQLite's cache. The file and its `-shm`/`-wal` companions are git-ignored |
| `Site:Name` | `AI Unmasked` | `SiteOptions.Name` | Bound into `SiteOptions`, but no view displays it at the moment. The layout and the page titles write "AI Unmasked" directly, so changing this key alone does not rename the site |
| `Site:DeveloperName` | `[Your Name]` | `HomeController.Contact` → `Views/Home/Contact.cshtml` | Shown as "This website is developed by ...". **Replace it with your name** |
| `Site:ContactEmail` | `your.email@example.com` | Contact page (`mailto:` link) | **Replace it with your e-mail** |
| `ImageCredits:<slot key>:Creator` | `""` | `MediaLibrary` → Contact page | Who made the image, for example "Jane Doe" or your name |
| `ImageCredits:<slot key>:Source` | `""` | same | Where it came from, for example "Unsplash" or "Own work". A credit is shown only when **both** Creator and Source are filled in (`ImageCredit.IsComplete`) |
| `ImageCredits:<slot key>:Url` | `""` | same | Link to the original. It is shown as "View original" only if it is an absolute `http`/`https` URL (`ImageCredit.HasLink`) |
| `ImageCredits:<slot key>:Licence` | `""` | same | For example "Unsplash Licence" or "CC BY 4.0". It is shown in brackets after the source |
| | | | Slot keys present: `hero-background`, `section-background`, `tile-laion`, `tile-getty`, `tile-wga`, `tile-sag`, `tile-heart`, `tile-theatre`, `upload-preview`, `favicon` (they must match `Content/ImageLibrary.cs`, which is enforced by a test). Credits are re-read while the site runs (`IOptionsMonitor`), so a refresh shows changes |
| `Uploads:FolderName` | `uploads` | `ImageStorage`, `SiteUrlExtensions.UploadedImage` | Folder under `wwwroot` for uploaded images, so files go to `wwwroot/uploads/` and are served from `/uploads/...` |
| `Uploads:MaxFileBytes` | `5242880` (5 MB) | `ImageStorage.ValidateAsync`, `_ImageForm.cshtml` (`data-max-bytes` for the browser check in `site.js`) | Largest accepted image |
| `SeedAccounts:Accounts[]:Email` | `admin@asp.net`, `member@asp.net` | `DbInitializer` | Accounts created at start-up if they do not exist yet. Entries with a blank e-mail are skipped |
| `SeedAccounts:Accounts[]:Password` | `Admin@123`, `Member@123` | `DbInitializer` | Used **only when the account is first created**. Changing it later does not change an existing account's password |
| `SeedAccounts:Accounts[]:Role` | `Admin`, `""` | `DbInitializer` | `Admin` = administrator, empty = member. Only `Admin` (or empty) works, because only that role is created |
| `Logging:LogLevel:Default` | `Information` | ASP.NET Core logging | Your own log messages (uploads, logins, errors) are shown |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | logging | Hides the framework's routine per-request messages |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` | `Warning` | logging | Hides every SQL statement EF Core runs (set it to `Information` to see them) |
| `AllowedHosts` | `*` | host filtering | Accept requests for any host name (template default) |

Settings bound with `IOptions<T>` (`Site`, `Uploads`, `SeedAccounts`) are read once, so **restart the site** after changing them. `ImageCredits` is read through `IOptionsMonitor` and updates live.

### 3.2 `appsettings.Development.json`

This file only overrides logging when `ASPNETCORE_ENVIRONMENT=Development`: `Default` = `Information`, `Microsoft.AspNetCore` = `Warning`. Every other value comes from `appsettings.json`. In Development, ASP.NET Core also reads **user secrets** (the project has a `UserSecretsId`). That lets you, for example, keep a different seed password out of the file: `dotnet user-secrets set "SeedAccounts:Accounts:0:Password" "..."`.

### 3.3 `Properties/launchSettings.json`

[`AICreativeArts/Properties/launchSettings.json`](../AICreativeArts/Properties/launchSettings.json) is used by `dotnet run` and Visual Studio (VS Code uses `.vscode/launch.json`, which sets the same URLs, as described in [05-vscode-guide.md](05-vscode-guide.md)).

| Profile | URL(s) | Notes |
|---|---|---|
| `http` | `http://localhost:5280` | `dotnet run` uses the first profile by default. There is no HTTPS, so `UseHttpsRedirection` does nothing |
| `https` | `https://localhost:7113` and `http://localhost:5280` | `dotnet run --launch-profile https`. HTTP requests are redirected to HTTPS. Run `dotnet dev-certs https --trust` once if the browser warns about the certificate |
| `IIS Express` | `http://localhost:3046`, SSL port `44385` | Visual Studio on Windows only |

Every profile sets `ASPNETCORE_ENVIRONMENT=Development` and `launchBrowser: true`.

### 3.4 `AICreativeArts.csproj`

| Setting / package | Version | Why |
|---|---|---|
| `Sdk="Microsoft.NET.Sdk.Web"`, `TargetFramework` `net8.0` | | ASP.NET Core 8 web app (long-term support) |
| `Nullable` `enable` | | The compiler warns about possible `null` mistakes. This is why the code uses `string?` and `= ""` |
| `ImplicitUsings` `enable` | | Common namespaces (`System`, `System.Linq`, `Microsoft.AspNetCore.Builder`, ...) are imported automatically |
| `UserSecretsId` | | Enables `dotnet user-secrets` in Development |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.31 | `IdentityDbContext` and the EF Core stores for users and roles |
| `Microsoft.AspNetCore.Identity.UI` | 8.0.31 | The default Identity Razor Pages (Logout, Lockout, Manage, ForgotPassword, ...) |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.31 | The SQLite database provider |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.31 | Design-time support for `dotnet ef migrations add` / `database update` (and Visual Studio's Package Manager Console) |

---

## 4. Roles and the access-control matrix

### 4.1 `Authorization/Roles.cs`

```csharp
public static class Roles
{
    public const string Admin = "Admin";
}
```

There is one role. The three kinds of user in the brief are:

| Brief term | In code |
|---|---|
| **Visitor** | Not signed in (`User.Identity.IsAuthenticated == false`) |
| **Member** | Signed in, no role. Everyone who registers through the Register page is a member |
| **Admin** | Signed in and in the `Admin` role (`User.IsInRole(Roles.Admin)`) |

The constant is used in `[Authorize(Roles = Roles.Admin)]`, in `DbInitializer` (to create the role), and in views (`Views/AIImages/Index.cshtml` and `_LoginPartial.cshtml` decide which buttons and badges to show). Using a constant instead of typing `"Admin"` everywhere means a typo becomes a compile error.

### 4.2 How the rules are enforced

1. **On the server (security).** Attributes on the controller actions are checked by `UseAuthorization()` before the action runs. A visitor who requests a protected URL gets a 302 redirect to `/Identity/Account/Login?ReturnUrl=<the URL>`. A member who requests an admin URL gets a 302 redirect to `/Identity/Account/AccessDenied?ReturnUrl=<the URL>`.
2. **In the views (convenience).** Buttons are only rendered for people allowed to use them. Hiding a button is *not* the security. The attributes in step 1 are.

There is no global "require login" policy. An action without an `[Authorize]` attribute is public.

### 4.3 Access-control matrix

✅ = opens · ➡️ Login = redirected to the login page (and back after logging in) · ⛔ = redirected to the Access Denied page

| URL | Method | Code | Visitor | Member | Admin |
|---|---|---|---|---|---|
| `/` (also `/Home`, `/Home/Index`) | GET | `HomeController.Index` | ✅ | ✅ | ✅ |
| `/Home/Contact` | GET | `HomeController.Contact` | ✅ | ✅ | ✅ |
| `/Home/ArtistShield` | GET | `HomeController.ArtistShield` | ✅ | ✅ | ✅ |
| `/AIImages` | GET | `AIImagesController.Index` `[AllowAnonymous]` | ✅ no buttons, like count shown as text | ✅ **Like** button | ✅ **Like**, **Edit**, **Delete**, and the image links to **Details** |
| `/AIImages/Create` | GET, POST | `Create` `[Authorize]` | ➡️ Login | ✅ | ✅ |
| `/AIImages/Like/{id}` | POST | `Like` `[Authorize]` | ➡️ Login | ✅ once per image per login | ✅ once per image per login |
| `/AIImages/Details/{id}` | GET | `Details` `[Authorize(Roles = Roles.Admin)]` | ➡️ Login | ⛔ | ✅ |
| `/AIImages/Edit/{id}` | GET, POST | `Edit` `[Authorize(Roles = Roles.Admin)]` | ➡️ Login | ⛔ | ✅ |
| `/AIImages/Delete/{id}` | GET, POST | `Delete` / `DeleteConfirmed` `[Authorize(Roles = Roles.Admin)]` | ➡️ Login | ⛔ | ✅ |
| `/Identity/Account/Login`, `/Identity/Account/Register` | GET, POST | `LoginModel`, `RegisterModel` `[AllowAnonymous]` | ✅ | redirected to `ReturnUrl` (or `/`) because they are already signed in | same as Member |
| `/Identity/Account/AccessDenied` | GET | `AccessDeniedModel` | ✅ | ✅ | ✅ |
| `/Identity/Account/Logout` | POST (the menu's Logout button) | default Identity UI | – (no Logout button is shown) | ✅ | ✅ |
| `/Identity/Account/Manage/...` (the "Hello ...!" link) | GET | default Identity UI | ➡️ Login | ✅ | ✅ |
| `/Home/Error`, `/Home/Status` | (re-executed) | `HomeController` | ✅ shown automatically after an error ([15](#15-exception-handling)) | ✅ same | ✅ same |

This matches the brief: visitors get the Home-folder pages plus Login and Register, members also get Create, and admins get everything. The AI Images list is open to visitors without any buttons, as in the brief's screenshot captioned *"Visitor user sees no button"*.

---

## 5. Controllers

A controller is a class whose public methods (*actions*) answer URLs. With the default route, `/AIImages/Edit/5` calls `AIImagesController.Edit(5)`.

Two patterns are used throughout:

- **Post/Redirect/Get.** After a successful POST, the action *redirects* instead of showing a page. Refreshing the browser then cannot submit the form twice.
- **TempData toasts.** Before redirecting, an action stores a one-off message in `TempData["SiteMessage"]` (green) or `TempData["SiteError"]` (red). `Views/Shared/_SiteToasts.cshtml`, which is included in `_Layout.cshtml`, shows it on the next page and then it disappears. TempData is kept in the `.AspNetCore.Mvc.CookieTempDataProvider` cookie between the two requests.

Actions without `[HttpPost]` answer GET (technically any method, but the site only links to them). Because of the global antiforgery filter, any POST must carry a valid token.

### 5.1 `Controllers/HomeController.cs`

Constructor: `ILogger<HomeController>` (for error logging) and `IOptions<SiteOptions>` (for the Contact page).

| Action | URL | Access | What it does |
|---|---|---|---|
| `Index()` | `/` | public | Returns `Views/Home/Index.cshtml`. The page reads its text from `Content/SiteContent.cs` and loads the upload gallery through `UploadGalleryViewComponent` |
| `Contact()` | `/Home/Contact` | public | Returns `Views/Home/Contact.cshtml` with the `SiteOptions` object as the model (developer name and e-mail). The view also uses `IMediaLibrary.All()` for the image credits and `SiteContent.TextSources` for the text sources |
| `ArtistShield()` | `/Home/ArtistShield` | public | The extra creative page. Returns `Views/Home/ArtistShield.cshtml`, whose quiz, checklist and timeline come from `Content/ArtistShieldContent.cs` |
| `Error()` | `/Home/Error` | public | Run by `UseExceptionHandler` after an unhandled exception. It gets the exception and the original path from `IExceptionHandlerPathFeature`, logs them with `LogError("Unhandled exception on {Path}")`, and returns `Views/Shared/Error.cshtml` with an `ErrorViewModel` whose `RequestId` is `Activity.Current?.Id ?? HttpContext.TraceIdentifier`. The page shows that ID as a "Reference" you can search for in the log. If you open the URL directly, it shows the same page with status 200 and logs nothing |
| `Status(int code)` | `/Home/Status?code=404` | public | Run by `UseStatusCodePagesWithReExecute`. See the steps below |

`Status(int code)` step by step:

1. If `IStatusCodeReExecuteFeature` is missing, someone typed the URL directly, so it redirects to `/`.
2. It sets `Response.StatusCode` to `code` if that is between 400 and 599, otherwise 500.
3. If the **original** path has a file extension (`Path.HasExtension`, for example `/img/tiles/laion-5b.jpg`, `/uploads/x.png`, `/favicon.ico`), it returns a bare status code with no HTML. The browser asked for an image or a script, not a page. This is how an empty image slot or a deleted upload fails quietly, and how `site.js` can swap a broken `<img data-image-fallback>` for a friendly placeholder.
4. Otherwise it returns `Views/Home/Status.cshtml` with `StatusCodeViewModel.For(code, originalPath)` (a title and message per code, plus the requested path).

Both `Error` and `Status` have two attributes:

- `[IgnoreAntiforgeryToken]`: a re-executed request keeps its original HTTP method. If a **POST** failed (for example, a form without a valid token returns 400), the error page is also requested as a POST. Without this attribute, the global antiforgery filter would reject that POST again, and the friendly page could never be shown.
- `[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]`: browsers and proxies must never cache an error page.

### 5.2 `Controllers/AIImagesController.cs`

The gallery's create, read, update and delete pages (CRUD) and likes. Constructor dependencies: `ApplicationDbContext` (database), `IImageStorage` (files), `ILikeTracker` (likes this login) and `ILogger<AIImagesController>`.

How a URL `id` is read: the actions that display a page take `int? id`. If the id is missing, not a number (`/AIImages/Edit/abc`) or too large for an `int` (`/AIImages/Delete/2147483648`), it arrives as `null` and the action returns `NotFound()`. The user then sees the friendly **404 Page Not Found** page. Every database read for display uses `AsNoTracking()`, which is faster because EF Core does not need to watch those objects for changes.

#### `Index()`: the gallery list

| | |
|---|---|
| URL / method | `GET /AIImages` |
| Access | `[AllowAnonymous]`: everyone |
| Does | Loads **all** images ordered by `Like` (highest first), then by `UploadDate` (newest first). It also loads the ids this login has already liked (`_likes.LikedImageIdsAsync()`), and for each image sets `canIncreaseLike` to true only if the user is signed in and has not liked it yet. This is the per-viewer flag the brief's model includes. Returns `Views/AIImages/Index.cshtml` |
| View behaviour | Each block is `<article id="image-{id}">`. Signed-in users see a **Like (n)** button (a POST form) or a disabled **Liked (n)** button. Admins also see Edit and Delete, and the image links to Details. Visitors see "n likes" as text |

#### `Details(int? id)`

| | |
|---|---|
| URL / method | `GET /AIImages/Details/5` |
| Access | `[Authorize(Roles = Roles.Admin)]` |
| Does | Finds the image. If the id is null or there is no record, `NotFound()` (404 page). Otherwise returns `Views/AIImages/Details.cshtml` (an Edit button and "Back to List", which links to `/AIImages#image-5`) |

#### `Create(string? prompt)`: GET

| | |
|---|---|
| URL / method | `GET /AIImages/Create` |
| Access | `[Authorize]`: members and admins |
| Does | Builds an `AIImageFormViewModel` with `UploadDate = DateTime.Now`, `Like = 0`. An optional `?prompt=` query value pre-fills the prompt box (trimmed and cut to 2,000 characters). No page currently links with it, so normally the box is empty. Returns `Views/AIImages/Create.cshtml` |

#### `Create(AIImageFormViewModel model)`: POST

| | |
|---|---|
| URL / method | `POST /AIImages/Create` (`multipart/form-data`) |
| Access | `[Authorize]` |
| Extra attributes | `[UploadSizeGuard]` ([section 9](#9-filters-uploadsizeguardattribute)), `[RequestSizeLimit(10 MB)]` and `[RequestFormLimits(MultipartBodyLengthLimit = 10 MB)]` (both use `UploadOptions.RequestLimitBytes`) |

Steps:

1. `model.UploadDate = DateTime.Now; model.Like = 0;`. Whatever the browser sent for these read-only fields is ignored (a test posts `Like=999` and a year-2000 date to prove it).
2. No file chosen → field error *"Please choose an image file to upload."*. Otherwise `_storage.ValidateAsync(file)` returns an error message or `null` ([8.1](#81-servicesimagestoragecs-saving-uploaded-files-safely)).
3. If `ModelState` is invalid (including the `[Required]`/`[StringLength]` rules on Prompt and Image Generator), it returns the same view with the messages (HTTP 200) and the user's text kept.
4. `_storage.SaveAsync(file)` writes the file to `wwwroot/uploads/`. On `IOException` or `UnauthorizedAccessException` (disk full, no permission) it logs the error, shows *"Your image could not be saved. Please try again."* and returns the view.
5. It creates the `AIImage` entity (trimmed Prompt and ImageGenerator, the stored filename, `Like = 0`), then `Add` and `SaveChangesAsync()`. On `DbUpdateException` it logs, **deletes the file it just saved** (so no orphan files are left behind), shows *"Your image could not be saved to the gallery. Please try again."* and returns the view.
6. Success: `TempData["SiteMessage"] = "Thanks! Your {generator} image has been uploaded."`, then a redirect to **`/#upload-ai-images`**, the Upload AI Images block on the Home page, where the new tile appears.

#### `Edit(int? id)`: GET

| | |
|---|---|
| URL / method | `GET /AIImages/Edit/5` |
| Access | `[Authorize(Roles = Roles.Admin)]` |
| Does | Finds the image or returns `NotFound()`. Converts it with `AIImageFormViewModel.FromEntity(image)`, which sets `UploadDate` to *now* (the brief: *"Upload date is automatically set to the current date"*), keeps `Like` and puts the stored filename in `CurrentFilename` so the current image is shown on the right |

#### `Edit(int id, AIImageFormViewModel model)`: POST

| | |
|---|---|
| URL / method | `POST /AIImages/Edit/5` |
| Access | `[Authorize(Roles = Roles.Admin)]`, plus `[UploadSizeGuard]`, `[RequestSizeLimit]`, `[RequestFormLimits]` as for Create |

Steps:

1. If the URL id and the hidden `Id` field differ, it returns `NotFound()`. This stops a tampered form editing a different record.
2. It loads the image **with tracking** (so EF Core can save changes). If it is missing, `NotFound()`.
3. It sets the display-only values from the server: `UploadDate = DateTime.Now`, `Like = image.Like` (from the database, never the form) and `CurrentFilename = image.Filename`.
4. The new file is **optional**. If one was chosen, it is validated exactly as in Create.
5. If the model is invalid, it returns the view with messages.
6. If a new file was chosen, it saves it (same `IOException`/`UnauthorizedAccessException` handling, with the message *"The new image could not be saved. Please try again."*).
7. It updates Prompt, ImageGenerator and UploadDate, and Filename if a new file was saved, then calls `SaveChangesAsync()`.
   - `DbUpdateConcurrencyException` (the record was deleted by someone else between steps 2 and 7): it deletes the new file. If the record is gone it returns `NotFound()`, otherwise it shows *"This image was changed by someone else. Please reload and try again."*.
   - `DbUpdateException`: it logs, deletes the new file and shows *"Your changes could not be saved. Please try again."*.
8. Only after the database save succeeds does it delete the **old** file (when it was replaced). A failed save therefore never leaves a record pointing at a deleted file.
9. `TempData["SiteMessage"] = "The image has been updated."` and a redirect to **`/AIImages#image-5`**, which scrolls to the edited block.

#### `Delete(int? id)`: GET (confirmation page)

| | |
|---|---|
| URL / method | `GET /AIImages/Delete/5` |
| Access | `[Authorize(Roles = Roles.Admin)]` |
| Does | Finds the image or returns `NotFound()`. Returns `Views/AIImages/Delete.cshtml` ("Are you sure you want to delete this?", the details and a Delete button in a POST form) |

#### `DeleteConfirmed(int id)`: POST

| | |
|---|---|
| URL / method | `POST /AIImages/Delete/5`. `[HttpPost, ActionName("Delete")]` keeps the URL the same as the GET page, and the C# method gets a different name (the scaffolding convention) |
| Access | `[Authorize(Roles = Roles.Admin)]` |

Steps:

1. `FindAsync(id)`. If the image no longer exists (for example a double click or two admins), `TempData["SiteMessage"] = "That image had already been deleted."` and a redirect to `/AIImages`. There is no error page, because the outcome the admin wanted has already happened.
2. `Remove` and `SaveChangesAsync()`. On `DbUpdateException` it logs, sets `TempData["SiteError"] = "The image could not be deleted. Please try again."` and redirects back to `/AIImages/Delete/5`.
3. Success: it deletes the file from `wwwroot/uploads/` (after the database delete, for the same reason as Edit), sets `TempData["SiteMessage"] = "The {generator} image has been deleted."` and redirects to `/AIImages`.

#### `Like(int id)`: POST

| | |
|---|---|
| URL / method | `POST /AIImages/Like/5` (the Like button's form) |
| Access | `[Authorize]`: members and admins. A visitor's POST is redirected to Login |

Steps:

1. It calls `_likes.LikeAsync(id)` (see 8.2), which returns `Liked`, `AlreadyLiked`, `NotFound` or `LoginExpired`.
2. `NotFound` → the friendly 404 page. `AlreadyLiked` → toast *"You have already liked this image. Log out and back in to like it again."* `LoginExpired` (a sign-in cookie from before login ids existed) → toast asking the user to log in again.
3. In every other case it redirects to **`/AIImages#image-5`**. The list is re-sorted by likes, and the browser scrolls the liked block to the top of the page, as the brief requires.

A matching `GET /AIImages/Like/5` (`LikeReturn`) simply redirects to `/AIImages#image-5`. It is only reached when someone's login expired before clicking Like: after logging in, the return URL brings them back to the image instead of a "405 Method Not Allowed" page.

`Truncate(string?, int)` is a private helper that is used only by `Create(string? prompt)`.

---

## 6. Data: DbContext, migrations, start-up initialiser and database schema

### 6.1 `Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : IdentityDbContext
{
    public DbSet<AIImage> AIImages => Set<AIImage>();
    ...
}
```

- It inherits from **`IdentityDbContext`**, which already contains the Identity tables (users, roles, claims, logins, tokens).
- `DbSet<AIImage> AIImages` is the gallery table. LINQ queries on it (`_context.AIImages.Where(...)`) become SQL.
- `OnModelCreating` first calls `base.OnModelCreating(builder)`, which is **required** so the Identity tables are configured. Then it:
  - names the table `AIImages`;
  - sets maximum lengths from `AIImageRules`: Prompt 2,000, ImageGenerator 100, Filename 260;
  - adds an **index on `Like`** (`HasIndex(i => i.Like)`), because both gallery lists sort by it.

### 6.2 `Data/Migrations/`

A *migration* is C# code that creates or changes database tables to match the model. EF Core records applied migrations in the `__EFMigrationsHistory` table.

| File | Purpose |
|---|---|
| `20261018040612_InitialCreate.cs` | `Up()` creates every table and index listed below. `Down()` drops them all |
| `20261018040612_InitialCreate.Designer.cs` | Generated metadata: `[DbContext(typeof(ApplicationDbContext))]`, `[Migration("20261018040612_InitialCreate")]` and the model as it was at that moment |
| `ApplicationDbContextModelSnapshot.cs` | The *current* model. `dotnet ef migrations add` compares your code with this file to work out what changed |

Earlier versions of the project had two other migrations (`00000000000000_CreateIdentitySchema` and `20261014005809_AddAIImages`). They were replaced by this single `InitialCreate`. That is why `DbInitializer` can back up an outdated database ([6.3](#63-dbinitializercs-preparing-the-database-at-start-up)).

**`AIImages` table created by `InitialCreate`:**

| Column | SQLite type | Null? | Constraint / note | C# property |
|---|---|---|---|---|
| `Id` | `INTEGER` | NOT NULL | Primary key `PK_AIImages`, auto-increment | `int Id` |
| `Prompt` | `TEXT` | NOT NULL | max length 2000 | `string Prompt` |
| `ImageGenerator` | `TEXT` | NOT NULL | max length 100 | `string ImageGenerator` |
| `UploadDate` | `TEXT` | NOT NULL | SQLite has no date type, so EF Core stores the date as text, for example `2026-10-18 04:23:47.1340509` | `DateTime UploadDate` |
| `Filename` | `TEXT` | NOT NULL | max length 260. Only the file name (for example `my-art-6d070802.png`), never a path | `string Filename` |
| `Like` | `INTEGER` | NOT NULL | Index `IX_AIImages_Like` | `int Like` |
| *(none)* | | | `canIncreaseLike` is `[NotMapped]`, so it is **not** a column ([7.1](#71-modelsaiimagecs)) | `bool canIncreaseLike` |

SQLite does not enforce the maximum lengths itself. The `[StringLength]` validation on the form enforces them (and a database server such as SQL Server would enforce them too). The gallery does not record *who* uploaded an image, so there is no link from `AIImages` to the users table.

### 6.3 DbInitializer.cs: preparing the database at start-up

`DbInitializer.InitialiseAsync(app.Services)` is called once from `Program.cs`. It creates a DI **scope**, because `ApplicationDbContext`, `UserManager` and `RoleManager` are scoped services and cannot be taken from the root container. Then it runs these steps:

1. **`ReplaceOutdatedDevelopmentDatabaseAsync`.** If the database is SQLite and can be opened, it compares the migrations recorded in `__EFMigrationsHistory` with the migrations in the code. If the database contains migrations the code does not know (for example an `app.db` created by an earlier version of this project), then:
   - in **Development**, for a real file (not `:memory:`): it closes the connection, clears the SQLite connection pool and **renames** `app.db` to `app.db.bak-yyyyMMddHHmmss` (and `app.db-shm`/`app.db-wal` likewise). Nothing is deleted. It logs a warning, and step 2 then creates a fresh database;
   - in any other environment: it only logs a critical message telling you to delete the file, because a production database must never be moved automatically.
2. **`db.Database.MigrateAsync()`** creates `app.db` if it does not exist and applies any migrations not yet applied.
3. **`EnsureRolesAsync`** creates the `Admin` role if it is missing.
4. **`EnsureAccountsAsync`**: for every `SeedAccounts:Accounts` entry with an e-mail:
   - if no user has that e-mail, it creates one with `UserName = Email`, `EmailConfirmed = true` and the configured password. If creation fails (for example the password breaks the password rules), the reason is logged and that account is skipped;
   - if `Role` is not empty and the user is not already in it, it adds the role. This also gives the role to an *existing* user if you add `"Role": "Admin"` later.
5. Every result is logged (`Created role Admin`, `Created account admin@asp.net`, ... or `Could not create ...: <reasons>`).

The whole method is inside `try { ... } catch (Exception ex) { logger.LogCritical(ex, "The database could not be initialised."); }`. If the database is broken, the site **still starts**. Pages that need the database then show the friendly error page, and the log says what went wrong. The home page still renders, because its gallery is protected by the view component ([11.3](#113-viewcomponentsuploadgalleryviewcomponentcs)).

The gallery starts **empty**. There is no sample data, and members add images through the Create page.

### 6.4 Complete database schema

```text
app.db (SQLite)
|
+-- AIImages                 Id PK | Prompt | ImageGenerator | UploadDate | Filename | Like      [IX_AIImages_Like]
|
+-- AspNetUsers              Id PK | UserName | NormalizedUserName | Email | NormalizedEmail | EmailConfirmed |
|                            PasswordHash | SecurityStamp | ConcurrencyStamp | PhoneNumber | PhoneNumberConfirmed |
|                            TwoFactorEnabled | LockoutEnd | LockoutEnabled | AccessFailedCount
|                            [UserNameIndex (unique), EmailIndex]
+-- AspNetRoles              Id PK | Name | NormalizedName | ConcurrencyStamp        [RoleNameIndex (unique)]
+-- AspNetUserRoles          UserId FK -> AspNetUsers | RoleId FK -> AspNetRoles      (PK = both)   <- who is Admin
+-- AspNetUserClaims         Id PK | UserId FK | ClaimType | ClaimValue
+-- AspNetUserLogins         LoginProvider + ProviderKey PK | ProviderDisplayName | UserId FK   (external logins, unused)
+-- AspNetUserTokens         UserId + LoginProvider + Name PK | Value                          (2FA / recovery tokens)
+-- AspNetRoleClaims         Id PK | RoleId FK | ClaimType | ClaimValue
|
+-- __EFMigrationsHistory    MigrationId PK | ProductVersion                         (applied migrations)
```

The Identity foreign keys use cascade delete, so deleting a user also deletes their role links, claims, logins and tokens. Passwords are stored only as hashes (`PasswordHash`).

---

## 7. Models and view models

### 7.1 `Models/AIImage.cs`

This is the model from the brief, with the same constructor and properties. It adds `[StringLength]` limits so the columns are bounded.

| Property | Attributes | Notes |
|---|---|---|
| `Id` | `[Required]` | Primary key (EF Core uses a property named `Id` as the key automatically) |
| `Prompt` | `[Required]`, `[StringLength(2000)]` | The prompt typed into the AI tool |
| `ImageGenerator` | `[Required]`, `[Display(Name = "Image Generator")]`, `[StringLength(100)]` | For example Midjourney |
| `UploadDate` | `[Display(Name = "Upload Date")]` | Always set on the server |
| `Filename` | `[StringLength(260)]` | Stored file name inside `wwwroot/uploads/` |
| `Like` | | Number of likes |
| `canIncreaseLike` | **`[NotMapped]`** | See below |

**Why `canIncreaseLike` is `[NotMapped]`:** whether the Like button is active depends on **who is looking**. A visitor can never like, and a member who already liked image 3 during this login cannot like it again, but another member still can. One database column could only hold a single true/false value for everyone. So the value is **calculated on every request** in `AIImagesController.Index` (using the likes stored for the current login) and never stored. `[NotMapped]` tells EF Core not to create a column for it, and the migration indeed has none. The lower-case name is kept exactly as in the brief.

`AIImageRules` (same file) holds the limits in one place: `PromptMaxLength = 2000`, `GeneratorMaxLength = 100`, `FilenameMaxLength = 260`. They are used by the model, the `DbContext` and the form view model.

### 7.2 `Models/ErrorViewModel.cs`

- **`ErrorViewModel`**: `RequestId` and `ShowRequestId` (true when there is an ID). Used by `Views/Shared/Error.cshtml`.
- **`StatusCodeViewModel`**: `StatusCode`, `Title`, `Message`, `OriginalPath`. `StatusCodeViewModel.For(code, path)` chooses the text:

| Code | Title | Message |
|---|---|---|
| 400 | Bad Request | Your browser sent a request we could not understand. If you were submitting a form, please reload the page and try again. |
| 403 | Access Denied | You do not have permission to view this page. |
| 404 | Page Not Found | We searched every gallery wall but could not find that page. It may have been moved or deleted. |
| 405 | Method Not Allowed | That page cannot be used in this way. Please use the links and buttons on the site. |
| 413 | Upload Too Large | The file you tried to upload is too large. Please choose an image smaller than 5 MB. |
| anything else | Something Went Wrong | The request could not be completed. Please try again in a moment. |

### 7.3 `ViewModels/AIImageFormViewModel.cs`

This is the model behind the Create and Edit form (`Views/AIImages/_ImageForm.cshtml`). A separate form model is used, instead of binding straight to `AIImage`, to prevent **over-posting**. The entity is never filled from the form, so a user cannot set `Like`, `UploadDate` or `Filename` by editing the HTML.

| Property | Validation | Notes |
|---|---|---|
| `Id` | | 0 on Create, the record's id on Edit (hidden field) |
| `Prompt` | `[Required]` *"Please enter the prompt used to generate the image."*. `[StringLength(2000, MinimumLength = 5)]` *"The prompt must be between 5 and 2000 characters."* | Textarea with 5 rows |
| `ImageGenerator` | `[Required]` *"Please enter the image generator (for example Midjourney)."*. `[StringLength(100, MinimumLength = 2)]` *"The image generator must be between 2 and 100 characters."* | Displayed as "Image Generator" |
| `UploadDate` | | Read-only input. Overwritten by the server on every POST |
| `Like` | | Read-only input. Overwritten by the server on every POST |
| `ImageFile` | (checked in the controller) | `IFormFile?`, the uploaded file. Required on Create, optional on Edit |
| `CurrentFilename` | | Edit only: shows the current image |

`FromEntity(AIImage)` copies an entity into the form for Edit, setting `UploadDate = DateTime.Now`. Because the same DataAnnotations drive the jQuery Validation scripts (`_ValidationScriptsPartial`), most mistakes are caught in the browser, and again on the server.

---

## 8. Services

### 8.1 `Services/ImageStorage.cs`: saving uploaded files safely

Interface `IImageStorage`:

| Method | Returns | Purpose |
|---|---|---|
| `ValidateAsync(IFormFile file, ...)` | `string?`: an error message, or `null` when valid | Checks size, extension and file signature |
| `SaveAsync(IFormFile file, ...)` | `string`: the stored file name | Saves a validated file under `wwwroot/uploads` |
| `Delete(string? filename)` | `bool` | Deletes a stored file. **Never throws**, and returns `false` if the file could not be removed |

The constructor works out the upload folder once: `Path.GetFullPath(Path.Combine(environment.WebRootPath, options.FolderName))`, which is the absolute path of `wwwroot/uploads`.

**Validation rules (`ValidateAsync`), checked in this order:**

| # | Rule | Message if it fails |
|---|---|---|
| 1 | The file is not empty (`Length == 0`) | The selected file is empty. Please choose an image. |
| 2 | Size ≤ `Uploads:MaxFileBytes` (5 MB) | The image is 6 MB. Please choose an image smaller than 5 MB. *(the real sizes are filled in)* |
| 3 | The extension is `.jpg`, `.jpeg`, `.png`, `.gif` or `.webp` (in any letter case) | Only JPG, PNG, GIF or WEBP images can be uploaded. |
| 4 | The first 12 bytes can be read | The file could not be read. Please try uploading it again. *(an `IOException` is logged as a warning)* |
| 5 | The first bytes (the "magic number") match the extension | This file is not a valid image (its contents do not match its extension). |

The **magic-byte signatures** in the `Signatures` dictionary:

| Extension | File must start with |
|---|---|
| `.jpg`, `.jpeg` | `FF D8 FF` |
| `.png` | `89 50 4E 47 0D 0A 1A 0A` (`‰PNG` followed by line-ending bytes) |
| `.gif` | `GIF87a` or `GIF89a` |
| `.webp` | `RIFF` at bytes 0-3 **and** `WEBP` at bytes 8-11 |

Renaming `virus.exe` to `cat.png`, or a text file to `photo.jpg`, therefore fails. A real PNG named `.jpg` also fails, because the contents must match the claimed type. `AllowedExtensions` exposes the list of allowed extensions, but nothing else uses it yet. The file picker's `accept` list is written out in `_ImageForm.cshtml`.

**Safe file names (`CreateSafeFilename`, public and static):**

1. Take the extension and change it to lower case.
2. Take the name without its extension. `Path.GetFileNameWithoutExtension` already drops any folder part, such as `../../`.
3. Change it to lower case and replace every run of characters other than `a-z` or `0-9` with `-`. Trim `-` from both ends.
4. Cut it to 40 characters. If nothing is left, use `ai-image`.
5. Add `-` and 8 random hexadecimal characters from a new GUID, then the extension.

| Uploaded name | Stored as (example) |
|---|---|
| `Mona Lisa!! (final).JPG` | `mona-lisa-final-3f9a2c1b.jpg` |
| `../../etc/passwd.png` | `passwd-1b2c3d4e.png` |
| `???.png` | `ai-image-9a8b7c6d.png` |

The user's name is kept so the files are easy to recognise, and the random part makes each name unique. `SaveAsync` opens the file with `FileMode.CreateNew`, which **never overwrites** an existing file. In the extremely unlikely event of a name clash, it throws an `IOException`, which the controller turns into *"Your image could not be saved. Please try again."*. `SaveAsync` also creates the folder if it is missing and logs `Saved uploaded image {Filename} ({Bytes} bytes)`.

**Path-traversal protection (`Delete` → `ResolvePath`):** file names come from the database, and `Delete` must never be tricked into deleting something like `../appsettings.json`. `ResolvePath` returns `null` (so nothing is deleted) when:

- the name is blank;
- the name contains any folder part (`filename != Path.GetFileName(filename)`);
- the full resolved path does not start with the uploads folder plus a path separator.

If the file is missing, `Delete` returns `false`. If deleting fails (`IOException`/`UnauthorizedAccessException`, for example the file is open in another program), it logs a warning and returns `false`. The controller carries on, because a leftover file is harmless. `Delete(null)` does nothing, which keeps the Edit code simple.

### 8.2 `Services/LikeTracker.cs`: one like per image per login

Interface `ILikeTracker`: `LikedImageIdsAsync()` (the images this login has already liked) and `LikeAsync(int imageId)` (returns a `LikeResult`).

Implementation `LoginLikeTracker`:

- At every sign-in, `Authorization/LoginSession.cs` adds a random **login id** claim to the sign-in cookie (the `OnSigningIn` cookie event in `Program.cs`).
- A like is saved as a row in the **`ImageLikes`** table (`Models/ImageLike.cs`): image id, login id, user id and time. A **unique index on (LoginId, AIImageId)** means the same login can never like the same image twice, even with two clicks at the same moment.
- `LikeAsync` checks that the image exists and that this login has not liked it. It then inserts the row and, in the same transaction, increases the count with one SQL statement: `UPDATE "AIImages" SET "Like" = "Like" + 1 WHERE "Id" = @id`. The database adds 1 itself, so likes from different people at the same moment are never lost.
- Logging out and in again creates a new login id, so each image can be liked once more, exactly as the brief describes. The rule survives page reloads and site restarts, because it is stored in the database.
- Deleting an image also deletes its likes (a cascade delete).

### 8.3 `Services/MediaLibrary.cs`: which images have been added

The site ships without images. Every image position is an **image slot** (defined in `Content/ImageLibrary.cs`). `IMediaLibrary` answers two questions for each slot: *has the owner added this file yet?* and *where did it come from?*

| Member | Does |
|---|---|
| `Find(string key)` | Returns the slot with that key (case-insensitive), or `null` |
| `HasFile(ImageSlot slot)` / `HasFile(string key)` | `_environment.WebRootFileProvider.GetFileInfo(slot.File).Exists`, checked **on every call**, so a newly saved image appears on the next refresh without restarting the site |
| `CreditFor(ImageSlot slot)` | The `ImageCredits:<key>` entry from configuration, or an empty `ImageCredit` |
| `All()` | A list of `ImageSlotStatus(Slot, HasFile, Credit)` for every slot. The Contact page uses it to list credits, "Image not added yet" and "Reference needed" |

Credits are read through `IOptionsMonitor<ImageCreditOptions>.CurrentValue`, so edits to `appsettings.json` show without a restart. Users of the service: `_Layout.cshtml` (adds the favicon `<link>` only if `favicon.ico` exists, and adds the `has-hero-image`/`has-section-image` body classes that switch on the CSS background images), `Contact.cshtml`, and the two tag helpers ([11.1](#111-taghelpersimageslottaghelpercs)). The step-by-step guide to filling slots is in [04-images-content-and-references.md](04-images-content-and-references.md).

---

## 9. Filters: UploadSizeGuardAttribute

[`Filters/UploadSizeGuardAttribute.cs`](../AICreativeArts/Filters/UploadSizeGuardAttribute.cs) is applied to the two upload actions (`Create` POST and `Edit` POST).

**The problem it solves:** if someone sends a 50 MB file, the 10 MB request limit makes reading the form fail. The antiforgery check, which reads the form first, would then turn that into an unfriendly error, and a browser that is still sending the file may show *"connection reset"* instead of any page.

**How it works:**

- It is an **authorisation filter** (`IAsyncAuthorizationFilter`) with `Order => -5000`, so it runs before every other MVC filter, *before* the request body is read. The site's actual run order is: `UploadSizeGuardAttribute (-5000)` → `RequestSizeLimitFilter` → `RequestFormLimitsFilter` → `AutoValidateAntiforgeryTokenAuthorizationFilter`. The `[Authorize]` check has already happened in the authorisation middleware.
- It looks only at the `Content-Length` header. If it is missing or ≤ `UploadOptions.RequestLimitBytes` (10 MB), the filter does nothing. Files between 5 and 10 MB therefore reach the action and get the precise field message from `ImageStorage` ("The image is 6 MB...").
- If the request is larger than 10 MB:
  1. If it is ≤ 100 MB (`DrainLimitBytes`), the filter raises this request's body limit to its own length and **reads and discards** the body (`CopyToAsync(Stream.Null)`), so the browser finishes sending and can show the redirect. If the client gives up (`IOException`/`OperationCanceledException`), that is ignored.
  2. It writes `TempData["SiteError"] = "That upload is far too large. Please choose an image smaller than 5 MB."` and calls `tempData.Save()` itself. Short-circuiting in an authorisation filter skips the later filters that would normally save TempData.
  3. It sets `context.Result` to a redirect to the **same** action and controller (and `id`, for Edit), that is `GET /AIImages/Create` or `GET /AIImages/Edit/5`, where the red toast appears.

Normally `site.js` stops a too-large file before it is sent (it reads `data-max-bytes` on the file input), and this filter covers browsers with JavaScript turned off. A client that sends no `Content-Length` header is not caught by the guard. Browsers always send one for form uploads, so in practice only scripted clients do this. Such a request is stopped by the 10 MB hard limit instead and ends on the generic "Something Went Wrong" status page.

---

## 10. Infrastructure: options classes and URL helpers

### 10.1 `Infrastructure/Options.cs`

These are plain classes that ASP.NET Core fills from `appsettings.json` (the *options pattern*). Each has a `SectionName` constant that `Program.cs` uses.

| Class | Section | Properties (defaults) | Used by |
|---|---|---|---|
| `SiteOptions` | `Site` | `Name` (`"AI Unmasked"`), `DeveloperName` (`""`), `ContactEmail` (`""`) | `HomeController.Contact` |
| `UploadOptions` | `Uploads` | `FolderName` (`"uploads"`), `MaxFileBytes` (5 MB), and the **constant** `RequestLimitBytes = 10 MB` | `ImageStorage`, `SiteUrlExtensions`, `_ImageForm.cshtml`, the upload attributes |
| `SeedAccountOptions` | `SeedAccounts` | `Accounts` (`List<SeedAccount>`) | `DbInitializer` |
| `SeedAccount` | | `Email`, `Password`, `Role` (`null`/empty = member) | |
| `ImageCredit` | | `Creator`, `Source`, `Url`, `Licence`, plus the computed `IsComplete` (Creator and Source both filled in) and `HasLink` (absolute `http`/`https` URL) | Contact page |
| `ImageCreditOptions` | `ImageCredits` | A `Dictionary<string, ImageCredit>` with **case-insensitive** keys (slot key → credit) | `MediaLibrary` |

`RequestLimitBytes` is a `const`, not a setting, because attributes such as `[RequestSizeLimit(...)]` need a compile-time value. It is deliberately twice `MaxFileBytes`, so files a little too large still get a friendly field message.

### 10.2 `Infrastructure/SiteUrlExtensions.cs`

These are extension methods on `IUrlHelper` (the `Url` object in views and controllers):

| Method | Example result | Used in |
|---|---|---|
| `Url.UploadedImage(filename)` | `/uploads/my-art-6d070802.png` (the file name is URL-escaped, the folder comes from `Uploads:FolderName`). Blank name → `""` | `Index.cshtml`, `_ImageForm.cshtml`, `_ImageDetails.cshtml`, the upload gallery |
| `Url.ImageAnchor(id)` | `/AIImages#image-5` | Redirects after Like and Edit, the home gallery tiles, Details' "Back to List" |
| `SiteUrlExtensions.AnchorId(id)` | `image-5` | The `id` attribute of each `<article>` in `Views/AIImages/Index.cshtml` |

Because the anchor text is built in one place, the link and its target always match.

---

## 11. Tag helpers and the view component

Tag helpers are C# classes that turn custom tags or attributes in `.cshtml` files into HTML on the server. They are enabled for the whole project by `@addTagHelper *, AICreativeArts` in `Views/_ViewImports.cshtml` and `Areas/Identity/Pages/_ViewImports.cshtml`.

### 11.1 `TagHelpers/ImageSlotTagHelper.cs`

This file contains two tag helpers.

**`<image-slot key="..." class="..." lazy="true|false" />`** (`ImageSlotTagHelper`). Example: `<image-slot key="@example.SlotKey" class="tool-tile-image" />` in `Views/Home/_CaseTiles.cshtml`.

| Situation | Output |
|---|---|
| Unknown key | Nothing (`SuppressOutput`) |
| The slot's file exists in `wwwroot` | `<img src="/img/tiles/laion-5b.jpg?v=..." alt="..." decoding="async" loading="lazy" class="...">`. The `?v=` part comes from `IFileVersionProvider`, so replacing the file with a new one under the same name is never hidden by the browser cache. `lazy="false"` leaves out `loading="lazy"` (used for the Create page preview) |
| File missing | `<span role="img" aria-label="Image placeholder: ..." data-image-slot="tile-laion" class="image-slot ...">` containing an "Image needed" badge, the description of the picture to add, the path to save it to (`wwwroot/img/tiles/laion-5b.jpg`) and the recommended size. All text is HTML-encoded |

**`<background-slot-note key="..." />`** (`BackgroundSlotNoteTagHelper`). It is used for the CSS background slots (`hero-background`, `section-background`) on Home, Contact and Artist Shield. While the file is missing it renders `<p class="slot-note">Background image needed: <description> Save it as wwwroot/... (size).</p>`. Once the file exists, or for an unknown key, it renders nothing.

### 11.2 `TagHelpers/NavActiveTagHelper.cs`

The `nav-active` attribute on `<a>` tags highlights the menu link for the current page:

```html
<a class="nav-link" nav-active="Home/Index" asp-controller="Home" asp-action="Index">Home</a>
<a class="nav-link" nav-active="AIImages/*" asp-controller="AIImages" asp-action="Index">AI Images</a>
```

The attribute value is `Controller/Action`, `Controller/*` (any action) or just `Controller`. If it matches the current route values (ignoring letter case), the helper adds `class="active"` and `aria-current="page"` (which screen readers announce). The `nav-active` attribute itself is not written to the HTML.

### 11.3 `ViewComponents/UploadGalleryViewComponent.cs`

A *view component* is like a small controller and view that a page can embed. `Views/Home/Index.cshtml` calls `@await Component.InvokeAsync("UploadGallery")` inside the **Upload AI Images** block (`<section id="upload-ai-images">`).

- `InvokeAsync(int maxItems = 24)` loads the 24 most-liked images (ties broken by newest), using `AsNoTracking()`. It also counts all images and returns `UploadGalleryViewModel(Images, TotalCount)` to `Views/Shared/Components/UploadGallery/Default.cshtml`. That view lists each tile linking to `/AIImages#image-{id}`, and adds "See all N AI images" when there are more than 24.
- `UploadGalleryViewModel` is a record: `(IReadOnlyList<AIImage> Images, int TotalCount, bool Unavailable = false)`.
- **Error handling:** any exception except `OperationCanceledException` is logged (`"Could not load the upload gallery."`), and the component returns `Unavailable: true`. The view then shows *"The gallery is taking a short break. Please refresh the page in a moment."* and **the rest of the home page still renders**. `OperationCanceledException` is let through because it only means the visitor closed the page (`HttpContext.RequestAborted`).

---

## 12. Content classes

The text and data are kept in C# so the views stay short, and so the Contact page can list every source automatically. How to edit them is covered in [04-images-content-and-references.md](04-images-content-and-references.md).

| File | Contains |
|---|---|
| `Content/SiteContent.cs` | The hero texts (`HeroEyebrow`, `HeroTitle`, `HeroText`, `HeroSource`, `HeroSourceUrl`), the three home page case files in `Sections` (Art Without Consent, Livelihoods at Risk, Fakes and Deception), each with two tiles, and `TextSources` (every text source, listed on the Contact page) |
| `Content/FeatureSection.cs` | The records used by `Sections`: `FeatureSection(Id, CaseLabel, Title, Description, KeyFact, ReadMoreUrl, TextFirst, Examples)` and `CaseExample(Name, SlotKey, HoverText, Source, Url)`. `TextFirst` decides which side the text card sits on, and `SlotKey` links a tile to its image slot |
| `Content/ArtistShieldContent.cs` | Data for the Artist Shield page: `Quiz` (6 `QuizQuestion`s with options, the correct index, an explanation and a source), `Steps` (8 `ShieldStep`s for the protection checklist) and `Timeline` (9 `TimelineEntry`s) |
| `Content/ImageLibrary.cs` | `ImageSlotKind` (`Image`, `Background`, `Icon`), the `ImageSlot(Key, File, Kind, UsedOn, Description, Recommended, Alt)` record, and `ImageLibrary.Slots`, the 10 slots (constants `HeroBackground`, `SectionBackground`, `UploadPreview`, `Favicon` name the special ones). `ImageLibrary.Find(key)` is case-insensitive. This list drives the placeholders, the Contact page credits and `wwwroot/img/README.md` |

---

## 13. Identity pages: Login, Register and AccessDenied

ASP.NET Core Identity ships ready-made pages in the `Microsoft.AspNetCore.Identity.UI` package. A project can **override** a page by putting a file at the same path under `Areas/Identity/Pages/`. This project overrides three of them, to match the site's design and to make them safer. The other pages (Logout, Lockout, LoginWith2fa, ForgotPassword, Manage, ...) still come from the package. `Areas/Identity/Pages/_ViewStart.cshtml` sets `Layout = "/Views/Shared/_Layout.cshtml"`, so every Identity page, whether overridden or default, uses the site's menu and footer.

### 13.1 `Login.cshtml.cs`: `LoginModel`

`[AllowAnonymous]`. URL: `/Identity/Account/Login`.

| Member | Purpose |
|---|---|
| `InputModel` | `Email` (`[Required]`, `[EmailAddress]`), `Password` (`[Required]`), `RememberMe` ("Remember me?"). `RememberMe` makes the cookie survive closing the browser |
| `ReturnUrl` | Where to go after logging in (for example `/AIImages/Create`) |
| `ErrorMessage` (`[TempData]`) | An error passed from another Identity page, shown in the summary |
| `OnGetAsync(returnUrl)` | Makes the return URL safe. If the user is already signed in, it redirects there. Otherwise it signs out any external-login cookie and shows the form |
| `OnPostAsync(returnUrl)` | Validates the form, then `PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true)` |

Outcomes of `OnPostAsync`:

| Result | What happens |
|---|---|
| Success | Logs `User {Email} logged in.`. The cookie event resets the like list, and the user is sent to `ReturnUrl` |
| Two-factor required | Redirect to `./LoginWith2fa` (not used by the seed accounts) |
| Locked out (the 5th wrong password) | Logs a warning and redirects to `./Lockout`, the default page: *"This account has been locked out, please try again later."* Login stays blocked for 5 minutes |
| Wrong e-mail or password | *"Invalid login attempt. Please check your email and password."* |

`SafeReturnUrl` accepts the return URL only if `Url.IsLocalUrl(returnUrl)` is true. Otherwise it uses `/`. This prevents **open redirects** (a link like `...Login?returnUrl=https://evil.example` cannot send users to another site). It also avoids the exception `LocalRedirect` would throw for an external URL. A test checks this.

### 13.2 `Register.cshtml.cs`: `RegisterModel`

`[AllowAnonymous]`. URL: `/Identity/Account/Register`.

| Member | Purpose |
|---|---|
| `InputModel` | `Email` (`[Required]`, `[EmailAddress]`), `Password` (`[Required]`, 6-100 characters), `ConfirmPassword` (`[Compare("Password")]`: *"The password and confirmation password do not match."*) |
| `OnGetAsync(returnUrl)` | Makes the return URL safe and redirects if the user is already signed in |
| `OnPostAsync(returnUrl)` | Validates the form, then `UserManager.CreateAsync(new IdentityUser { UserName = Email, Email = Email }, Password)` |

- **Success:** logs `User {Email} created a new account.`, signs the user in (`isPersistent: false`), sets `TempData["SiteMessage"] = "Welcome to AI Unmasked! Your account is ready, so you can now upload AI images."` and redirects to `ReturnUrl`. The home page's "register" link passes `returnUrl=/AIImages/Create`, so a new member lands straight on the upload form. New accounts have **no role**, so they are members.
- **Failure:** each Identity error is added to the page. For example: *"Passwords must have at least one uppercase ('A'-'Z')."*, *"Passwords must have at least one non alphanumeric character."*, *"Email 'member@asp.net' is already taken."*.

There is no e-mail sender. Because `RequireConfirmedAccount = false`, no confirmation e-mail is needed. The default "Forgot your password?" and "Resend email confirmation" pages open but cannot actually send e-mail.

### 13.3 `AccessDenied.cshtml.cs`: `AccessDeniedModel`

URL: `/Identity/Account/AccessDenied`. The cookie handler sends a signed-in member here, with `?ReturnUrl=/AIImages/Edit/5`, when they open an admin-only page. `OnGet(returnUrl)` stores the URL in `AttemptedUrl` **only if it is local**, and the page shows *"Only administrators can view details, edit or delete AI images."*, the requested URL and buttons back to AI Images and Home.

---

## 14. Walkthroughs

### 14.1 A member uploads an image

1. The member is signed in, so the Home page's upload block (`_UploadCallToAction.cshtml`) shows **"Click here to upload a new AI image"**, which links to `GET /AIImages/Create`.
2. `UseAuthentication` reads the `.AIUnmasked.Auth` cookie. `UseAuthorization` sees `[Authorize]` and lets the signed-in user through.
3. `AIImagesController.Create(string? prompt)` builds an `AIImageFormViewModel` (`UploadDate = now`, `Like = 0`). `Create.cshtml` → `_ImageForm.cshtml` renders the form with `enctype="multipart/form-data"`. The form tag helper adds the hidden antiforgery token, and the `upload-preview` image slot is the default picture.
4. The member types a prompt and a generator and picks a file. `site.js` checks the size against `data-max-bytes` and shows a preview ([03-front-end.md](03-front-end.md)).
5. **Create** sends `POST /AIImages/Create`. Before the action runs:
   - authorisation middleware: the user is still signed in;
   - `UploadSizeGuardAttribute`: `Content-Length` ≤ 10 MB, so it continues (above 10 MB the member would be redirected back with the red toast);
   - request size and form limits are set to 10 MB;
   - the antiforgery token is validated (a 400 page if it is missing);
   - model binding fills `AIImageFormViewModel` and checks the DataAnnotations.
6. The action overwrites `UploadDate` and `Like`, checks a file was chosen and calls `ImageStorage.ValidateAsync` (size, extension, magic bytes). Any problem → the form is shown again with messages.
7. `ImageStorage.SaveAsync` writes, for example, `wwwroot/uploads/lighthouse-4c1d2e3f.png`.
8. A new `AIImage` is added and `SaveChangesAsync()` runs `INSERT INTO "AIImages" ...` against `app.db`. If that fails, the saved file is deleted and an error message is shown.
9. `TempData["SiteMessage"]` is set and the browser is redirected to `/#upload-ai-images`.
10. The Home page loads. `_SiteToasts.cshtml` shows *"Thanks! Your ... image has been uploaded."*, and `UploadGalleryViewComponent` lists the new tile among the others, most-liked first.

### 14.2 A member clicks Like

1. `GET /AIImages`: `Index()` loads all images sorted by likes, plus the ids this login has already liked (`LikedImageIdsAsync`). For each image, `canIncreaseLike` is true only if the user is signed in and has not liked it during this login.
2. The view shows **Like (n)** as a submit button in `<form class="like-form" method="post" action="/AIImages/Like/{id}">` with the antiforgery token. Already-liked images show a disabled **Liked (n)** button.
3. The member clicks Like, which sends `POST /AIImages/Like/7`. The authorisation middleware (`[Authorize]`) and the antiforgery filter both pass.
4. `LikeAsync(7)` inserts a row in `ImageLikes` for this login id, and adds 1 to the image's `Like` column in the same transaction.
5. The browser is redirected to `/AIImages#image-7`. The list is re-sorted, the block now shows a disabled **Liked (n + 1)**, and `site.js` scrolls that block to the top of the page.
6. If the same POST is sent again (for example from another tab), the existing row is found and the toast says *"You have already liked this image. Log out and back in to like it again."*
7. When the member logs out and in again, the new sign-in gets a new login id, so each image can be liked once more.

### 14.3 An admin edits and then deletes an image

1. On `/AIImages` the admin sees **Edit** and **Delete** (because `User.IsInRole(Roles.Admin)` is true in the view) and clicks Edit, which opens `GET /AIImages/Edit/7`.
2. `[Authorize(Roles = Roles.Admin)]` passes. `Edit(7)` loads the record (or shows 404) and fills the form with `FromEntity`, so Upload Date shows *now*, Like is unchanged and the current image appears on the right.
3. The admin changes the prompt and, optionally, picks a new image, then clicks **Save**, which sends `POST /AIImages/Edit/7`.
4. The same filters as for Create run. The action checks that the URL id equals the form's `Id` and reloads the record with tracking.
5. If there is a new file, it is validated and saved. Prompt, ImageGenerator, UploadDate (and Filename) are updated, and `SaveChangesAsync()` runs an `UPDATE`.
6. On success the old file is deleted (if it was replaced), the toast *"The image has been updated."* is set and the browser goes to `/AIImages#image-7`.
7. Later the admin clicks **Delete**, which opens `GET /AIImages/Delete/7`: *"Are you sure you want to delete this?"* with the record's details.
8. Clicking **Delete** sends `POST /AIImages/Delete/7`, handled by `DeleteConfirmed(7)`. The record is removed (`DELETE FROM "AIImages" ...`), then its file is removed from `wwwroot/uploads/`. The toast says *"The ... image has been deleted."*, and the browser returns to `/AIImages`.

### 14.4 A visitor opens an admin page

1. A visitor (not signed in) types `/AIImages/Edit/7`.
2. Routing chooses `AIImagesController.Edit`. `UseAuthentication` finds no cookie, so the user is anonymous.
3. `UseAuthorization` sees `[Authorize(Roles = Roles.Admin)]`. For an anonymous user this is a **challenge**: the cookie handler answers `302 Found` with `Location: /Identity/Account/Login?ReturnUrl=%2FAIImages%2FEdit%2F7`. The controller never runs.
4. The visitor logs in. `LoginModel` checks that the return URL is local and redirects back to `/AIImages/Edit/7`.
5. Now there are two possibilities:
   - they logged in as the **admin**: the role check passes and the Edit page opens;
   - they logged in as a **member**: they are signed in but not in the role, so this is a **forbid**. The cookie handler redirects to `/Identity/Account/AccessDenied?ReturnUrl=%2FAIImages%2FEdit%2F7`, and `AccessDeniedModel` shows the *Access Denied* page with the requested URL and links back.

---

## 15. Exception handling

The brief deducts marks for every runtime exception found, so the site handles errors at three levels:

1. **Expected problems** (bad input, missing records, failed saves) are handled where they happen, with a clear message.
2. **Error status codes** (404, 405, 400, ...) become a friendly page through `UseStatusCodePagesWithReExecute` and `HomeController.Status`.
3. **Anything unexpected** is caught by `UseExceptionHandler`, which logs it and shows the friendly error page. No stack trace is ever shown to visitors.

| Situation | What the user sees | Where it is handled |
|---|---|---|
| Any unhandled exception in a controller, view or service | *"Oops! Something went wrong"* page with a reference ID and links Home and to AI Images (HTTP 500). Details are in the log | `UseExceptionHandler("/Home/Error")` → `HomeController.Error` (logs) → `Views/Shared/Error.cshtml` |
| Unknown URL, for example `/this-page-does-not-exist` | *Page Not Found* page showing the requested path (HTTP 404) | `UseStatusCodePagesWithReExecute` → `HomeController.Status` → `StatusCodeViewModel.For(404)` |
| `/AIImages/Details`, `Edit` or `Delete` with no id, a non-numeric id (`abc`), an id too large for `int`, or an id with no record | *Page Not Found* (404) | `id is null ? null : ...` → `NotFound()` in `AIImagesController` → status page |
| A missing file: an image slot not filled yet, a deleted upload, `/favicon.ico` | Plain 404 with no HTML. The page shows the slot's placeholder, and `site.js` swaps a broken upload `<img data-image-fallback>` for a friendly fallback | `HomeController.Status` (`Path.HasExtension`), the `<image-slot>` tag helper, `site.js` |
| A GET to a POST-only URL, for example opening `/AIImages/Like/1` in the address bar | *Method Not Allowed* (405) | Routing → status page |
| A form posted without a valid antiforgery token (for example after logging in or out in another tab, after the browser deleted its cookies, or a forged request from another site) | *Bad Request* (400): "...please reload the page and try again." | Global `AutoValidateAntiforgeryTokenAttribute` → status page. `[IgnoreAntiforgeryToken]` on `Status` lets the page render for the POST |
| A visitor opens Create, Details, Edit or Delete, or posts Like | Redirect to the Login page, then back to the page they wanted | `[Authorize]` + `LoginPath` in `Program.cs` |
| A member opens Details, Edit or Delete | *Access Denied* page | `[Authorize(Roles = Roles.Admin)]` + `AccessDeniedPath` → `AccessDeniedModel` |
| `/Home/Status` typed directly | Redirect to Home | `HomeController.Status` |
| Upload: no file chosen | Field message: *Please choose an image file to upload.* | `AIImagesController.Create` (and jQuery Validation in the browser, via `data-val-required` on the Create form's file input) |
| Upload: empty file | *The selected file is empty. Please choose an image.* | `ImageStorage.ValidateAsync` |
| Upload: file between 5 and 10 MB | *The image is 6 MB. Please choose an image smaller than 5 MB.* | `ImageStorage.ValidateAsync` (and `site.js` before sending) |
| Upload: request larger than 10 MB | Back on the same form with a red toast: *That upload is far too large. Please choose an image smaller than 5 MB.* | `UploadSizeGuardAttribute` |
| Upload: wrong extension (`.exe`, `.bmp`, no extension) | *Only JPG, PNG, GIF or WEBP images can be uploaded.* | `ImageStorage.ValidateAsync` |
| Upload: contents do not match the extension (renamed or fake file) | *This file is not a valid image (its contents do not match its extension).* | `ImageStorage.ValidateAsync` (magic bytes) |
| Upload: file stream cannot be read | *The file could not be read. Please try uploading it again.* | `ImageStorage.ValidateAsync` (`IOException`) |
| Prompt or Image Generator missing, too short or too long | The field messages from [7.3](#73-viewmodelsaiimageformviewmodelcs), in the browser and again on the server | DataAnnotations + jQuery Validation, `ModelState` |
| The file cannot be written (disk full, no permission, name clash) | Create: *Your image could not be saved. Please try again.* Edit: *The new image could not be saved. Please try again.* | `try/catch (IOException or UnauthorizedAccessException)` in `Create`/`Edit` |
| The database rejects a new record | *Your image could not be saved to the gallery. Please try again.* The saved file is removed | `catch (DbUpdateException)` in `Create` |
| Edit: the record was deleted by someone else while the admin was editing | *Page Not Found* (404). The new file is removed | `catch (DbUpdateConcurrencyException)` in `Edit` |
| Edit: a concurrency conflict while the record still exists (the code allows for it, although with no concurrency token configured it should not normally happen) | *This image was changed by someone else. Please reload and try again.* The new file is removed | `catch (DbUpdateConcurrencyException)` in `Edit` |
| Edit: the database rejects the update | *Your changes could not be saved. Please try again.* The new file is removed | `catch (DbUpdateException)` in `Edit` |
| Edit: the URL id and the form id differ (tampered form) | *Page Not Found* (404) | `if (id != model.Id) return NotFound();` |
| Delete: the image was already deleted (double click, two admins) | Back on the list with the toast *That image had already been deleted.* | `DeleteConfirmed` (`FindAsync` returns `null`) |
| Delete: the database rejects the delete | Back on the Delete page with a red toast: *The image could not be deleted. Please try again.* | `catch (DbUpdateException)` in `DeleteConfirmed` |
| A file cannot be deleted (locked by another program) | Nothing. The record is still deleted or updated, and a warning is logged | `ImageStorage.Delete` (never throws) |
| Like: image already liked during this login | Toast: *You have already liked this image. Log out and back in to like it again.* | `AIImagesController.Like` + `SessionLikeTracker` |
| Like: the image does not exist | *Page Not Found* (404) | `ExecuteUpdateAsync` returns 0 → `NotFound()` |
| The database cannot be reached while the home page loads | The home page still loads, and the gallery shows *The gallery is taking a short break. Please refresh the page in a moment.* | `UploadGalleryViewComponent` (`try/catch`) |
| The database cannot be prepared at start-up | The site still starts. Pages needing the database show the error page, and the log has a critical message | `DbInitializer.InitialiseAsync` (`try/catch`) |
| An old `app.db` from an earlier version of the project (unknown migrations) | Nothing visible in Development: the old file is renamed `app.db.bak-<time>` and a new database is created. In other environments a critical log message explains what to do | `DbInitializer.ReplaceOutdatedDevelopmentDatabaseAsync` |
| A seed account cannot be created (for example a weak password in `appsettings.json`) | Nothing visible. The site starts, and the log lists the reasons | `DbInitializer.EnsureAccountsAsync` |
| Login: wrong e-mail or password | *Invalid login attempt. Please check your email and password.* | `LoginModel.OnPostAsync` |
| Login: 5 wrong passwords in a row | *Locked out* page. Login is blocked for 5 minutes | Identity lockout options in `Program.cs` + `LoginModel` |
| Register: weak password, mismatched confirmation, e-mail already used | Identity's messages listed on the form | `RegisterModel.OnPostAsync`, `[Compare]` |
| Login or Register with an external `returnUrl` | Ignored. The user goes to `/` instead | `SafeReturnUrl` in `LoginModel`/`RegisterModel`. `AccessDeniedModel` only shows local URLs |
| The browser leaves the page during a database query | Nothing (the request is abandoned) | `OperationCanceledException` is not treated as an error in `UploadGalleryViewComponent` |

To check an error yourself, look at the terminal (or the VS Code **Debug Console**). For example, an unhandled exception is logged as `Unhandled exception on /the/path` together with its stack trace.

---

## 16. The test project

`AICreativeArts.Tests/` is an **xUnit** project with 87 tests that run against the real site. Run them from the solution folder:

```bash
dotnet test
```

(In VS Code: **Terminal → Run Task → test**, or the Testing panel of C# Dev Kit. See [05-vscode-guide.md](05-vscode-guide.md).)

Packages: `Microsoft.AspNetCore.Mvc.Testing` (hosts the site in memory), `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` and `coverlet.collector`. The project references `AICreativeArts.csproj`.

### Helpers (`AICreativeArts.Tests/Infrastructure/`)

| File | What it does |
|---|---|
| `SiteFactory.cs` | A `WebApplicationFactory<Program>` that starts the real site in memory in the `Testing` environment. It swaps the database for a throw-away SQLite file in the temp folder and the upload folder for `wwwroot/uploads-tests`, so tests **never touch your `app.db` or `wwwroot/uploads`**. Both are deleted afterwards. `CreateBrowser()` returns an `HttpClient` that keeps cookies like a browser but does not follow redirects, so tests can check them. `AddImageAsync(...)` inserts a gallery record directly |
| `BrowserExtensions.cs` | `LoginAsync`, `LogoutAsync`, `GetAntiforgeryTokenAsync` (reads the hidden token from a page, as a browser would), `PostFormAsync`, `GetLikeCountAsync`, and the seed account constants |
| `ProjectPaths.cs` | Finds the `AICreativeArts` folder from the test output folder, so tests can read `.cshtml` and `appsettings.json` files |

### Test classes

| Class | Tests | What it covers |
|---|---|---|
| `AccessControlTests` | 24 | Visitors can open Home, Contact, Artist Shield, AI Images, Login and Register. Visitors are sent to Login (with the right `ReturnUrl`) for Create, Details, Edit and Delete. Members can open Create but get Access Denied for Details, Edit and Delete. Admins can open every page. The buttons match the role (visitor: none, member: Like, admin: Like, Edit and Delete). Unknown pages and bad ids (`999`, `abc`, `2147483648`) give the friendly 404. Login ignores an external `returnUrl` |
| `AIImageWorkflowTests` | 7 | A member can like once per login, and once more after logging out and in. Visitors cannot like. A POST without an antiforgery token gets 400. The list is sorted by likes, highest first. An upload is saved with server-controlled values (posted `Like=999` and a year-2000 date are ignored) and redirects to `/#upload-ai-images`. A fake image gets the validation message. An admin can edit (redirect to `/AIImages#image-{id}`) and delete |
| `ImageStorageTests` | 12 | Extension and signature checks (a real PNG passes as `.png` or `.PNG`, but fails as `.jpg`, `.exe` or with no extension). A real JPEG header passes. Empty and oversized files are rejected. Safe file names strip paths and symbols. A save-then-delete round trip works. `Delete("../keep.txt")` refuses to touch a file outside the upload folder |
| `ImageSlotTests` | 13 | Every slot has a unique key, a description, alt text, a size and a relative path. Every home tile uses a known slot. `appsettings.json` has a credit entry (with all four fields) for every slot. The `<image-slot>` tag helper renders a placeholder, an `<img>` with a version, or nothing for an unknown key. The Home page shows a placeholder for every missing tile and background. The Contact page asks for a reference for every uncredited image and shows a configured credit. Missing files give a plain 404, not an HTML page. An outdated Development database is backed up (`.bak-*`) and recreated |
| `ComplianceTests` | 31 | One test per `.cshtml` file (29 at present) checks there is no `style="..."`, no `<style>`, no `<script>` without `src` and no inline `onclick=`-style handler, which is the brief's external-CSS/JS rule. The layout links `~/css/site.css` and `~/js/site.js` (and both files exist). The menu and the footer both link to Home, AI Images, Contact and Artist Shield |

If you add a view, `ComplianceTests` automatically tests it too, so the total number of tests goes up.

---

## 17. Common changes

| I want to... | Do this |
|---|---|
| Put my name and e-mail on the Contact page | Edit `Site:DeveloperName` and `Site:ContactEmail` in `appsettings.json`, then restart |
| Credit an image I added | Fill in `ImageCredits:<slot key>` (Creator, Source, Url, Licence). No restart is needed. See [04-images-content-and-references.md](04-images-content-and-references.md) |
| Add another administrator | Add `{ "Email": "...", "Password": "...", "Role": "Admin" }` to `SeedAccounts:Accounts` and restart. If the e-mail already has an account, it is given the role and its password is left unchanged |
| Start with an empty database | Stop the site, delete `AICreativeArts/app.db`, `app.db-shm` and `app.db-wal` (and the files in `wwwroot/uploads/` except `.gitkeep`, if you want an empty gallery), then start it. The database, the role and the seed accounts are recreated |
| Change the maximum upload size | Change `Uploads:MaxFileBytes`. Keep `UploadOptions.RequestLimitBytes` (in `Infrastructure/Options.cs`) above it, and update the "5 MB" wording in `UploadSizeGuardAttribute.cs`, `StatusCodeViewModel` (413) and the hints in `Views/AIImages/_ImageForm.cshtml` |
| Add a column to `AIImage` | Add the property to `Models/AIImage.cs` (and to `AIImageFormViewModel` plus the form if users should edit it), then from the `AICreativeArts` folder run `dotnet ef migrations add <Name>`. The next start applies it automatically (`MigrateAsync`), or run the VS Code task **ef: update database**. Installing the `dotnet-ef` tool is covered in [05-vscode-guide.md](05-vscode-guide.md) |
| See the SQL EF Core runs | Set `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` to `Information` |
| Move the code into a new ASP.NET Core Web App project | Follow [05-vscode-guide.md](05-vscode-guide.md). Keep the namespaces (`AICreativeArts.*`) or rename them everywhere, including `_ViewImports.cshtml` and `@addTagHelper *, AICreativeArts`, which must name the new assembly |

# AI Unmasked: Project Documentation

**AI Unmasked** is an ASP.NET Core 8 MVC website for *Web Design and Programming (7175), Assessment 3*
at the University of Canberra. Its theme is **the harmful side of AI in the creative arts**:

- AI trained on art without consent;
- creative jobs and income under threat;
- convincing fakes that deceive audiences;
- what artists can do to protect themselves.

This file is the starting point. It explains what every file in the project does and links to the
detailed guides.

| Part | What it covers |
|---|---|
| **This file** | Quick start, architecture, a map of every file, how the brief is met, finishing checklist, glossary |
| [docs/02-back-end.md](docs/02-back-end.md) | Every C# file: `Program.cs`, controllers, data, services, tag helpers, Identity pages, configuration, walkthroughs, exception handling, database schema, tests |
| [docs/03-front-end.md](docs/03-front-end.md) | Every Razor view and partial, `site.css` section by section, `site.js` function by function, responsive breakpoints, accessibility |
| [docs/04-images-content-and-references.md](docs/04-images-content-and-references.md) | How to add your own images, credit them, edit the text, add sources, and the final checklist before you submit |
| [docs/05-vscode-guide.md](docs/05-vscode-guide.md) | Running, debugging and testing in **VS Code**, and moving the code into a **new ASP.NET Core Web App (MVC)** project |
| [AICreativeArts/wwwroot/img/README.md](AICreativeArts/wwwroot/img/README.md) | Quick reference: which image file goes where |

---

## 1. Quick start

**VS Code** (full details in [docs/05-vscode-guide.md](docs/05-vscode-guide.md)):

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and the **C# Dev Kit** extension.
2. Use **File › Open Folder…** to open the repository folder (the one that contains `AICreativeArts.sln`).
3. Press **F5**. The site builds, starts and opens in your browser.

**Command line:**

```bash
cd AICreativeArts
dotnet run --launch-profile https
```

**Visual Studio 2022:** open `AICreativeArts.sln` and press **F5**.

On the first run the site creates its SQLite database (`AICreativeArts/app.db`), the **Admin** role and two
test accounts. You don't need to run any database commands.

| Role | Email | Password | Can do |
|---|---|---|---|
| Admin | `admin@asp.net` | `Admin@123` | Everything: upload, like, view details, edit, delete |
| Member | `member@asp.net` | `Member@123` | Browse, like (once per image per login), upload |
| Visitor | *(not logged in)* | – | Home, AI Images list (no buttons), Artist Shield, Contact, Register, Login |

The site ships **with no images**. Every place an image belongs shows a placeholder that describes the
image to add, so the layout is never broken. See
[docs/04-images-content-and-references.md](docs/04-images-content-and-references.md).

---

## 2. Architecture in one picture

```
Browser ──HTTP──▶ Kestrel web server
                    │
                    ▼  Program.cs: middleware pipeline, in order
   ExceptionHandler → StatusCodePages → HSTS (not in Development) → HTTPS redirect → security headers
   → static files (wwwroot) → routing → authentication → authorisation
                    │
        ┌───────────┴───────────────────────┐
        ▼                                   ▼
  MVC controllers                     Razor Pages (Identity area)
  HomeController                      Login, Register, AccessDenied
  AIImagesController                  (+ the rest of the built-in Identity UI)
        │
        │ uses services (dependency injection)
        ├── ApplicationDbContext (EF Core) ──▶ SQLite file app.db
        ├── ImageStorage   (checks and saves uploads in wwwroot/uploads)
        ├── LoginLikeTracker (one like per image per login, stored in the ImageLikes table)
        └── MediaLibrary   (has each image slot been filled? credits from appsettings.json)
        │
        ▼
  Razor views (Views/**) + _Layout.cshtml
  partials · view component (UploadGallery) · tag helpers (<image-slot>, nav-active)
        │
        ▼
  HTML + wwwroot/css/site.css + wwwroot/js/site.js (+ Bootstrap, Font Awesome, fonts)
```

---

## 3. What every file does

### Solution root

| File / folder | What it does |
|---|---|
| `AICreativeArts.sln` | The solution: groups the web project and the test project, so VS Code, Visual Studio and `dotnet build` can open both at once. |
| `README.md` | Short project overview and quick start. |
| `DOCUMENTATION.md` | This file. |
| `docs/` | The detailed documentation parts listed above. |
| `.vscode/launch.json` | **F5** configuration: builds, then runs the site with the debugger attached and opens the browser. |
| `.vscode/tasks.json` | Tasks for *Terminal › Run Task…*: build, watch (hot reload), test, trust the HTTPS certificate, update the database. |
| `.vscode/extensions.json` | Extensions VS Code recommends when you open the folder (C# Dev Kit, C#, .NET runtime, SQLite Viewer). |
| `.vscode/settings.json` | Tells C# Dev Kit which solution to load and hides `bin`/`obj`. |
| `.gitignore` | Keeps build output, the local database and uploaded files out of Git. |

### Web project: `AICreativeArts/`

| File / folder | What it does |
|---|---|
| `AICreativeArts.csproj` | Project file: targets .NET 8 and lists the NuGet packages (Identity, EF Core SQLite, EF tools). |
| `Program.cs` | Start-up: registers services (database, Identity with roles and the login-id claim, options, app services, MVC with global antiforgery and Identity-guard filters) and builds the middleware pipeline. Then it prepares the database and starts the site. |
| `appsettings.json` | Configuration: database connection string, your name and email (`Site`), image credits (`ImageCredits`), upload limits (`Uploads`), seed accounts (`SeedAccounts`) and logging. |
| `appsettings.Development.json` | Logging overrides used when running in Development. |
| `Properties/launchSettings.json` | Launch profiles (`http`, `https`, `IIS Express`) with URLs and the environment name. |
| **Authorization/** | |
| `Roles.cs` | The role name constant `Admin`. Members are logged-in users without a role. |
| `LoginSession.cs` | Gives every sign-in a new random *login id* claim in the sign-in cookie, which the like rule uses. |
| **Content/**: the site's words and image list, kept out of the views | |
| `SiteContent.cs` | Home page text: the hero, the three "case files" with their example tiles and links, and the list of text sources shown on the Contact page. |
| `FeatureSection.cs` | The `FeatureSection` (case block) and `CaseExample` (square tile) record types. |
| `ArtistShieldContent.cs` | The Artist Shield page data: quiz questions, protection checklist steps, timeline entries. |
| `ImageLibrary.cs` | **Every image slot on the site**: key, file path, where it is used, what image to add, recommended size, alt text. |
| **Controllers/** | |
| `HomeController.cs` | Home, Contact and Artist Shield pages, plus the friendly error page (`Error`) and status pages (`Status`: 404, 403, 400…). |
| `AIImagesController.cs` | The AI image gallery: Index (public), Create (members), Details/Edit/Delete (admin), Like (members, once per login), with validation and error handling. |
| **Data/** | |
| `ApplicationDbContext.cs` | The EF Core database context: the Identity tables, `AIImages` (column lengths, an index on `Like`) and `ImageLikes` (a unique login/image index). |
| `DbInitializer.cs` | Runs at start-up: backs up an outdated development database, applies migrations, then creates the Admin role and the seed accounts. |
| `Migrations/…_InitialCreate.cs` | The migration that creates every table (Identity, `AIImages`, `ImageLikes`). `…Designer.cs` and `ApplicationDbContextModelSnapshot.cs` are EF's records of the model. |
| **Filters/** | |
| `UploadSizeGuardAttribute.cs` | Stops uploads over 10 MB before anything reads them, and sends the user back to the form with a message. |
| `IdentityPageGuardFilter.cs` | Makes the built-in Identity pages fail politely: redirects instead of exceptions, and friendly status pages instead of plain-text errors. |
| **Infrastructure/** | |
| `Options.cs` | Strongly typed settings classes: `SiteOptions`, `UploadOptions`, `SeedAccountOptions`, `ImageCredit`, `ImageCreditOptions`. |
| `SiteUrlExtensions.cs` | URL helpers: `Url.UploadedImage(filename)` and `Url.ImageAnchor(id)` (e.g. `/AIImages#image-3`). |
| **Models/** | |
| `AIImage.cs` | The `AIImage` model from the brief (Id, Prompt, ImageGenerator, UploadDate, Filename, Like, canIncreaseLike), plus length limits. |
| `ImageLike.cs` | One like by one login for one image (the "one like per image per login" rule). |
| `ErrorViewModel.cs` | Data for the error page, and `StatusCodeViewModel` (the friendly title and message for each HTTP status code). |
| **Services/** | |
| `ImageStorage.cs` | Validates uploads (size, extension, real file signature), saves them with a safe unique name, and deletes them safely. |
| `LikeTracker.cs` | Records likes in the database against the current login id, increases the like count atomically, and reports which images this login has already liked. |
| `MediaLibrary.cs` | Checks whether each image slot's file exists, and reads each slot's credit from configuration. |
| **TagHelpers/** | |
| `ImageSlotTagHelper.cs` | `<image-slot key="…">` renders the image, or a placeholder describing it. `<background-slot-note key="…">` shows a note while a background image is missing. |
| `NavActiveTagHelper.cs` | `nav-active="Home/Index"` highlights the current menu link (`active` class and `aria-current`). |
| **ViewComponents/** | |
| `UploadGalleryViewComponent.cs` | The "Upload AI images" gallery tiles on the home page, sorted by likes (still renders if the database fails). |
| **ViewModels/** | |
| `AIImageFormViewModel.cs` | The Create/Edit form model. Only Prompt, ImageGenerator and the file are accepted from the browser. |
| **Views/**: Razor templates (see [docs/03-front-end.md](docs/03-front-end.md)) | |
| `_ViewImports.cshtml`, `_ViewStart.cshtml` | Shared `@using` lines and tag helper registration; the default layout. |
| `Shared/_Layout.cshtml` | The page shell: head, menu, footer, scripts. |
| `Shared/_LoginPartial.cshtml` | Register/Login links, or "Hello …!" and Logout. |
| `Shared/_SiteToasts.cshtml` | Pop-up status messages (uploaded, updated, deleted, errors). |
| `Shared/_UploadCallToAction.cshtml` | "Please login or register…" or "Click here to upload a new AI image". |
| `Shared/_ValidationScriptsPartial.cshtml` | jQuery validation scripts for forms. |
| `Shared/Error.cshtml` | The friendly "Something went wrong" page. |
| `Shared/Components/UploadGallery/Default.cshtml` | The gallery view component's view. |
| `Home/Index.cshtml` | Home page: hero, three case blocks, upload gallery. |
| `Home/_FeatureSection.cshtml`, `_FeatureCard.cshtml`, `_CaseTiles.cshtml` | One case block, its text card, and its two square tiles. |
| `Home/Contact.cshtml` | Contact & About, Copyright, **Image Credits** and Sources. |
| `Home/ArtistShield.cshtml` | The additional creative page: quiz, protection checklist, timeline. |
| `Home/Status.cshtml` | Friendly 404/403/400/405/413 pages. |
| `AIImages/Index.cshtml` | List of AI Images, with role-based buttons. |
| `AIImages/Create.cshtml`, `Edit.cshtml`, `_ImageForm.cshtml` | Upload and edit pages, sharing one form partial. |
| `AIImages/Details.cshtml`, `Delete.cshtml`, `_ImageDetails.cshtml` | Details and delete confirmation, sharing one details partial. |
| **Areas/Identity/Pages/**: the themed account pages | |
| `Account/Login.cshtml(.cs)`, `Register.cshtml(.cs)`, `AccessDenied.cshtml(.cs)` | Customised Identity pages. The rest (Logout, Manage account, etc.) come from the built-in Identity UI. |
| `_ViewStart.cshtml`, `_ViewImports.cshtml` | Make the Identity pages use the site layout and tag helpers. |
| **wwwroot/**: files sent straight to the browser | |
| `css/site.css` | **All** of the site's styles (brief requirement). |
| `js/site.js` | **All** of the site's JavaScript (brief requirement). |
| `img/` | **Your images go here** (see `img/README.md`). It is empty until you add them. |
| `uploads/` | Images uploaded through the Create/Edit pages. |
| `lib/` | Third-party files: Bootstrap 5, jQuery and validation, Font Awesome icons, self-hosted Google Fonts. |

### Test project: `AICreativeArts.Tests/`

| File | What it tests |
|---|---|
| `AccessControlTests.cs` | Visitor/member/admin access to every page, login redirects, Access Denied, friendly 404s, open-redirect protection. |
| `AIImageWorkflowTests.cs` | Likes (once per login), antiforgery, sorting, uploading (including over-posting and fake files), admin edit and delete. |
| `ImageStorageTests.cs` | File signature checks, size limits, safe file names, path-traversal protection. |
| `ImageSlotTests.cs` | Image slots: placeholders, credits, the Contact page, plain 404s for missing files, the outdated-database backup. |
| `IdentityAndLikeRulesTests.cs` | Built-in Identity pages never error; likes are counted once per login and per user, survive without any browser state but the sign-in cookie, and are removed with their image. |
| `ComplianceTests.cs` | Scans every view for inline styles, `<style>` blocks, internal scripts and inline event handlers, and checks that the menu and footer link to every main page. |
| `Infrastructure/SiteFactory.cs` | Starts the real site in memory with a temporary database for each test class. |
| `Infrastructure/BrowserExtensions.cs` | Helpers to log in, log out and post forms with antiforgery tokens, like a browser. |
| `Infrastructure/ProjectPaths.cs` | Finds the web project folder from the test output folder. |

---

## 4. How the brief is met

| Brief requirement | Where |
|---|---|
| ASP.NET Core MVC, C# | The whole `AICreativeArts` project |
| Full-width layout, no gaps | `body` is a flex column and `main` fills it (`site.css` §3); edge-to-edge sections |
| `Views/Home/Index` and `Contact` | `HomeController` + `Views/Home/` |
| `Views/AIImages/` Create, Details, Delete, Edit, Index with the `AIImage` model and Entity Framework | `AIImagesController`, `Views/AIImages/`, `Models/AIImage.cs`, `Data/` |
| `_Layout.cshtml` with head, menu, footer | `Views/Shared/_Layout.cshtml` |
| Register and Login pages | `Areas/Identity/Pages/Account/` |
| One additional creative page, linked from the menu and footer | **Artist Shield** (`Views/Home/ArtistShield.cshtml`) |
| Menu and footer links | `_Layout.cshtml` (checked by `ComplianceTests`) |
| Visitor / Member / Admin access | `[AllowAnonymous]`, `[Authorize]`, `[Authorize(Roles = Roles.Admin)]` on each action |
| Bootstrap 5, Flexbox, Grid, media queries | Bootstrap navbar, grid and forms; Flexbox and CSS Grid throughout `site.css`; media queries in `site.css` §18 |
| External `site.css`, no inline or internal CSS | `wwwroot/css/site.css` (checked by `ComplianceTests`) |
| External `site.js`, no internal JavaScript | `wwwroot/js/site.js` (checked by `ComplianceTests`) |
| Parallax scrolling | `background-attachment: fixed` (`site.css` §3) |
| Mouse over / mouse out (CSS + JS); JavaScript produces HTML5 elements | `site.js`: `initToolTiles` and `initUploadTiles` build the captions and images and handle `mouseover`/`mouseout` |
| Blocks appear only when fully in the viewport | `site.js` `initRevealOnView` (an `IntersectionObserver`) |
| Anchor hyperlinks | Gallery tiles link to `/AIImages#image-{id}`; Like returns to the liked block |
| List sorted by likes, one like per login | `AIImagesController.Index` and `Like`, `Services/LikeTracker.cs`, `Authorization/LoginSession.cs` |
| Upload Date and Like read-only | `readonly` inputs, and the server always sets both values |
| Images uploaded to `wwwroot/uploads/` | `Services/ImageStorage.cs` |
| Handle all exceptions | Friendly error and status pages, validation, guarded file and database operations (see [docs/02-back-end.md](docs/02-back-end.md)) |
| Media copyright-free, credited on the Contact page | Image slots + `ImageCredits` → Contact **Image Credits** card; text sources → **Sources** card |

---

## 5. Before you submit

1. Add your images to `AICreativeArts/wwwroot/img/` (the file names are in `wwwroot/img/README.md`).
2. Credit each one in `appsettings.json` › `ImageCredits`.
3. Set `Site:DeveloperName` and `Site:ContactEmail` in `appsettings.json`.
4. Open the Contact page and check that no **"Reference needed"** or **"Image not added yet"** remains.
5. Run the tests (`dotnet test`) and zip the project as described in [docs/05-vscode-guide.md](docs/05-vscode-guide.md).

The full checklist is in [docs/04-images-content-and-references.md](docs/04-images-content-and-references.md).

---

## 6. Glossary

| Term | Meaning in this project |
|---|---|
| **MVC** | Model–View–Controller: *models* hold data (`AIImage`), *controllers* handle requests (`AIImagesController`), *views* produce the HTML (`Views/AIImages/Index.cshtml`). |
| **Razor** | The `.cshtml` syntax that mixes HTML with C# (`@Model.Prompt`, `@if (…) { … }`). |
| **Tag helper** | A C# class that turns a custom HTML element or attribute into HTML on the server, e.g. `<image-slot>` or `asp-action="Edit"`. |
| **Partial view** | A reusable piece of a view (`_FeatureCard.cshtml`). |
| **View component** | A partial view with its own C# logic, which can query the database (`UploadGallery`). |
| **Entity Framework Core (EF Core)** | Maps C# classes to database tables, so the code works with objects rather than SQL. |
| **Migration** | A versioned C# script describing a database change. `InitialCreate` builds every table. |
| **Identity** | ASP.NET Core's built-in user accounts, passwords, roles and login cookies. |
| **Dependency injection** | Services registered in `Program.cs` are handed to controllers and views automatically. |
| **Image slot** | A named place for an image (`Content/ImageLibrary.cs`). It shows a placeholder until you add the file. |
| **Antiforgery token** | A hidden form value that proves a POST came from this site's own form. |

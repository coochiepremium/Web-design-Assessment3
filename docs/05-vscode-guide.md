# VS Code guide

How to run, debug and test this project in **Visual Studio Code**, and how to move the code into a new
**ASP.NET Core Web App (Model-View-Controller)** project of your own.

- [1. Install once](#1-install-once)
- [2. Option A: open this project in VS Code (recommended)](#2-option-a-open-this-project-in-vs-code-recommended)
- [3. Option B: move the code into your own new ASP.NET Core Web App](#3-option-b-move-the-code-into-your-own-new-aspnet-core-web-app)
- [4. Database commands](#4-database-commands)
- [5. Zipping for submission](#5-zipping-for-submission)
- [6. Troubleshooting](#6-troubleshooting)

---

## 1. Install once

1. **.NET 8 SDK**: <https://dotnet.microsoft.com/download/dotnet/8.0>. Check it with `dotnet --list-sdks`; you
   should see a line starting with `8.0`.
2. **VS Code**: <https://code.visualstudio.com/>.
3. The **C# Dev Kit** extension (publisher: Microsoft). It also installs the C# extension and the .NET runtime
   helper. When you open this folder, VS Code offers it automatically, because it is listed in
   `.vscode/extensions.json`.
4. *(Optional)* The **SQLite Viewer** extension, to look inside `app.db`.

---

## 2. Option A: open this project in VS Code (recommended)

1. Download or clone the repository, then use **File › Open Folder…** and choose the folder that contains
   `AICreativeArts.sln` (not the inner `AICreativeArts` folder).
2. Click **Yes, I trust the authors** if VS Code asks.
3. Wait a few seconds while C# Dev Kit loads the solution. The **Solution Explorer** appears in the Explorer
   side bar, and packages are restored automatically.
4. Press **F5** (*Run › Start Debugging*). VS Code builds the solution, starts the site and opens
   <https://localhost:7113> in your browser. If the browser warns about the certificate, see
   [Troubleshooting](#6-troubleshooting).
5. Log in with `admin@asp.net` / `Admin@123` (admin) or `member@asp.net` / `Member@123` (member).

To stop the site, press **Shift+F5** or click the red square on the debug toolbar.

### What the `.vscode` files do

| File | Purpose |
|---|---|
| `launch.json` | The **F5** configuration "AI Unmasked (launch website)". It runs the `build` task first, then starts `AICreativeArts/bin/Debug/net8.0/AICreativeArts.dll` in the `AICreativeArts` folder with `ASPNETCORE_ENVIRONMENT=Development` on `https://localhost:7113` and `http://localhost:5280`, and opens the browser when the site reports *"Now listening on"*. Breakpoints in `.cs` files work. |
| `tasks.json` | Tasks for **Terminal › Run Task…**: `build` (also **Ctrl+Shift+B**), `watch (run with hot reload)`, `test`, `trust HTTPS development certificate`, and `ef: update database`. |
| `extensions.json` | The extensions VS Code recommends for this folder. |
| `settings.json` | Tells C# Dev Kit to load `AICreativeArts.sln`, and hides `bin`/`obj` folders. |

### Other useful ways to run it

- **Hot reload while editing:** *Terminal › Run Task… › watch (run with hot reload)*, or in a terminal run
  `cd AICreativeArts` then `dotnet watch run`. Saved changes to `.cshtml`, `.css` and `.js` files appear in the
  browser without restarting.
- **Plain terminal:** `cd AICreativeArts` then `dotnet run --launch-profile https`.

### Running the tests

Open the **Testing** panel (the flask icon) and click *Run Tests*, or run `dotnet test` in the terminal at the
repository root. All 96 tests should pass.

### Where things are created

- The database `AICreativeArts/app.db` is created automatically on the first run, with the Admin role and the
  two accounts. You never need to run a migration by hand.
- Uploaded images go to `AICreativeArts/wwwroot/uploads/`.
- Your own site images go in `AICreativeArts/wwwroot/img/` (see
  [04-images-content-and-references.md](04-images-content-and-references.md)).

---

## 3. Option B: move the code into your own new ASP.NET Core Web App

Use this if you must hand in a project you created yourself (for example called `MyArtSite`). These steps were
tested with the .NET 8 SDK: the new project built and every page loaded.

### Step 1: create the new project

Open a terminal in VS Code (**Terminal › New Terminal**) in the folder where you keep your work, and run:

```bash
dotnet new mvc --auth Individual -o MyArtSite --framework net8.0
```

`--auth Individual` adds user accounts (Identity) and the database packages this site needs.

> You can also use C# Dev Kit: **Ctrl+Shift+P** › *.NET: New Project…* › *ASP.NET Core Web App
> (Model-View-Controller)*. If you do, add the packages from Step 4 afterwards.

Open the new `MyArtSite` folder in VS Code (**File › Open Folder…**).

### Step 2: delete the template's own files

Inside `MyArtSite/`, delete these (this site replaces them):

```
Controllers/   Models/   Views/   Data/   Areas/   wwwroot/
Program.cs   appsettings.json   appsettings.Development.json   app.db (if present)
```

Keep `MyArtSite.csproj` and the `Properties/` folder.

### Step 3: copy this project's files in

From this repository's `AICreativeArts/` folder, copy these into `MyArtSite/`:

```
Areas/  Authorization/  Content/  Controllers/  Data/  Filters/  Infrastructure/  Models/
Services/  TagHelpers/  ViewComponents/  ViewModels/  Views/  wwwroot/
Program.cs  appsettings.json  appsettings.Development.json
```

(Copy, don't move, so the original stays intact. Do not copy `bin/`, `obj/` or `app.db`.)

### Step 4: check the NuGet packages

Open `MyArtSite.csproj`. It must contain these packages; the `--auth Individual` template adds them already:

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.31" />
<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.31" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.31" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.31" />
```

If any is missing (for example you created the project without authentication), add it in the terminal:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.31
dotnet add package Microsoft.AspNetCore.Identity.UI --version 8.0.31
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.31
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.31
```

### Step 5: rename the namespace

The code uses the namespace and assembly name `AICreativeArts`. It must match your project name.

1. Press **Ctrl+Shift+H** (*Search: Replace in Files*).
2. **Find:** `AICreativeArts`  **Replace:** `MyArtSite` (your project name, with the same capitals).
3. Click the **…** to show more options and set **files to include** to `*.cs, *.cshtml`.
4. Click **Replace All**.

This updates every `namespace`, `using`, Razor `@using`, and the `@addTagHelper *, AICreativeArts` lines in the
`_ViewImports.cshtml` files. The last one matters: if it keeps the old name, the custom tag helpers
(`<image-slot>` and the menu highlighting) silently stop working.

### Step 6: run it

```bash
dotnet build
dotnet run --launch-profile https
```

The first run creates `app.db`, the Admin role and the two accounts, exactly as in Option A. To debug with
**F5**, copy this repository's `.vscode/` folder into the folder you opened. In `launch.json` and `tasks.json`,
change the paths (`AICreativeArts.sln`, `AICreativeArts/bin/Debug/net8.0/AICreativeArts.dll`, `AICreativeArts/…`)
to your project's names, or simply press F5 and choose **C#** › your project when VS Code asks.

> The copied migration in `Data/Migrations/` works as it is after the rename. If you prefer a fresh one,
> delete `Data/Migrations/` and run `dotnet ef migrations add InitialCreate -o Data/Migrations` (see section 4).

---

## 4. Database commands

You normally need none of these, because the site creates and updates its database on start-up.

| Task | Command (run in the `AICreativeArts` folder) |
|---|---|
| Install the EF tool (once) | `dotnet tool install --global dotnet-ef --version 8.*` |
| List migrations | `dotnet ef migrations list` |
| Add a migration after changing a model | `dotnet ef migrations add YourChangeName -o Data/Migrations` |
| Apply migrations by hand | `dotnet ef database update` |
| Start with an empty database | Stop the site, delete `app.db` (and `app.db-shm`, `app.db-wal`), then run the site again |

To look inside the database, install **SQLite Viewer** and click `AICreativeArts/app.db`.

---

## 5. Zipping for submission

1. Stop the site.
2. Delete the `bin/` and `obj/` folders (in `AICreativeArts/` and `AICreativeArts.Tests/`) and the hidden `.vs/`
   folder if it exists. They are rebuilt automatically.
3. Delete `AICreativeArts/app.db*` (it is recreated on first run) and any test images in
   `AICreativeArts/wwwroot/uploads/` (keep `.gitkeep`).
4. Keep your own images in `AICreativeArts/wwwroot/img/`.
5. Zip the folder that contains `AICreativeArts.sln`.

---

## 6. Troubleshooting

| Problem | Fix |
|---|---|
| Browser says the connection is not private / certificate error | Run `dotnet dev-certs https --trust` once (or the task *trust HTTPS development certificate*), then restart the browser. Or use <http://localhost:5280>. |
| "Address already in use" / port 7113 or 5280 busy | Another copy of the site is still running. Stop it (Shift+F5, or close the terminal running it), then try again. |
| `SQLite Error 1: no such table` | The database is from an older version. Stop the site and delete `app.db`; it is recreated. In Development, an outdated database is backed up and recreated automatically. |
| `dotnet ef` is not recognised | Install it: `dotnet tool install --global dotnet-ef --version 8.*`, then open a new terminal. |
| C# Dev Kit shows no Solution Explorer | Make sure you opened the folder that contains `AICreativeArts.sln`, then run **Ctrl+Shift+P** › *.NET: Open Solution*. |
| After a rename, the image placeholders or menu highlight disappear | An `@addTagHelper *, AICreativeArts` line still has the old name. Search for `AICreativeArts` again and replace it. |
| Changes to a page don't show | Save the file and refresh. With plain `dotnet run`, restart the site, or use `dotnet watch run` for hot reload. |
| "That upload is too large" | Images must be JPG, PNG, GIF or WEBP and under 5 MB. |

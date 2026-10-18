# AI Unmasked: the dark side of AI art

**Web Design and Programming (7175) – Assessment 3** · ASP.NET Core 8 MVC · Entity Framework Core (SQLite) · ASP.NET Core Identity

AI Unmasked is a responsive website about **the harm generative AI can do to the creative arts**. It covers
three "case files":

- **Art Without Consent** (training data and copyright)
- **Livelihoods at Risk** (creative jobs and income)
- **Fakes and Deception** (voice clones and AI entries in art prizes)

The site also has:

- a member-driven gallery of AI images, with role-based access for visitors, members and admins;
- **Artist Shield**, an interactive page with a quiz, a protection checklist and a timeline of key events.

All facts are sourced and credited on the Contact page.

## Run it

| Tool | How |
|---|---|
| **VS Code** | Open this folder, install the recommended **C# Dev Kit** extension, press **F5**. See [docs/05-vscode-guide.md](docs/05-vscode-guide.md). |
| **Visual Studio 2022** | Open `AICreativeArts.sln`, press **F5**. |
| **Terminal** | `cd AICreativeArts` then `dotnet run --launch-profile https` |

Requirements: the .NET 8 SDK. The database (`app.db`), the Admin role and the test accounts are created
automatically on first run.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@asp.net` | `Admin@123` |
| Member | `member@asp.net` | `Member@123` |

## Finish it with your own images

The site ships **without images**. Each image position is an *image slot* that shows a placeholder
describing the picture to add, until you save the file with the listed name in
`AICreativeArts/wwwroot/img/`. Credit every image in `appsettings.json` › `ImageCredits`; the Contact
page lists them all. Start with [AICreativeArts/wwwroot/img/README.md](AICreativeArts/wwwroot/img/README.md),
then follow the checklist in [docs/04-images-content-and-references.md](docs/04-images-content-and-references.md).

| Home (before images are added) | Case block placeholder and hover caption |
|---|---|
| ![Home hero](docs/screenshots/01-home-hero.jpg) | ![Case block](docs/screenshots/02-case-block-placeholders-hover.jpg) |
| **Contact: image credits** | **Artist Shield** |
| ![Contact page image credits](docs/screenshots/04-contact-image-credits.jpg) | ![Artist Shield](docs/screenshots/03-artist-shield.jpg) |

## Documentation

| Document | Contents |
|---|---|
| [DOCUMENTATION.md](DOCUMENTATION.md) | Start here: architecture, what every file does, how the brief is met, glossary |
| [docs/02-back-end.md](docs/02-back-end.md) | Controllers, data, services, Identity, configuration, exception handling, tests |
| [docs/03-front-end.md](docs/03-front-end.md) | Views, `site.css` section by section, `site.js` function by function, responsive design |
| [docs/04-images-content-and-references.md](docs/04-images-content-and-references.md) | Adding images, credits and sources; editing content; final checklist |
| [docs/05-vscode-guide.md](docs/05-vscode-guide.md) | VS Code setup, debugging, database and tests, moving into a new ASP.NET Core Web App project |

## Tests

```bash
dotnet test
```

There are 96 automated tests covering:

- role-based access;
- likes (once per login, stored in the database), uploads, edits and deletes;
- built-in Identity pages that never error;
- upload validation;
- image slots and credits;
- compliance with the brief's "no inline CSS/JS" rule.

# Images, content and references guide

This guide walks you through finishing **AI Unmasked** with your own images, credits and text. The site ships with **no images on purpose**. Every place an image belongs is a named *image slot*. Until you add a file, the slot shows a placeholder that describes the picture it needs, gives the exact file name to save it as, and lists the recommended size. To add an image, you save a file with that name in `AICreativeArts/wwwroot` and credit it in `appsettings.json`. The Contact page then lists it automatically. The guide also covers where to find images you are allowed to use, how to edit the researched text and its sources, how to add a new image slot in code and how to rename the site. Section 6 is a checklist of everything you must fill in before you submit.

> Unless a path starts with `AICreativeArts.Tests/`, `docs/` or `.vscode/`, paths in this guide are inside the web project folder `AICreativeArts/`. For example, `Content/ImageLibrary.cs` means `AICreativeArts/Content/ImageLibrary.cs`.

## Contents

1. [How the image-slot system works](#1-how-the-image-slot-system-works)
2. [Every image slot](#2-every-image-slot)
3. [Adding or replacing an image](#3-adding-or-replacing-an-image)
4. [Finding images you are allowed to use](#4-finding-images-you-are-allowed-to-use)
5. [Crediting your images in appsettings.json](#5-crediting-your-images-in-appsettingsjson)
6. [Final checklist before you submit](#6-final-checklist-before-you-submit)
7. [Editing the written content](#7-editing-the-written-content)
8. [Adding a brand-new image slot](#8-adding-a-brand-new-image-slot)
9. [Changing the site name or brand](#9-changing-the-site-name-or-brand)

Related documents: [DOCUMENTATION.md](../DOCUMENTATION.md) (overview), [02-back-end.md](02-back-end.md) (C# reference), [03-front-end.md](03-front-end.md) (views, CSS and JavaScript), [05-vscode-guide.md](05-vscode-guide.md) (running, testing and zipping in VS Code).

---

## 1. How the image-slot system works

### 1.1 The parts and what each one does

| File | What it does |
|---|---|
| `Content/ImageLibrary.cs` | The **master list** of image slots. `ImageLibrary.Slots` holds one `ImageSlot` record per image. `ImageLibrary.Find(key)` looks a slot up by its key and ignores upper and lower case. Four constants name the special slots so views cannot misspell them: `HeroBackground`, `SectionBackground`, `UploadPreview` and `Favicon`. |
| `Services/MediaLibrary.cs` | `IMediaLibrary` and its class `MediaLibrary` answer three questions. Is this slot's file saved yet? (`HasFile`) Who made it? (`CreditFor`) What is the status of every slot? (`All()`, used by the Contact page). `Program.cs` registers the service once for the whole site with `builder.Services.AddSingleton<IMediaLibrary, MediaLibrary>()`. |
| `TagHelpers/ImageSlotTagHelper.cs` | This file has two tag helpers. **`<image-slot key="…" />`** outputs an `<img>` when the file exists and a placeholder when it does not. **`<background-slot-note key="…" />`** shows a dashed note while a CSS background image is missing, and outputs nothing once you add it. |
| `Views/_ViewImports.cshtml` | `@addTagHelper *, AICreativeArts` makes both tag helpers available in every MVC view. `Areas/Identity/Pages/_ViewImports.cshtml` does the same for the Login and Register pages. |
| `Views/Shared/_Layout.cshtml` | Adds the classes `has-hero-image` and `has-section-image` to `<body>` once those background files exist. It also outputs the favicon `<link>`, but only once `favicon.ico` exists. |
| `wwwroot/css/site.css` | Holds the placeholder styles (section *7b. Image placeholders*) and the background rules (section *3. Base & backgrounds*, plus the `--hero-…`/`--section-…` variables in `:root`). |
| `Infrastructure/Options.cs` | `ImageCredit` holds four strings: `Creator`, `Source`, `Url` and `Licence`. `ImageCreditOptions` holds every credit, keyed by slot key (case-insensitive). Both are read from the `ImageCredits` section of `appsettings.json`. |
| `Views/Home/Contact.cshtml` | The **Image Credits** card. It shows one line per slot with its status and credit. |
| `wwwroot/img/README.md` | A short copy of the slot table, handy when you are browsing the image folders. The code does not use it. |
| `AICreativeArts.Tests/ImageSlotTests.cs` | Automated checks for slots and credits (see [8.6](#86-the-tests-that-check-slots-imageslottests) and [6.7](#67-a-note-about-the-automated-tests)). |

### 1.2 The `ImageSlot` record

Each entry in `ImageLibrary.Slots` is written like this:

```csharp
new("tile-laion", "img/tiles/laion-5b.jpg", ImageSlotKind.Image,
    "Home – case 01 'Art Without Consent', first square",                    // UsedOn
    "Represents the LAION-5B training dataset: a mosaic of many tiny artworks, or rows of data on a screen.", // Description
    "Square JPG, at least 600 × 600 px",                                      // Recommended
    "Mosaic representing billions of scraped images"),                        // Alt
```

| Field | Meaning | Where you see it |
|---|---|---|
| `Key` | The slot's unique name, for example `tile-laion` | `key="…"` in views, `SlotKey` in `SiteContent.cs`, the entry name under `ImageCredits` in `appsettings.json`, and the `data-image-slot` attribute on a placeholder |
| `File` | The path **relative to `wwwroot`**, with forward slashes and no leading `/` | On the placeholder, on the Contact page, and in the `src` of the finished `<img>` |
| `Kind` | `Image` (shown with `<img>`), `Background` (a CSS background) or `Icon` (the favicon) | Only a label for people reading the list. No code reads it, because each slot is wired up by where it is used. |
| `UsedOn` | Where the image appears on the site | The heading of that slot's line on the Contact page |
| `Description` | What kind of picture to use | The text in the middle of the placeholder or background note |
| `Recommended` | Format and size | The last line of the placeholder or background note |
| `Alt` | Alternative text for screen readers | The `alt` of the finished `<img>`, and the placeholder's `aria-label` ("Image placeholder: …") |

### 1.3 What happens when a page is requested

```text
View:  <image-slot key="tile-laion" class="tool-tile-image" />
          │
          ▼
ImageSlotTagHelper.Process()
          │  1. _media.Find("tile-laion")        → the ImageSlot record (unknown key → output nothing)
          │  2. _media.HasFile(slot)             → WebRootFileProvider.GetFileInfo("img/tiles/laion-5b.jpg").Exists
          ▼
   File on disk?
     ├─ yes → <img src="/img/tiles/laion-5b.jpg?v=…" alt="…" decoding="async" loading="lazy" class="tool-tile-image" />
     └─ no  → <span class="image-slot tool-tile-image" role="img" data-image-slot="tile-laion" …>Image needed …</span>
```

Step by step:

1. A view contains the tag. In `Views/Home/_CaseTiles.cshtml` the key comes from the content: `<image-slot key="@example.SlotKey" class="tool-tile-image" />`.
2. The tag helper calls `_media.Find(Key)`. If no slot has that key, it **outputs nothing at all** (there is no error), so a misspelt key makes the image quietly disappear.
3. It then calls `_media.HasFile(slot)`, which runs `_environment.WebRootFileProvider.GetFileInfo(slot.File).Exists`. This is a quick disk check that runs on **every request**. Nothing is cached, so **a new image appears on the next refresh without restarting the site**.
4. **If the file exists**, the tag becomes an `<img>`. Its `src` gets a version string (`?v=…`) from ASP.NET Core's `IFileVersionProvider`. That string is a fingerprint of the file's contents, so it changes when you replace the file and the browser fetches the new image. The tag helper also sets `alt` from the slot, `decoding="async"`, and `loading="lazy"` (unless you write `lazy="false"`), and copies your `class`. This is real output from the running site:

   ```html
   <img src="/img/tiles/laion-5b.jpg?v=5mOw7mvM4s9eZX_jI1IcKWCZnw3GeB6ptEVMXQA5Uc0"
        alt="Mosaic representing billions of scraped images" decoding="async" loading="lazy" class="tool-tile-image" />
   ```

5. **If the file is missing**, the tag becomes a styled placeholder with four parts: a yellow **Image needed** badge, the description, the path to save to, and the recommended size. All text is HTML-encoded.

   ```html
   <span role="img" aria-label="Image placeholder: Mosaic representing billions of scraped images"
         data-image-slot="tile-laion" class="image-slot tool-tile-image">
     <span class="image-slot-badge"><i class="fa-solid fa-image" aria-hidden="true"></i> Image needed</span>
     <span class="image-slot-text">Represents the LAION-5B training dataset: …</span>
     <code class="image-slot-path">wwwroot/img/tiles/laion-5b.jpg</code>
     <span class="image-slot-size">Square JPG, at least 600 × 600 px</span>
   </span>
   ```

   The placeholder is the same size as the finished image, so the layout and the mouse-over caption still work (screenshot below).

![A case block with one tile showing its hover caption and one tile showing its "Image needed" placeholder](screenshots/02-case-block-placeholders-hover.jpg)

If a browser asks for a missing image file directly (for example `/img/tiles/laion-5b.jpg`), `HomeController.Status` returns a plain **404 with no HTML page**, because the address has a file extension. That keeps the browser console tidy and is covered by a test.

### 1.4 Background slots and the body classes

Two slots are **CSS backgrounds** rather than `<img>` tags, because the brief's parallax effect needs `background-attachment: fixed`. The brief also forbids inline styles, so the image address has to live in `site.css`. That causes a problem: if `site.css` always pointed at `hero.jpg`, every visitor's browser would ask for a file that does not exist yet. The layout solves this by adding a class to `<body>` **only when the file exists**:

```cshtml
@* Views/Shared/_Layout.cshtml *@
var backgroundClasses = string.Join(' ', new[]
{
    Media.HasFile(ImageLibrary.HeroBackground) ? "has-hero-image" : null,
    Media.HasFile(ImageLibrary.SectionBackground) ? "has-section-image" : null,
}.Where(c => c is not null));
…
<body class="@(ViewData["BodyClass"] ?? "page-default") @backgroundClasses">
```

`site.css` only uses the image when that class is present:

```css
:root {
    --hero-image: none;          /* no image until the class below switches it on */
    --section-image: none;
    /* --hero-shade, --hero-fallback, --section-shade and --section-fallback are also defined here */
}

.has-hero-image {
    --hero-image: url('../img/backgrounds/hero.jpg');
}

.has-section-image {
    --section-image: url('../img/backgrounds/section.jpg');
}

.hero,
.page-contact .site-main {
    background-image: var(--hero-shade), var(--hero-image), var(--hero-fallback);
    background-attachment: fixed;   /* parallax: the image stays still while the page scrolls */
    background-position: center;
    background-size: cover;
}
```

Each background has three layers, and CSS draws the first one on top:

1. **Shade**: a semi-transparent dark gradient that keeps white text readable. Your image will look a little darker on the site than the original.
2. **Your image**: `none` until the file exists.
3. **Fallback**: a designed gradient, so the page still looks finished before you add the image.

Which pages show which background is decided by the page's `ViewData["BodyClass"]`:

| Page | `<body>` class | Hero background (`hero.jpg`) | Section background (`section.jpg`) |
|---|---|---|---|
| Home | `page-home` | Top banner (`.hero`) | Behind the three case files and the upload gallery (`.parallax-section`) |
| AI Images list | `page-list` | Top banner | The rest of the page (`.page-list .site-main`) |
| Artist Shield | `page-shield` | Top banner | Behind the quiz, checklist and timeline (`.parallax-section`) |
| Contact | `page-contact` | The **whole page** (`.page-contact .site-main`) | Not used |
| Create, Edit, Delete, Details | `page-form` | Not used | The whole page |
| Error, Status (for example 404) and Access denied | `page-message` | Not used | The whole page |
| Login, Register | `page-account` | Not used | Not used (plain dark gradient) |

> **Note.** In `ImageLibrary.cs` (and `wwwroot/img/README.md`), the `UsedOn` text for `section-background` lists Home, Create, Edit, Delete, Details and the error pages. The CSS rules above also show it behind the lower part of **Artist Shield** and the **AI Images list**, so choose an image that works on all of those pages.

While a background is missing, `<background-slot-note key="…" />` shows a dashed note under the heading. It appears on **Home** (one note for each background), **Contact** (hero) and **Artist Shield** (hero), and disappears once the file exists.

### 1.5 The favicon

The favicon is the small icon in the browser tab. It is the only slot saved directly in `wwwroot` rather than in `wwwroot/img/`. The layout links to it only once it exists:

```cshtml
@if (Media.HasFile(ImageLibrary.Favicon))
{
    <link rel="icon" href="~/favicon.ico" asp-append-version="true" />
}
```

A favicon has nowhere on the page to show a placeholder, so the only reminder is its line on the Contact page.

### 1.6 Not part of this system: uploaded gallery images

Images that members upload on the **Create** page are a different thing. Each one is saved in `wwwroot/uploads/` with a generated file name, and a row is added to the database (`app.db`). They are shown with a `data-image-fallback` attribute, so `site.js` replaces a missing upload with an "Image unavailable" box. Uploads are not image slots and are not listed under Image Credits. **Never save slot images in `wwwroot/uploads/`.** The gallery starts empty.

---

## 2. Every image slot

There are **10 slots**, all taken from `ImageLibrary.Slots`. Save each file **exactly** as shown: same folder, same name, lower case and the same extension.

```text
AICreativeArts/wwwroot/
├── favicon.ico                         ← favicon
└── img/
    ├── README.md
    ├── backgrounds/
    │   ├── hero.jpg                    ← hero-background
    │   └── section.jpg                 ← section-background
    ├── tiles/
    │   ├── laion-5b.jpg                ← tile-laion
    │   ├── getty-lawsuit.jpg           ← tile-getty
    │   ├── wga-strike.jpg              ← tile-wga
    │   ├── sag-aftra-strike.jpg        ← tile-sag
    │   ├── heart-on-my-sleeve.jpg      ← tile-heart
    │   └── theatre-dopera-spatial.jpg  ← tile-theatre
    └── ui/
        └── upload-preview.jpg          ← upload-preview
```

The empty `.gitkeep` files in those folders only make Git keep the empty folders. You can leave them.

### 2.1 Where each slot goes

| # | Key | Save as (inside `AICreativeArts/wwwroot/`) | Kind | Where it appears | Recommended size |
|---|---|---|---|---|---|
| 1 | `hero-background` | `img/backgrounds/hero.jpg` | Background | Top banner of Home, AI Images and Artist Shield, and the whole Contact page (fixed parallax background) | Landscape JPG, at least 1920 × 1080 px, under 600 KB |
| 2 | `section-background` | `img/backgrounds/section.jpg` | Background | Behind the three case blocks and upload gallery on Home, plus the Create, Edit, Delete, Details and error pages (and also Artist Shield and the AI Images list, see [1.4](#14-background-slots-and-the-body-classes)) | Landscape JPG, at least 1920 × 1280 px, under 600 KB |
| 3 | `tile-laion` | `img/tiles/laion-5b.jpg` | Image | Home: case 01 "Art Without Consent", first square | Square JPG, at least 600 × 600 px |
| 4 | `tile-getty` | `img/tiles/getty-lawsuit.jpg` | Image | Home: case 01 "Art Without Consent", second square | Square JPG, at least 600 × 600 px |
| 5 | `tile-wga` | `img/tiles/wga-strike.jpg` | Image | Home: case 02 "Livelihoods at Risk", first square | Square JPG, at least 600 × 600 px |
| 6 | `tile-sag` | `img/tiles/sag-aftra-strike.jpg` | Image | Home: case 02 "Livelihoods at Risk", second square | Square JPG, at least 600 × 600 px |
| 7 | `tile-heart` | `img/tiles/heart-on-my-sleeve.jpg` | Image | Home: case 03 "Fakes and Deception", first square | Square JPG, at least 600 × 600 px |
| 8 | `tile-theatre` | `img/tiles/theatre-dopera-spatial.jpg` | Image | Home: case 03 "Fakes and Deception", second square | Square JPG, at least 600 × 600 px |
| 9 | `upload-preview` | `img/ui/upload-preview.jpg` | Image | Create page: the default image shown before a file is chosen (the brief's "default image") | Square JPG, at least 600 × 600 px |
| 10 | `favicon` | `favicon.ico` (directly in `wwwroot/`) | Icon | Browser tab icon on every page | ICO file containing 16 × 16, 32 × 32 and 48 × 48 px |

### 2.2 What to use for each slot

The "What to use" column is the slot's `Description` from `ImageLibrary.cs`, which is the same text the placeholder shows. The "Search ideas" column suggests keywords to try on the free photo sites in [section 4](#4-finding-images-you-are-allowed-to-use).

| Key | What to use | Alt text the site gives it | Search ideas |
|---|---|---|---|
| `hero-background` | A dark, moody wide image about the harm AI can do to art, e.g. a cracked or glitched painting, an empty artist's studio, or a canvas fading into pixels. It must be dark enough for white text. | Dark background image | "empty art studio", "cracked paint", "torn canvas", "dark studio" |
| `section-background` | A subtle, dark texture that will not compete with text, e.g. a close-up of canvas or paper, a paint-stained desk in shadow, or blurred lights of a server room. | Dark textured background | "canvas texture", "black paper texture", "server room", "paint desk" |
| `tile-laion` | Represents the LAION-5B training dataset: a mosaic of many tiny artworks, or rows of data on a screen. | Mosaic representing billions of scraped images | "mosaic", "data on screen", "code screen", "wall of photos" |
| `tile-getty` | Represents the Getty Images v Stability AI lawsuit: a gavel, a courtroom, or a photo with a watermark. Do not use the Getty Images logo unless you have permission. | Courtroom gavel representing the Getty Images lawsuit | "gavel", "courtroom", "law books" |
| `tile-wga` | Represents the 2023 Hollywood writers' strike: picket signs, a typewriter, or a crowd of protesters. | Writers on a picket line | "typewriter", "picket sign", "protest signs" |
| `tile-sag` | Represents the 2023 actors' strike and digital replicas: a picket line, a theatre mask, or a face being scanned. | Actors' strike and digital face scanning | "theatre masks", "film set", "stage lights" |
| `tile-heart` | Represents AI voice cloning: a studio microphone, a sound wave, or headphones. Avoid photos of Drake or The Weeknd unless they are licensed for reuse. | Studio microphone representing a cloned voice | "studio microphone", "sound wave", "headphones" |
| `tile-theatre` | Represents the AI image that won an art prize: an award ribbon, a gallery wall, or judges at a contest. Do not use the original artwork unless you have permission. | Award ribbon at an art competition | "award ribbon", "art gallery wall", "art exhibition" |
| `upload-preview` | A neutral "your image here" picture, e.g. an empty frame on a wall or a blank canvas on an easel. | Empty frame waiting for an uploaded image | "empty frame", "blank canvas", "easel" |
| `favicon` | A small square icon for the site, e.g. a shield or a warning sign. Convert a PNG to .ico with any favicon generator. | Site icon (not shown, since an icon has no alt) | Make it from a free icon (see [5.2](#52-worked-example)) or draw your own |

> **Tip.** For protest or picket-line images, choose photos where nobody can be recognised, such as signs, crowds seen from behind or a blurred background. The reasons are in [4.4](#44-why-not-logos-real-people-or-the-actual-artworks).

---

## 3. Adding or replacing an image

### 3.1 Add an image, step by step

1. **Choose the slot.** Use the tables in section 2, or read the placeholder on the page, which prints the exact path to use (for example `wwwroot/img/tiles/wga-strike.jpg`).
2. **Prepare the file.** Crop it to the right shape and size and save it as JPG (or ICO for the favicon). See [3.3](#33-format-and-size-tips).
3. **Name it exactly.** Use lower case and `.jpg`, not `.jpeg`, `.JPG` or `.png`. On Windows, first turn on **File Explorer › View › Show › File name extensions**. Otherwise you can easily end up with `hero.jpg.jpg` without noticing.
4. **Put it in the right folder.** In VS Code you can drag the file from your desktop onto the folder in the Explorer panel, for example `AICreativeArts › wwwroot › img › tiles`. File Explorer or Finder works just as well. Put it in the project's `wwwroot`, **not** in `bin/…/wwwroot`.
5. **Refresh the page** (F5). You **do not need to restart or rebuild**, because `MediaLibrary` checks the file on every request (see [1.3](#13-what-happens-when-a-page-is-requested)). The placeholder turns into your image. For a background, the dashed note disappears and the image appears behind the content.
6. **Credit it** in `appsettings.json` ([section 5](#5-crediting-your-images-in-appsettingsjson)).
7. **Tick it off** in the [final checklist](#6-final-checklist-before-you-submit).

### 3.2 Replace or remove an image

- **Replace:** save the new file over the old one with the same name, then refresh.
  - Slots shown with `<img>` (the six tiles and `upload-preview`) get a new `?v=…` fingerprint, so the new image shows straight away.
  - The two **backgrounds** are referenced from `site.css` without a fingerprint, so your browser may keep showing the old one. Do a hard refresh: **Ctrl + F5** (Windows) or **Cmd + Shift + R** (macOS).
  - Browsers cache **favicons** very stubbornly. Do a hard refresh, or close the tab and open the site again.
- **Remove:** delete the file. The placeholder comes back on the next refresh, and the Contact page shows "Image not added yet" again.
- **Use a different name or format** (for example a `.png` or `.webp`): the simplest fix is to convert the image to JPG. If you would rather change the code, edit the slot's `File` in `Content/ImageLibrary.cs`. For the two backgrounds, also edit the `url(...)` in `.has-hero-image` or `.has-section-image` in `site.css`. Then update `wwwroot/img/README.md` to match and restart the site. Do not just rename a `.png` to `.jpg`; convert it properly.

### 3.3 Format and size tips

| Slot type | Shape | Minimum size | Suggested target | How the site displays it |
|---|---|---|---|---|
| Backgrounds (`hero`, `section`) | Landscape | 1920 × 1080 (hero), 1920 × 1280 (section) | JPG at about 70–80 % quality, **under 600 KB** | `background-size: cover`, centred, fixed while scrolling. It is scaled to fill the screen, so the edges are cropped, especially on tall phone screens. A dark shade is drawn on top. |
| Tiles (`tile-…`) | Square | 600 × 600 | 600–1000 px square JPG, roughly 100–250 KB | `object-fit: cover` crops it to a square from the centre. On mouse over it fades to 15 % opacity and greyscale behind the caption. |
| `upload-preview` | Square | 600 × 600 | 600–1000 px square JPG | `object-fit: cover` in a square frame on the Create page |
| `favicon` | Square icon | 16, 32 and 48 px in one `.ico` | Start from a 512 × 512 PNG and convert it | Browser tab |

More tips:

- **Crop the image yourself** so you choose what stays in view. Keep the important part in the centre of backgrounds.
- **Use dark images for the hero.** The page heading and text are white or yellow on top of it.
- **Shrink large camera photos.** A 6000 px, 8 MB photo slows the page down. To resize and compress you can use [Squoosh](https://squoosh.app/) in the browser, the Windows Photos app (*Resize image*), Paint (*Resize*), or macOS Preview (*Tools › Adjust Size*, then *File › Export* as JPEG).
- **Make the favicon** from a square PNG with a converter such as [favicon.io](https://favicon.io/) or [RealFaviconGenerator](https://realfavicongenerator.net/), and name the result `favicon.ico`.

### 3.4 Troubleshooting

| What you see | Likely cause | Fix |
|---|---|---|
| The placeholder is still there after a refresh | The name, extension or folder does not match exactly (`hero.JPG`, `hero.jpeg`, `hero.jpg.jpg`, saved in `img/` instead of `img/backgrounds/`, or saved under `bin/`) | Compare the file with the path printed on the placeholder. Turn on file name extensions in Windows. |
| It works on your computer but not on another one | Windows ignores upper and lower case, but macOS and Linux can be case-sensitive | Use lower-case names exactly as listed |
| No image **and** no placeholder | The `key` in the view does not match any slot, so the tag helper outputs nothing | Check the spelling against `ImageLibrary.cs` (see [1.3](#13-what-happens-when-a-page-is-requested), step 2) |
| The old background still shows | Browser cache | Hard refresh (Ctrl + F5 or Cmd + Shift + R) |
| A tile looks badly cropped | The image is not square | Crop it to a square yourself |
| The Contact page says "Reference needed" | `Creator` or `Source` is empty | See [section 5](#5-crediting-your-images-in-appsettingsjson) |
| There is no "View original" link | `Url` is empty or does not start with `https://` or `http://` | Paste the full address of the image's page |
| The site will not start after you edited `appsettings.json` | The JSON is invalid (a missing comma or quote) | See [5.4](#54-json-pitfalls) |

---

## 4. Finding images you are allowed to use

### 4.1 What the brief requires

The brief (requirements 9 to 12) says:

- all media must be your own choice;
- *"Media must be sourced from copyright-free resources or created by you"*;
- *"All sources must be credited in the Contact page"*;
- *"Do not use the sample media shown in the provided layout"* (so no Gemini, ChatGPT, Pixlr, DALL·E, IBM or Amazon images from the sample).

This site also argues that AI art harms artists, so **do not use AI-generated images** for any slot. That includes AI images offered on stock photo sites. The site was rebuilt without images so that you can add human-made ones.

### 4.2 Where to look

| Source | Licence to write in `Licence` | Does the licence require credit? | Watch out for |
|---|---|---|---|
| [Unsplash](https://unsplash.com/) | `Unsplash Licence` | No, but **the brief does**, so always credit | **Unsplash+** images are a paid subscription with a different licence, so skip them. The licence covers the photographer's copyright only. It does not cover logos, trademarks, or the rights of people shown in the photo. |
| [Pexels](https://www.pexels.com/) | `Pexels Licence` | No, but credit anyway | Recognisable people must not be shown in a bad or offensive light. Do not suggest that a person or brand endorses your site. |
| [Pixabay](https://pixabay.com/) | `Pixabay Content Licence` | No, but credit anyway | Pixabay also accepts AI-generated images, so check the image page and its tags and skip anything marked as AI-generated. It has the same rules about people and brands. |
| [Wikimedia Commons](https://commons.wikimedia.org/) | Whatever the file page says: `Public domain`, `CC0`, `CC BY 4.0`, `CC BY-SA 4.0` and so on | **Yes** for CC BY and CC BY-SA: the author, the licence and a link | Read the **Licensing** section of each file page. The **"Use this file"** button gives you ready-made credit text. With CC BY-SA, if you change the image (even by cropping), say so in the credit. Some images in English Wikipedia articles are "non-free" files used under fair use and are not on Commons, so do not reuse those. |
| [Openverse](https://openverse.org/) | Shown on each result | Depends on the licence | It is a search engine for openly licensed media. Always open the **original** page and check the licence there. |
| Your own photos or drawings | (`Source` is `Own work`) | Credit yourself | Do not include people who could be recognised unless they agree. Do not photograph other people's artwork, posters, album covers or logos. |

Licence terms can change. Read the licence page on the site you download from at the time you download.

### 4.3 Check before you download

- [ ] The licence allows use on a website, and allows changes such as cropping and resizing.
- [ ] It is not a premium, paid or "editorial use only" image.
- [ ] It is **not AI-generated** (check the description and tags).
- [ ] It shows no logos, brand names or watermarks.
- [ ] Nobody in it can be recognised. If someone can, the licence must allow it and your use must not show them in a bad light.
- [ ] It is not a copy of someone's artwork (a painting, illustration, poster, album cover or film still).
- [ ] You have copied the **creator's name, the image page's address and the licence name** into `appsettings.json` straight away ([section 5](#5-crediting-your-images-in-appsettingsjson)).

### 4.4 Why not logos, real people or the actual artworks

The case files name real companies, people and artworks, so it is tempting to show them. Use **symbolic** images instead, as every slot description suggests.

- **Logos** (Getty Images, Stability AI, Midjourney, Universal Music Group, the Writers Guild, SAG-AFTRA and so on) are trademarks owned by those organisations. Free photo licences do not cover them. On a page that criticises those organisations, a logo could also suggest they are involved in or endorse your site. That is why the `tile-getty` description says *"Do not use the Getty Images logo unless you have permission."*
- **Photos of real people** (Drake, The Weeknd, the striking writers and actors, the artists who went to court) are usually copyrighted press photos, often owned by photo agencies such as Getty Images itself. People also have privacy and personality rights. Putting a real face next to headings like "Fakes and Deception" could unfairly suggest that person did something wrong. That is why the `tile-heart` description says *"Avoid photos of Drake or The Weeknd unless they are licensed for reuse."*
- **The actual artworks.** *Théâtre D'opéra Spatial* is an AI-generated image, and this site does not use AI images. The artworks inside LAION-5B belong to the artists whose work was scraped, and showing them without permission would repeat the harm the site is about. That is why the `tile-theatre` description says *"Do not use the original artwork unless you have permission."*

---

## 5. Crediting your images in appsettings.json

### 5.1 Where credits live

`AICreativeArts/appsettings.json` has an `ImageCredits` section with **one entry for every slot key**. Each entry has **four fields**, and they all start empty:

```json
"ImageCredits": {
  "hero-background": {
    "Creator": "",
    "Source": "",
    "Url": "",
    "Licence": ""
  },
  "section-background": { … },
  …
  "favicon": { … }
}
```

| Field | What to write | Examples | How the site uses it |
|---|---|---|---|
| `Creator` | The person or organisation who made the image | `"Jane Doe"`, your own name | The first part of the credit line. **Required.** |
| `Source` | Where you got it | `"Unsplash"`, `"Pexels"`, `"Pixabay"`, `"Wikimedia Commons"`, `"Own work"` | The second part of the credit line. **Required.** |
| `Url` | The full address of the **image's page**, not the download link | `"https://unsplash.com/photos/…"` | Becomes a **View original** link, but only if it starts with `https://` or `http://` |
| `Licence` | The licence name | `"Unsplash Licence"`, `"CC BY-SA 4.0"`, `"Public domain"` | Shown in brackets after the source. Leave it empty to hide the brackets. |

The rules in the code (`Infrastructure/Options.cs`):

- `ImageCredit.IsComplete` is true when `Creator` **and** `Source` are both filled in. Only then does the Contact page show the credit instead of "Reference needed".
- `ImageCredit.HasLink` is true when `Url` is a full `http` or `https` address.
- Keys are matched without regard to upper and lower case, but keep them exactly as they are.

### 5.2 Worked example

Say you found a photo of a gavel on Unsplash for **`tile-getty`**:

1. Download the image, crop it square, and save it as `AICreativeArts/wwwroot/img/tiles/getty-lawsuit.jpg`.
2. On the photo's Unsplash page, note the **photographer's name** and copy the **page address** from the address bar.
3. Open `appsettings.json` and find the `tile-getty` entry.

**Before:**

```json
"tile-getty": {
  "Creator": "",
  "Source": "",
  "Url": "",
  "Licence": ""
},
```

**After** (use the real name and address of the photo you chose):

```json
"tile-getty": {
  "Creator": "Jane Doe",
  "Source": "Unsplash",
  "Url": "https://unsplash.com/photos/abc123",
  "Licence": "Unsplash Licence"
},
```

4. Save and refresh the Contact page. The `tile-getty` line now reads:

   > **Home – case 01 'Art Without Consent', second square**
   > Jane Doe, Unsplash (Unsplash Licence) *View original*

More examples:

```json
"hero-background": {
  "Creator": "A. Photographer (cropped)",
  "Source": "Wikimedia Commons",
  "Url": "https://commons.wikimedia.org/wiki/File:Example.jpg",
  "Licence": "CC BY-SA 4.0"
},
"section-background": {
  "Creator": "Your Name",
  "Source": "Own work",
  "Url": "",
  "Licence": ""
},
"favicon": {
  "Creator": "Font Awesome (Fonticons, Inc.)",
  "Source": "Font Awesome Free",
  "Url": "https://fontawesome.com/icons/shield-halved",
  "Licence": "CC BY 4.0"
}
```

- **Your own photo:** with `Url` and `Licence` left empty, the line reads "Your Name, Own work" with no link.
- **Favicon made from a free icon:** Font Awesome Free icons (already used for the footer) are licensed CC BY 4.0, so a favicon made from one, such as the shield, must credit it as shown. If you draw your own icon, credit yourself instead.
- **Wikimedia Commons:** use the author name exactly as the file page shows it, and say if you changed the image (for example "(cropped)").

### 5.3 What the Contact page shows

Each slot's line on the Contact page starts with its `UsedOn` text, followed by one or two status messages:

| File saved? | `Creator` and `Source` filled in? | The Contact page shows |
|---|---|---|
| No | No | **Image not added yet** – `wwwroot/<file>` **and** **Reference needed** – add it under `ImageCredits:<key>` in appsettings.json |
| No | Yes | **Image not added yet** – `wwwroot/<file>`, plus your credit line |
| Yes | No | **Reference needed** – add it under `ImageCredits:<key>` in appsettings.json |
| Yes | Yes | Just your credit line. **Done.** |

Lines whose file is missing are also highlighted (the CSS class `credit-item--missing`). Before you add anything, every line shows both messages:

![The Contact page's Image Credits card, with every slot showing "Image not added yet" and "Reference needed"](screenshots/04-contact-image-credits.jpg)

**Credits update on refresh, with no restart.** `MediaLibrary` reads them through `IOptionsMonitor<ImageCreditOptions>`, which picks up changes to `appsettings.json` while the site runs. The `Site` details are different (see the checklist): `HomeController` reads them once with `IOptions<SiteOptions>`, so you must **restart the site** to see a new name or email.

### 5.4 JSON pitfalls

- Put every value in double quotes: `"Creator": "Jane Doe"`.
- Separate the fields and entries with commas, with **no comma after the last one** in a block. The site itself would accept a trailing comma, but the test `Appsettings_has_a_credit_entry_for_every_slot` reads the file with a strict JSON parser and fails on one.
- Do **not** add `//` comments to `appsettings.json`, for the same reason.
- Do not delete or rename an entry or a field. The same test checks that every slot has all four fields, and empty values are fine.
- Inside a value, write a double quote as `\"` and a backslash as `\\`.
- VS Code underlines JSON mistakes in red and lists them in the **Problems** panel (Ctrl + Shift + M). Check it after editing.
- If the JSON is invalid, the site **will not start**, and the error message names `appsettings.json`.

---

## 6. Final checklist before you submit

Work through this list from top to bottom. To hunt for anything left over, use VS Code's **Search across files** (Ctrl + Shift + F) to search for `[Your Name]`, `example.com`, `Image needed` and `Reference needed`.

### 6.1 Your details (`appsettings.json` › `Site`)

- [ ] `Site:DeveloperName`: replace `"[Your Name]"` with your name. It appears on the Contact page as "This website is developed by …".
- [ ] `Site:ContactEmail`: replace `"your.email@example.com"` with your email address. It is the `mailto:` link on the Contact page.
- [ ] **Restart the site** (Shift + F5, then F5) and check both on the Contact page.

`Site:Name` (`"AI Unmasked"`) is in the file too, but no page displays it. See [section 9](#9-changing-the-site-name-or-brand) if you want to rename the site.

### 6.2 Image files (inside `AICreativeArts/wwwroot/`)

- [ ] `img/backgrounds/hero.jpg` (`hero-background`)
- [ ] `img/backgrounds/section.jpg` (`section-background`)
- [ ] `img/tiles/laion-5b.jpg` (`tile-laion`)
- [ ] `img/tiles/getty-lawsuit.jpg` (`tile-getty`)
- [ ] `img/tiles/wga-strike.jpg` (`tile-wga`)
- [ ] `img/tiles/sag-aftra-strike.jpg` (`tile-sag`)
- [ ] `img/tiles/heart-on-my-sleeve.jpg` (`tile-heart`)
- [ ] `img/tiles/theatre-dopera-spatial.jpg` (`tile-theatre`)
- [ ] `img/ui/upload-preview.jpg` (`upload-preview`)
- [ ] `favicon.ico` (`favicon`)

### 6.3 Image credits (`appsettings.json` › `ImageCredits`)

Fill in `Creator`, `Source`, `Url` and `Licence` for each entry (leave `Url` and `Licence` empty for your own work if you like):

- [ ] `hero-background`
- [ ] `section-background`
- [ ] `tile-laion`
- [ ] `tile-getty`
- [ ] `tile-wga`
- [ ] `tile-sag`
- [ ] `tile-heart`
- [ ] `tile-theatre`
- [ ] `upload-preview`
- [ ] `favicon`

### 6.4 Check it in the browser

- [ ] **Contact page:** no line says "Image not added yet" or "Reference needed", and every **View original** link opens the right page.
- [ ] **Home:** no "Image needed" placeholders on the six tiles, and no "Background image needed" notes.
- [ ] **Artist Shield** and **Contact:** no "Background image needed" note.
- [ ] **Create page** (log in as `member@asp.net`): your default image shows instead of the placeholder.
- [ ] The browser tab shows your favicon.
- [ ] Everything looks right at phone, tablet and desktop widths, in **both Chrome and Edge** (brief requirement 8).

### 6.5 Content and sources

- [ ] Any text you changed or added is in your own words and has its source in `SiteContent.TextSources` ([7.5](#75-the-source-rule-every-fact-needs-a-source-in-sitecontenttextsources)).
- [ ] All external links work: the **Read more** links, the tiles, the quiz, checklist and timeline **Source** links, and the Sources list on the Contact page.
- [ ] Optional: the five footer icons link to the home pages of Google Arts & Culture, YouTube, X, Facebook and LinkedIn. Change the `href`s in `Views/Shared/_Layout.cshtml` if you want them to point somewhere more specific.

### 6.6 Leave these as they are

These look like placeholders but are meant to be there:

| What | Where | Why it stays |
|---|---|---|
| `name@example.com` and `password` hints in the email and password boxes | `Areas/Identity/Pages/Account/Login.cshtml`, `Register.cshtml` | They are HTML `placeholder` hints inside empty form boxes, not content |
| "Describe the prompt you gave the AI image generator" and "e.g. Midjourney" | `Views/AIImages/_ImageForm.cshtml` | The same kind of hint on the Create and Edit form |
| `admin@asp.net` / `Admin@123` and `member@asp.net` / `Member@123` | `appsettings.json` › `SeedAccounts` | The marker uses these to test the Admin and Member roles |
| `uploads` and `5242880` (5 MB) | `appsettings.json` › `Uploads` | The upload folder and size limit for gallery images |

### 6.7 A note about the automated tests

Run the tests **before** you add images: `dotnet test`, or the VS Code **test** task. They should all pass (96 tests at the time of writing).

Some tests in `AICreativeArts.Tests/ImageSlotTests.cs` run the real site against the real `wwwroot` and `appsettings.json`. They check the "no images yet" state, so **once you add images and credits, they fail even though the site works correctly**. This was confirmed by running the tests on a copy of the project with images and credits added:

| Test | Fails once… |
|---|---|
| `Home_page_shows_a_placeholder_for_each_missing_tile_and_background` | any tile image, `hero.jpg` or `section.jpg` exists |
| `Contact_page_asks_for_a_reference_for_every_uncredited_image` | any `ImageCredits` entry has `Creator` and `Source` filled in |
| `Contact_page_shows_a_configured_credit` | any entry other than `tile-laion` is credited |
| `Missing_files_get_a_plain_404_not_an_html_page`, the `/img/backgrounds/hero.jpg` case | `hero.jpg` exists |
| `Missing_files_get_a_plain_404_not_an_html_page`, the `/favicon.ico` case | `favicon.ico` exists |

With every image and credit added, expect **91 passed and 5 failed** (out of 96), and the 5 failures should be exactly the tests listed above. Any **other** failing test means something really is wrong. To run only the image tests, use:

```bash
dotnet test --filter "FullyQualifiedName~ImageSlotTests"
```

### 6.8 Gallery uploads and zipping

- [ ] The AI Images gallery starts empty. Whether you upload example AI images before submitting is up to you. If you do, upload images you generated yourself and name the tool honestly in **Image Generator**.
- [ ] Gallery records (and their likes) live in `app.db`, and their files live in `wwwroot/uploads/`, so keep or remove them **together**. Delete any test uploads before you zip the project, as the comment in `.gitignore` also says. To submit an empty gallery, delete `app.db` (plus `app.db-shm` and `app.db-wal` if present) and the files in `wwwroot/uploads/`, but keep `.gitkeep`. The database, the Admin role and the seed accounts are recreated automatically the next time the site starts.
- [ ] Zip the project as described in [05-vscode-guide.md](05-vscode-guide.md).

---

## 7. Editing the written content

### 7.1 Where each piece of text lives

The researched text is kept in two C# files, so the views stay short and the Contact page can list every source automatically.

| Text | File | Shown on |
|---|---|---|
| Hero eyebrow, title, paragraph and source link | `Content/SiteContent.cs` (`HeroEyebrow`, `HeroTitle`, `HeroText`, `HeroSource`, `HeroSourceUrl`) | Home, top banner |
| The three case files and their six tiles | `Content/SiteContent.cs` (`Sections`) | Home |
| The list of text sources | `Content/SiteContent.cs` (`TextSources`) | Contact page, **Sources** card |
| Quiz questions | `Content/ArtistShieldContent.cs` (`Quiz`) | Artist Shield, *Test your knowledge* |
| Protection checklist | `Content/ArtistShieldContent.cs` (`Steps`) | Artist Shield, *Protect your art* |
| Timeline | `Content/ArtistShieldContent.cs` (`Timeline`) | Artist Shield, *How the Harm Unfolded* |
| The shapes of these records | `Content/FeatureSection.cs` (`FeatureSection`, `CaseExample`) and the top of `ArtistShieldContent.cs` (`QuizQuestion`, `ShieldStep`, `TimelineEntry`) | Not shown on the site |

### 7.2 Hero text (`SiteContent.cs`)

```csharp
public const string HeroEyebrow = "Content warning: the hidden cost of generative AI";
public const string HeroTitle = "The Dark Side of AI Art";
public const string HeroText = "Generative AI can produce a painting, a song or a story in seconds, … ";
public const string HeroSource = "Adapted from Wikipedia";
public const string HeroSourceUrl = "https://en.wikipedia.org/wiki/AI_art";
```

- Keep `HeroText` about the same length as the brief's sample paragraph (around 50–60 words).
- `HeroSourceUrl` is also the first entry in `TextSources`, so if you change it, the Contact page follows automatically.
- The browser-tab title of the Home page is set separately, by `ViewData["Title"] = "The Dark Side of AI Art";` at the top of `Views/Home/Index.cshtml`. Change it there too if you change `HeroTitle`.

### 7.3 The three case files and their tiles (`SiteContent.Sections`)

Each case file is a `FeatureSection`:

| Field | What it is | Shown as |
|---|---|---|
| `Id` | A unique lower-case-with-hyphens name, for example `"art-without-consent"` | The block's HTML `id`, so you can link to it with `#art-without-consent`. The Home page's "See the evidence" link jumps to the **first** section's `Id`. |
| `CaseLabel` | For example `"Case 01 · Consent & copyright"` | The small label above the heading |
| `Title` | For example `"Art Without Consent"` | The block's `<h3>` heading |
| `Description` | A paragraph of about 30–45 words | The block text, followed by a **Read more** link |
| `KeyFact` | One striking, sourced fact | The highlighted line with the "!" icon |
| `ReadMoreUrl` | The external page for **Read more** | Opens in a new tab |
| `TextFirst` | `true` puts the text card on the left and the tiles on the right; `false` swaps them | Sections alternate `true`, `false`, `true` like the brief's layout |
| `Examples` | The two tiles (`CaseExample`) | The two squares |

Each tile is a `CaseExample`:

| Field | What it is | Shown as |
|---|---|---|
| `Name` | For example `"LAION-5B"` | The bold title of the mouse-over caption, and part of the tile's screen-reader label |
| `SlotKey` | The image slot for the tile, for example `"tile-laion"` | Must match a key in `ImageLibrary.Slots` (a test checks this) |
| `HoverText` | About 20–35 words | The mouse-over caption text |
| `Source` | For example `"Wikipedia"` | The italic credit under the caption (like the brief's `<br>Wikipedia`) |
| `Url` | The tile's link | Opens in a new tab when the tile is clicked |

Example: to change the first tile's caption, edit only the third string:

```csharp
new("LAION-5B", "tile-laion",
    "LAION-5B is an open dataset of about 5.85 billion image–text pairs scraped from the web, " +
    "released in 2022 and used to train image generators such as Stable Diffusion.",
    "LAION", "https://laion.ai/blog/laion-5b/"),
```

Keep **three** case files with **two** tiles each, because that is the layout the brief asks for.

### 7.4 Artist Shield content (`ArtistShieldContent.cs`)

**Quiz:** each `QuizQuestion(Id, Question, Options, CorrectIndex, Explanation, SourceUrl)`:

| Field | Notes |
|---|---|
| `Id` | Unique, for example `"q-laion"`. It is used as the name of the question's radio-button group, so two questions must never share an `Id`. |
| `Question` | The question text |
| `Options` | The answers, written as `new[] { "First", "Second", "Third" }` |
| `CorrectIndex` | **Counts from 0**: `0` is the first option, `1` the second, `2` the third |
| `Explanation` | Shown after **Check my answers**, with a **Source** link |
| `SourceUrl` | Where the fact comes from (also add it to `TextSources`) |

A new question goes inside the `Quiz` list, after the last `),`:

```csharp
new("q-your-id", "Your question?",
    new[] { "Answer A", "Answer B", "Answer C" }, 1,   // 1 = "Answer B" is correct
    "One or two sentences explaining the answer, in your own words.",
    "https://the-page-you-got-the-fact-from"),
```

The page counts the questions automatically ("6 questions about…").

**Checklist:** each `ShieldStep(Id, Title, Detail, LinkText, LinkUrl)`:

- `Id` (for example `"step-glaze"`) is saved in the visitor's browser (`localStorage`, under the key `aiunmasked.shieldSteps`) to remember which steps they have ticked. Keep it unique. If you rename it, any tick saved under the old `Id` is simply forgotten.
- Set `LinkText` and `LinkUrl` to `null` for a step with no link. The link only appears when `LinkUrl` is not `null`.
- The progress bar ("0 of 8 steps done") counts the steps automatically.

**Timeline:** each `TimelineEntry(Date, Title, Text, SourceUrl)`:

- `Date` is plain text, shown exactly as you write it (for example `"May – September 2023"`).
- Entries appear **in the order they are written**, so keep them in date order.

### 7.5 The source rule: every fact needs a source in `SiteContent.TextSources`

The Contact page's **Sources** card says the case files, quiz, checklist and timeline *"were researched and written in our own words using these sources"*, and it lists exactly what is in `SiteContent.TextSources`. The `ReadMoreUrl`, tile `Url` and quiz or timeline `SourceUrl` links appear next to the text, but they are **not** added to the Contact page automatically. So whenever you add or change a fact:

1. Write it **in your own words**, unless it is an adapted quote that you label, like the hero's "Adapted from Wikipedia".
2. Put its link in the right field (`ReadMoreUrl`, `Url` or `SourceUrl`).
3. **Add the same source to `TextSources`**, unless it is already there:

   ```csharp
   public static IReadOnlyList<(string Title, string Url)> TextSources { get; } = new List<(string, string)>
   {
       ("Wikipedia – AI art", HeroSourceUrl),
       …
       ("US Copyright Office – Copyright and Artificial Intelligence", "https://www.copyright.gov/ai/"),
       ("Publisher – Page title", "https://the-page-you-used"),   // ← your new source
   };
   ```

4. If you remove the last fact that used a source, you may remove that source from the list too.

A quick check: search `SiteContent.cs` and `ArtistShieldContent.cs` for `https://` and make sure every address also appears in `TextSources`. At the time of writing, every link in both files is listed.

### 7.6 Tips for editing C# text and seeing your changes

- Text goes between double quotes. To show a double quote inside, write `\"`, or use single quotes as the existing text does ('Heart on My Sleeve').
- Long strings are joined with `+`. Keep a space at the end of each piece: `"…the cost. " + "These systems…"`.
- Each item in a list ends with a comma, and the list ends with `};`.
- **Do not type HTML** such as `<br>` or `<em>`. Razor HTML-encodes the text, so the tags would appear on the page as text.
- Characters such as en dashes, accented letters and × are fine, because the files are saved as UTF-8.
- For web addresses with special characters, copy the address from the browser's address bar (for example `Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial`).
- `.cs` and `.cshtml` changes are **compiled**, unlike image files and `appsettings.json` credits. Stop the site (Shift + F5) and start it again (F5). The **watch (run with hot reload)** task often applies changes automatically, but the lists are built once when the site starts, so restart if a change does not show. Press **Ctrl + Shift + B** (the default **build** task) to catch typing mistakes, which appear in the **Problems** panel.

### 7.7 Text written directly in the views

Some short text lives in the Razor views rather than the content classes:

| Text | File |
|---|---|
| "See the evidence", the "Upload AI images" heading and its introduction | `Views/Home/Index.cshtml` |
| "Click here to upload a new AI image" and "Please login or register to upload your AI images." | `Views/Shared/_UploadCallToAction.cshtml` |
| Artist Shield eyebrow ("Know the risks · Protect your work"), banner paragraph and panel headings | `Views/Home/ArtistShield.cshtml` |
| **About Us** and **Copyright** paragraphs | `Views/Home/Contact.cshtml` |
| "The gallery is empty" message | `Views/AIImages/Index.cshtml` |
| Menu and footer link labels, footer note, default meta description | `Views/Shared/_Layout.cshtml` |
| Each page's tab title and description | `ViewData["Title"]` and `ViewData["Description"]` at the top of each view |

---

## 8. Adding a brand-new image slot

The example below adds a hypothetical illustration to the Artist Shield page with the key `shield-illustration`. **It does not exist yet**; replace the names with your own.

### 8.1 Step 1: add the slot to `ImageLibrary.Slots`

In `Content/ImageLibrary.cs`, optionally add a constant (this protects you from typos), then add a record to the list:

```csharp
public const string ShieldIllustration = "shield-illustration";   // optional, next to the other constants

// inside Slots, before the closing "};"
new(ShieldIllustration, "img/ui/shield-illustration.jpg", ImageSlotKind.Image,
    "Artist Shield – illustration beside the checklist",              // UsedOn: the Contact page heading
    "An artist's hands sketching on paper, or a padlock on a sketchbook.", // Description: placeholder text
    "Landscape JPG, at least 1200 × 800 px",                           // Recommended
    "An artist sketching on paper"),                                  // Alt
```

Rules (checked by `ImageSlotTests`):

- `Key` must be **unique** (ignoring case). Use lower case with hyphens.
- `File` is relative to `wwwroot`: use forward slashes and **do not start with `/`**.
- `Description`, `Recommended` and `Alt` must not be empty.

### 8.2 Step 2: use it in a view

```cshtml
<image-slot key="@ImageLibrary.ShieldIllustration" class="shield-illustration" />
@* or, without a constant: <image-slot key="shield-illustration" class="shield-illustration" /> *@
```

| Attribute | Required? | What it does |
|---|---|---|
| `key` | Yes | The slot key. An unknown key outputs nothing. |
| `class` | No | Extra CSS classes, added to the `<img>` and to the placeholder (as `image-slot shield-illustration`), so one CSS rule sizes both |
| `lazy` | No (default `true`) | `lazy="false"` loads the image straight away instead of when it scrolls into view. The Create page's preview uses this. |

Write it as a self-closing tag (`/>`). Add any sizing rules to `wwwroot/css/site.css`, never in a `style` attribute (brief requirement 6). For example:

```css
.shield-illustration {
    width: 100%;
    aspect-ratio: 3 / 2;
    object-fit: cover;
}
```

`ImageLibrary` is available in every MVC view because `Views/_ViewImports.cshtml` has `@using AICreativeArts.Content`. In the Identity pages (Login, Register), use the key as plain text instead.

### 8.3 Step 3: add an `ImageCredits` entry

In `appsettings.json`, add an entry with **all four fields**, and put a comma after the previous entry:

```json
"favicon": {
  "Creator": "",
  "Source": "",
  "Url": "",
  "Licence": ""
},
"shield-illustration": {
  "Creator": "",
  "Source": "",
  "Url": "",
  "Licence": ""
}
```

The Contact page lists the new slot automatically (`Media.All()` loops over every slot), with "Image not added yet" and "Reference needed" until you add the file and its credit.

### 8.4 Step 4: keep the documentation in step

Add a row to the table in `wwwroot/img/README.md` and to [section 2](#2-every-image-slot) of this guide.

### 8.5 Variations

**A new home-page tile.** Create the slot as above, then use its key as the `SlotKey` of a `CaseExample` in `SiteContent.Sections`. `_CaseTiles.cshtml` already outputs `<image-slot key="@example.SlotKey" … />`, so you do not need to touch the view.

**A new background image.** Backgrounds need the body-class pattern from [1.4](#14-background-slots-and-the-body-classes), so that the CSS never asks for a missing file:

1. Add a constant and a record with `ImageSlotKind.Background`, for example `ShieldBackground = "shield-background"` with `File` set to `"img/backgrounds/shield.jpg"`.
2. In `Views/Shared/_Layout.cshtml`, add a line to the `backgroundClasses` array:

   ```cshtml
   Media.HasFile(ImageLibrary.ShieldBackground) ? "has-shield-image" : null,
   ```

3. In `wwwroot/css/site.css`, add `--shield-image: none;` inside `:root`, then switch it on with the class and use it on the element that should show it:

   ```css
   .has-shield-image {
       --shield-image: url('../img/backgrounds/shield.jpg');   /* relative to wwwroot/css/ */
   }

   .page-shield .shield-section {
       background-image: var(--section-shade), var(--shield-image), var(--section-fallback);
   }
   ```

4. Optionally, add `<background-slot-note key="@ImageLibrary.ShieldBackground" />` to the view so that a note shows while the file is missing.
5. Add the `ImageCredits` entry (step 3).

### 8.6 The tests that check slots (`ImageSlotTests`)

| Test | What it checks | What it means when you add a slot |
|---|---|---|
| `Every_slot_is_described_and_unique` | Keys are unique (ignoring case); `Description`, `Alt` and `Recommended` are not empty; `File` does not start with `/` | Fill in every field and keep the path relative |
| `Every_case_tile_uses_a_known_slot` | Every `CaseExample.SlotKey` in `SiteContent.Sections` exists in `ImageLibrary` | Add the slot **before** you use its key in a tile |
| `Appsettings_has_a_credit_entry_for_every_slot` | `appsettings.json` has an `ImageCredits` entry for every slot, each with `Creator`, `Source`, `Url` and `Licence` | Do step 3, or this test fails |
| `Contact_page_asks_for_a_reference_for_every_uncredited_image` | "Reference needed" appears once per slot, and every `ImageCredits:<key>` is mentioned | Passes automatically while the new entry is empty (see [6.7](#67-a-note-about-the-automated-tests)) |
| `Tag_helper_renders_a_placeholder_when_the_file_is_missing`, `Tag_helper_renders_an_image_when_the_file_exists`, `Tag_helper_renders_nothing_for_an_unknown_key` | How `<image-slot>` behaves, using the `tile-laion` slot | Not affected |

Run them with `dotnet test --filter "FullyQualifiedName~ImageSlotTests"`.

---

## 9. Changing the site name or brand

The name **AI Unmasked** is typed directly into a few files. `Site:Name` in `appsettings.json` (default `"AI Unmasked"` in `SiteOptions`) exists, but **no page reads it**, so changing it alone changes nothing you can see. To rename the site, edit these places. Use **Search across files** (Ctrl + Shift + F) for `AI Unmasked` to find them all.

| Where | What to change |
|---|---|
| `Views/Shared/_Layout.cshtml` | `<title>@ViewData["Title"] - AI Unmasked</title>` (every browser tab) |
| `Views/Shared/_Layout.cshtml` | The menu brand: `<span class="brand-text" data-text="AI Unmasked">AI Unmasked</span>`. Change both copies. |
| `Views/Shared/_Layout.cshtml` | The default `<meta name="description" …>` text, which starts "AI Unmasked examines…" |
| `Views/Shared/_Layout.cshtml` | The footer note: `AI Unmasked &middot; A student project about…` |
| `Views/Home/Contact.cshtml` | `ViewData["Description"]` ("…information about the AI Unmasked website…") |
| `Areas/Identity/Pages/Account/Register.cshtml.cs` | The welcome message "Welcome to AI Unmasked! …" |
| `appsettings.json` and `Infrastructure/Options.cs` | `Site:Name` and the `SiteOptions.Name` default, to keep them consistent |
| Comments and docs | The first comment line of `wwwroot/css/site.css` and `wwwroot/js/site.js`, the title of `wwwroot/img/README.md`, `README.md`, `DOCUMENTATION.md`, these guides, and the configuration name in `.vscode/launch.json` ("AI Unmasked (launch website)") |

Other parts of the brand:

| Element | Where |
|---|---|
| Menu icon (the warning triangle) | `<i class="fa-solid fa-triangle-exclamation brand-icon">` in `_Layout.cshtml`. Swap in any Font Awesome *solid* icon name. Its colour comes from `.brand-icon` in `site.css`. |
| Colours and fonts | The variables at the top of `wwwroot/css/site.css` (`:root`), for example `--hazard`, `--crimson` and `--font-display` |
| Mobile browser bar colour | `<meta name="theme-color" content="#0b0b0d" />` in `_Layout.cshtml` |
| Home headline and eyebrow | `HeroTitle` and `HeroEyebrow` in `Content/SiteContent.cs` (see [7.2](#72-hero-text-sitecontentcs)) |
| Favicon | Replace `wwwroot/favicon.ico` ([3.2](#32-replace-or-remove-an-image)) |

**Leave these internal names alone.** Visitors never see them, and changing them has side effects:

| Name | Where | What would happen if you changed it |
|---|---|---|
| `.AIUnmasked.Auth` | Sign-in cookie name in `Program.cs` | Everyone currently logged in would be logged out, and a test in `AICreativeArts.Tests/IdentityAndLikeRulesTests.cs` that looks for this cookie name would fail |
| `aiunmasked:login-id` | `LoginSession.ClaimType` in `Authorization/LoginSession.cs` (the login id that likes are recorded against) | Anyone already logged in would be asked to log in again before they could like an image |
| `aiunmasked.shieldSteps` | `localStorage` key in `wwwroot/js/site.js` | Visitors would lose their ticked checklist steps |
| `AICreativeArts` | Project, namespace and solution name | Renaming means changing every `namespace`, `@using`, the `.csproj`, the `.sln` and the test project, which is not worth the risk |

No test checks the visible site name, so renaming it in the places listed above does not break any tests. Rebuild and restart afterwards, because the changes are in `.cshtml` and `.cs` files.

# Images for AI Unmasked

The site ships **without images** so you can add your own. Every image belongs to a
named *slot* defined in `Content/ImageLibrary.cs`. Until a file exists, the site shows a
styled placeholder that describes the image needed, so the layout never breaks.

Save each file with **exactly** the name below (lower case, same extension). The image
appears on the next page refresh, with no code changes or restart needed.

| Slot key | Save as (inside `wwwroot/`) | Where it appears | What to use | Size |
|---|---|---|---|---|
| `hero-background` | `img/backgrounds/hero.jpg` | Top banner of Home, AI Images and Artist Shield, and the whole Contact page (fixed parallax) | A dark, moody wide image about the harm AI can do to art: a cracked or glitched painting, an empty studio, a canvas fading into pixels. It must be dark enough for white text. | Landscape JPG, at least 1920 × 1080, under 600 KB |
| `section-background` | `img/backgrounds/section.jpg` | Behind the Home case blocks and gallery, plus the Create, Edit, Delete, Details and error pages | A subtle, dark texture: canvas or paper close-up, a paint-stained desk in shadow, the blurred lights of a server room | Landscape JPG, at least 1920 × 1280, under 600 KB |
| `tile-laion` | `img/tiles/laion-5b.jpg` | Home, case 01, first square | The LAION-5B dataset: a mosaic of tiny artworks, rows of data on a screen | Square JPG, at least 600 × 600 |
| `tile-getty` | `img/tiles/getty-lawsuit.jpg` | Home, case 01, second square | The Getty Images lawsuit: a gavel, a courtroom, a watermarked photo (no Getty logo unless licensed) | Square JPG, at least 600 × 600 |
| `tile-wga` | `img/tiles/wga-strike.jpg` | Home, case 02, first square | Writers' strike: picket signs, a typewriter, protesters | Square JPG, at least 600 × 600 |
| `tile-sag` | `img/tiles/sag-aftra-strike.jpg` | Home, case 02, second square | Actors' strike or digital replicas: a picket line, a theatre mask, a face being scanned | Square JPG, at least 600 × 600 |
| `tile-heart` | `img/tiles/heart-on-my-sleeve.jpg` | Home, case 03, first square | AI voice cloning: a microphone, a sound wave, headphones (no photos of the artists unless licensed) | Square JPG, at least 600 × 600 |
| `tile-theatre` | `img/tiles/theatre-dopera-spatial.jpg` | Home, case 03, second square | The AI art prize: an award ribbon, a gallery wall, judges (not the original artwork unless permitted) | Square JPG, at least 600 × 600 |
| `upload-preview` | `img/ui/upload-preview.jpg` | Create page, the default image shown before a file is chosen | An empty frame on a wall, or a blank canvas on an easel | Square JPG, at least 600 × 600 |
| `favicon` | `favicon.ico` (directly in `wwwroot/`) | Browser tab icon | A small shield or warning icon, converted to .ico | 16, 32 and 48 px in one .ico |

## Credit every image (required by the brief)

For each image you add, fill in its entry in the `ImageCredits` section of
`appsettings.json`, using the slot key:

```json
"tile-laion": {
  "Creator": "Jane Doe",
  "Source": "Unsplash",
  "Url": "https://unsplash.com/photos/...",
  "Licence": "Unsplash Licence"
}
```

The Contact page lists every slot. It shows **"Reference needed"** until `Creator` and
`Source` are filled in, and **"Image not added yet"** until the file exists.

Use only images that you created yourself or that are free to reuse (for example
Unsplash, Pexels, Pixabay, or Wikimedia Commons with a CC licence or public domain).

# Screenshots

Drop UI screenshots here to fill the showcase rows on the homepage.

## Filename convention

`{showcase-id}.png` — must match the `id` in `website/content/showcase.ts`.

Currently expected:

| File                        | Showcase row          | Chrome    | Recommended size |
| --------------------------- | --------------------- | --------- | ---------------- |
| `lessenreeksen.png`         | Lessenreeksen         | dashboard | 1600 × 1000      |
| `anonieme-inschrijving.png` | Anonieme inschrijving | phone     | 720 × 1520       |
| `planningsalgoritme.png`    | Planningsalgoritme    | dashboard | 1600 × 1000      |
| `formulierbouwer.png`       | Formulierbouwer       | dashboard | 1600 × 1000      |

## How to update

1. Take the screenshot at the recommended size (or higher — `next/image` will handle it).
2. Save it to this folder with the exact filename above.
3. Open `website/content/showcase.ts` and set `image.src` to e.g. `/screenshots/lessenreeksen.png`.
4. If the actual image dimensions differ from the table above, update `image.width` and `image.height` to match — that drives the aspect ratio of the frame, so getting it wrong will distort the layout.

When `image.src` is empty the frame renders a labeled placeholder with the expected filename, so you can ship the layout before the assets land.

## Tips

- For dashboard shots, capture at 2× scale on a Retina display and save as PNG. `next/image` will downscale and convert as needed.
- For phone shots, crop to a clean device viewport — the frame already adds a phone bezel, so leave none of your own.
- Avoid baking in dark mode or system chrome — the frames provide their own.

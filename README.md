# Overview
[![NuGet](https://img.shields.io/nuget/v/StbTrueTypeNext.svg)](https://www.nuget.org/packages/StbTrueTypeNext/) 
[![Build & Publish Beta](https://github.com/StbSharp/StbTrueTypeNext/actions/workflows/build-and-publish-beta.yml/badge.svg)](https://github.com/StbSharp/StbTrueTypeNext/actions/workflows/build-and-publish-beta.yml)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbTrueTypeNext is a safe fork of [StbTrueTypeSharp](https://github.com/StbSharp/StbTrueTypeSharp), the C# port of
Sean Barrett's [stb_truetype.h](https://github.com/nothings/stb).

In other words, StbTrueTypeNext is a pure managed C# font rasterizer for TrueType and CFF/OpenType fonts.

## Install

https://www.nuget.org/packages/StbTrueTypeNext/

## What the library does

- Parses `.ttf` (TrueType, `glyf`/`loca`), `.otf` (CFF/OpenType, `CFF `) and `.ttc` collection files from a byte array.
- Maps Unicode codepoints to glyph indices via the `cmap` table (formats 0, 4, 6, 12 and 13).
- Rasterizes glyphs into 8-bit antialiased coverage bitmaps, with subpixel positioning through horizontal and vertical shifts.
- Returns a newly allocated bitmap, or renders directly into a caller-supplied buffer at an arbitrary offset and row stride, so glyphs can be baked straight into an atlas.
- Supports oversampling prefiltering when downscaling, and reports the subpixel offsets it introduces.
- Provides two rasterizer implementations, selectable per font (`STBRasterizerVersion.V1` and `V2`, the default).
- Computes glyph bounding boxes in font units or in pixels relative to the glyph origin.
- Extracts glyph outlines as move/line/quadratic/cubic vertices, handling composite TrueType glyphs and CFF charstrings with subroutines.
- Reports metrics: font bounding box, `hhea` ascent/descent/line gap, `OS/2` typographic metrics, per-glyph advance width and left side bearing, plus helpers for em-to-pixel and pixel-height scaling.
- Applies kerning from the `GPOS` table (pair and class-based pair positioning) with a fallback to the legacy `kern` table, and can enumerate every `kern` pair.
- Generates signed distance field bitmaps with configurable padding, on-edge value and pixel distance scale.
- Returns embedded SVG documents from the `SVG ` table when a font provides them.
- Reads font name strings (family name, style, and so on) by platform, encoding, language and name ID.
- Reads `ttcf` font collections: reports the font count, resolves a font's offset by index, and finds a font by name.

## Quick start

```csharp
using System;
using System.IO;
using StbTrueTypeNext;

var font = new STBFont(File.ReadAllBytes("DroidSans.ttf"));
var scale = font.ScaleForPixelHeight(32.0f);
var glyph = font.FindGlyphIndex('A');

// Let the library allocate the bitmap.
var bitmap = font.CreateGlyphBitmap(glyph, scale, scale);
if (bitmap != null)
{
    // Rows are bitmap.Width bytes wide here and the bitmap starts at bitmap.BitmapOffset.
    Console.WriteLine($"{bitmap.Width}x{bitmap.Height} at ({bitmap.XOffset}, {bitmap.YOffset}), first coverage {bitmap.Bitmap[bitmap.BitmapOffset]}");
}

// Or render straight into your own atlas at a byte offset and row stride.
const int atlasWidth = 512;
var atlas = new byte[atlasWidth * atlasWidth];
font.DrawGlyphOnBitmap(glyph, atlas, 0, atlasWidth, scale, scale);

// Advances, bearings and kerning.
font.GetGlyphHMetrics(glyph, out var advanceWidth, out var leftSideBearing);
var kerned = font.GetGlyphKernAdvance(glyph, font.FindGlyphIndex('V'));
```

## Samples

- `Samples/StbTrueTypeNext.Samples.OutputString` rasterizes a string into a grayscale PNG file.
- `Samples/StbTrueTypeNext.Samples.MonoGameTest` bakes glyphs into a MonoGame texture atlas and draws sample text
  with the resulting `SpriteFont`.

## License

StbTrueTypeNext is available under the Unlicense or the MIT license, at your option.

## Sponsor
If this project is useful for you, you can support the development:
- Boosty: https://boosty.to/rds1983
- Telegram Wallet: https://t.me/rds1983

### Crypto
| Blockchain | Supported Tokens | Wallet Address |
| :--- | :--- | :--- |
| **TON Network** | GRAM (native), USDT (TON) | UQCQy6tFInPvqinE44zHY4R0rYS3niaBikkqiSyGmyoAMwyO |
| **BSC** | BNB (native), USDT (BEP20) | 0x4205d5B12e4edc2D30E97A398A627c83872084E9 |
| **Solana** | SOL (native), USDT (SPL) | GjHtV9mP4eJNmi8p5bwsHtb5WV2zFu3v5VhiZ1bvddGV |
| **TRON** | TRX (native), USDT (TRC-20) | TM2SbB9eRQ99MP6d85a3yJSCWhqeV46crh |
| **Bitcoin** | BTC (native) | bc1qygcxzyhz0m34m9anj27rdydjekkv5ymxa9f927 |

# Credits
* [stb](https://github.com/nothings/stb)

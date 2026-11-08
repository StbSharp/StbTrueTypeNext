# Overview
[![NuGet](https://img.shields.io/nuget/v/StbTrueTypeNext.svg)](https://www.nuget.org/packages/StbTrueTypeNext/) 
[![Build & Publish Beta](https://github.com/StbSharp/StbTrueTypeNext/actions/workflows/build-and-publish-beta.yml/badge.svg)](https://github.com/StbSharp/StbTrueTypeNext/actions/workflows/build-and-publish-beta.yml)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbTrueTypeNext is a safe fork of [StbTrueTypeSharp](https://github.com/StbSharp/StbTrueTypeSharp), the C# port of
Sean Barrett's [stb_truetype.h](https://github.com/nothings/stb).

In other words, StbTrueTypeNext is a pure managed C# font rasterizer for TrueType and CFF/OpenType fonts.

## Install

```
dotnet add package StbTrueTypeNext
```

The library targets `netstandard2.0`, `net471` and `net8.0`; the `net8.0` build is AOT compatible.

## What the library does

- **Loads fonts from bytes.** `new STBFont(byte[])` takes the contents of a `.ttf` or `.otf` font file; the
  `(data, offset)` overload opens a single font inside a collection.
- **Rasterizes glyphs** into 8-bit alpha bitmaps at any scale and subpixel shift. `GetGlyphBitmapSubpixel` allocates
  the bitmap for you, while `MakeGlyphBitmapSubpixel` renders into your own buffer at a chosen byte offset and row
  stride, with optional horizontal and vertical oversampling prefiltering. Both return an `STBGlyphBitmap` that
  describes the buffer, the glyph's pixel offset from the origin and the subpixel offsets introduced by prefiltering.
- **Reports metrics**: vertical metrics (`GetFontVMetrics`, `GetFontVMetricsOS2`, `GetScaledFontVMetrics`), the font
  bounding box, per-glyph advance and left side bearing (`GetGlyphHMetrics`), and a glyph's bounding box in pixels or
  font units (`GetGlyphBoxInPixels`, `GetGlyphBoxInFontUnits`, both returning `STBGlyphBitmapBox`).
- **Applies kerning** from the modern `GPOS` table or the legacy `kern` table (`GetGlyphKernAdvance`,
  `GetKerningTable`).
- **Generates signed distance fields** for scalable, shader-friendly glyph rendering (`GetGlyphSDF`).
- **Reads embedded SVG documents** from the OpenType `SVG ` table (`GetGlyphSVG`).
- **Opens font collections**: `STBFontCollection` reports how many fonts a `.ttc` file holds
  (`GetNumberOfFonts`), resolves a font's offset by index (`GetFontOffsetForIndex`) and finds a font by family name
  (`FindMatchingFont`).
- **Exposes outlines and glyph lookup**: `GetGlyphShape` returns a glyph's outline as `STBVertex` data,
  `FindGlyphIndex` maps a Unicode codepoint to a glyph index, `IsGlyphEmpty` checks for empty glyphs, and
  `RasterizerVersion` selects the V1 or V2 rasterizer (V2 is the default).

The API is addressed by glyph index, so codepoint-based code maps its characters with `FindGlyphIndex` first.

It does **not** shape text: no ligatures, no contextual or mark positioning, no bidirectional reordering.

## Quick start

```csharp
using System;
using System.IO;
using StbTrueTypeNext;

var font = new STBFont(File.ReadAllBytes("DroidSans.ttf"));
var scale = font.ScaleForPixelHeight(32.0f);
var glyph = font.FindGlyphIndex('A');

// Let the library allocate the bitmap.
var bitmap = font.GetGlyphBitmapSubpixel(glyph, scale, scale);
if (bitmap != null)
{
    var g = bitmap.Value;
    // Rows are g.Width bytes wide here and the bitmap starts at g.BitmapOffset.
    Console.WriteLine($"{g.Width}x{g.Height} at ({g.XOffset}, {g.YOffset}), first coverage {g.Bitmap[g.BitmapOffset]}");
}

// Or render straight into your own atlas at a byte offset and row stride.
const int atlasWidth = 512;
var atlas = new byte[atlasWidth * atlasWidth];
font.MakeGlyphBitmapSubpixel(glyph, atlas, 0, atlasWidth, scale, scale);

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

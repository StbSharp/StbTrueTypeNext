# Overview
[![NuGet](https://img.shields.io/nuget/v/StbTrueTypeNext.svg)](https://www.nuget.org/packages/StbTrueTypeNext/) 
[![Build & Publish Beta](https://github.com/StbSharp/StbTrueTypeNext/actions/workflows/build-and-publish-beta.yml/badge.svg)](https://github.com/StbSharp/StbTrueTypeNext/actions/workflows/build-and-publish-beta.yml)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbTrueTypeNext is a safe fork of [StbTrueTypeSharp](https://github.com/StbSharp/StbTrueTypeSharp), the C# port of
Sean Barrett's [stb_truetype.h](https://github.com/nothings/stb).
In other words, StbTrueTypeNext is a pure managed C# font rasterizer for TrueType and CFF/OpenType fonts.

## What the library does

- **Loads fonts from bytes.** `STBFont` takes a `byte[]` holding a `.ttf`, `.otf` or `.ttc` file and
  locates the tables it needs. The data is read in place, not copied.
- **Rasterizes glyphs** into 8-bit alpha bitmaps at any scale, either one glyph at a time
  (`GetGlyphBitmap`, `GetCodepointBitmap`) or into your own buffer at a chosen offset
  (`MakeGlyphBitmap`, `MakeGlyphBitmapSubpixel`).
- **Reports metrics**: vertical metrics (ascent, descent, line gap), font bounding box, per-glyph
  advance and left side bearing, and the pixel bounding box of any rasterized glyph.
- **Applies kerning** from the modern `GPOS` table or the legacy `kern` table - see
  [Kernings](kernings.md).
- **Packs glyph atlases**: `Packer` rasterizes many characters, at one or more sizes, into a single
  single-channel bitmap and describes each glyph's rectangle, offsets and advance - see
  [Packer](packer.md).
- **Generates signed distance fields** for scalable, shader-friendly glyph rendering - see
  [SDF](sdf.md).
- **Reads embedded SVG documents** from the OpenType `SVG ` table (`GetCodepointSVG`,
  `GetGlyphSVG`).
- **Opens font collections**: `STBFontCollection` reports the number of fonts in a `.ttc` file,
  their offsets, and can pick a member by family name.

It does **not** shape text: no ligatures, no contextual or mark positioning, no bidirectional
reordering.

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

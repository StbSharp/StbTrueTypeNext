using StbImageWriteSharp;
using System;
using System.IO;
using static StbTrueTypeNext.StbTrueTypeFont;

namespace StbTrueTypeNext.Samples;

class Program
{
	static void Main(string[] args)
	{
		if (args.Length < 1 || string.IsNullOrEmpty(args[0].Trim()))
		{
			Console.WriteLine("Usage: OutputString.exe <string>");
			return;
		}

		var s = args[0];
		var fontSize = 32.0f;
		var bufferSize = 256;

		var bytes = File.ReadAllBytes("Fonts/DroidSans.ttf");
		var buffer = new byte[bufferSize * bufferSize];

		var info = StbTrueTypeFont.FromMemory(bytes);

		int ascent, descent, lineGap;
		info.stbtt_GetFontVMetrics(out ascent, out descent, out lineGap);
		var lineHeight = ascent - descent + lineGap;

		var scale = info.stbtt_ScaleForPixelHeight(fontSize);

		ascent = (int)(ascent * scale + 0.5f);
		descent = (int)(descent * scale - 0.5f);
		lineHeight = (int)(lineHeight * scale + 0.5f);

		int posX = 0, posY = 0;

		for (var i = 0; i < s.Length; ++i)
		{
			var c = s[i];
			var glyphId = info.stbtt_FindGlyphIndex(c);
			if (glyphId == 0)
			{
				continue;
			}

			int advanceTemp, lsbTemp;
			info.stbtt_GetGlyphHMetrics(glyphId, out advanceTemp, out lsbTemp);
			var advance = (int)(advanceTemp * scale + 0.5f);

			int x0, y0, x1, y1;
			info.stbtt_GetGlyphBitmapBox(glyphId, scale, scale, out x0, out y0, out x1, out y1);

			posX += x0;
			posY = ascent + y0;

			if (posY < 0)
			{
				posY = 0;
			}

			var ptr = new FakePtr<byte>(buffer, posX + posY * bufferSize);
			info.stbtt_MakeGlyphBitmap(
				ptr,
				x1 - x0,
				y1 - y0,
				bufferSize,
				scale,
				scale,
				glyphId
			);

			posX += advance;
		}

		var imageWriter = new ImageWriter();
		using (var stream = File.OpenWrite("output.png"))
		{
			imageWriter.WritePng(buffer, bufferSize, bufferSize, ColorComponents.Grey, stream);
		}
	}
}

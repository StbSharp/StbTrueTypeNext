using StbImageWriteSharp;
using System;
using System.IO;
using static StbTrueTypeNext.StbTrueType;

namespace StbTrueTypeNext.Samples;

unsafe class Program
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

		var info = new stbtt_fontinfo();

		var res = stbtt_InitFont(info, bytes, 0);

		int ascent, descent, lineGap;
		stbtt_GetFontVMetrics(info, out ascent, out descent, out lineGap);
		var lineHeight = ascent - descent + lineGap;

		var scale = stbtt_ScaleForPixelHeight(info, fontSize);

		ascent = (int)(ascent * scale + 0.5f);
		descent = (int)(descent * scale - 0.5f);
		lineHeight = (int)(lineHeight * scale + 0.5f);

		int posX = 0, posY = 0;

		for (var i = 0; i < s.Length; ++i)
		{
			var c = s[i];
			var glyphId = stbtt_FindGlyphIndex(info, c);
			if (glyphId == 0)
			{
				continue;
			}

			int advanceTemp, lsbTemp;
			stbtt_GetGlyphHMetrics(info, glyphId, out advanceTemp, out lsbTemp);
			var advance = (int)(advanceTemp * scale + 0.5f);

			int x0, y0, x1, y1;
			stbtt_GetGlyphBitmapBox(
				info, glyphId, scale, scale, out x0, out y0, out x1, out y1
			);

			posX += x0;
			posY = ascent + y0;

			if (posY < 0)
			{
				posY = 0;
			}

			var ptr = new FakePtr<byte>(buffer, posX + posY * bufferSize);
			stbtt_MakeGlyphBitmap(
				info,
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

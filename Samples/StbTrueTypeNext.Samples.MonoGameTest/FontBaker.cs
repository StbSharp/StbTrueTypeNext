using StbTrueTypeNext.Packing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StbTrueTypeNext.Samples;

public class FontBaker
{
	private byte[] _bitmap;
	private Packer _context;
	private Dictionary<int, GlyphInfo> _glyphs;
	private int bitmapWidth, bitmapHeight;

	public void Begin(int width, int height)
	{
		bitmapWidth = width;
		bitmapHeight = height;
		_bitmap = new byte[width * height];
		_context = new Packer();

		_context.stbtt_PackBegin(_bitmap, width, height, width, 1);

		_glyphs = new Dictionary<int, GlyphInfo>();
	}

	public void Add(byte[] ttf, float fontPixelHeight,
		IEnumerable<CharacterRange> characterRanges)
	{
		if (ttf == null || ttf.Length == 0)
			throw new ArgumentNullException(nameof(ttf));

		if (fontPixelHeight <= 0)
			throw new ArgumentOutOfRangeException(nameof(fontPixelHeight));

		if (characterRanges == null)
			throw new ArgumentNullException(nameof(characterRanges));

		if (!characterRanges.Any())
			throw new ArgumentException("characterRanges must have a least one value.");

		var font = STTFont.FromMemory(ttf);
		var scaleFactor = font.stbtt_ScaleForPixelHeight(fontPixelHeight);

		int ascent, descent, lineGap;
		font.stbtt_GetFontVMetrics(out ascent, out descent, out lineGap);

		foreach (var range in characterRanges)
		{
			if (range.Start > range.End)
				continue;

			var cd = new PackedChar[range.End - range.Start + 1];
			_context.stbtt_PackFontRange(font, fontPixelHeight,
				range.Start,
				range.End - range.Start + 1,
				cd);

			for (var i = 0; i < cd.Length; ++i)
			{
				var yOff = cd[i].YOff;
				yOff += ascent * scaleFactor;

				var glyphInfo = new GlyphInfo
				{
					X = cd[i].X0,
					Y = cd[i].Y0,
					Width = cd[i].X1 - cd[i].X0,
					Height = cd[i].Y1 - cd[i].Y0,
					XOffset = (int)cd[i].XOff,
					YOffset = (int)Math.Round(yOff),
					XAdvance = (int)Math.Round(cd[i].XAdvance)
				};

				_glyphs[i + range.Start] = glyphInfo;
			}
		}
	}

	public FontBakerResult End()
	{
		_context.stbtt_PackEnd();
		return new FontBakerResult(_glyphs, _bitmap, bitmapWidth, bitmapHeight);
	}
}
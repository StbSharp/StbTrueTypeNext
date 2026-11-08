using StbRectPackSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StbTrueTypeNext.Samples;

public class FontBaker
{
	private class RectData
	{
		public STBFont Font;
		public int Codepoint;
		public int GlyphId;
		public float Scale;
		public int Ascent;

		public RectData(STBFont font, int codepoint, int glyphId, float scale, int ascent)
		{
			Font = font;
			Codepoint = codepoint;
			GlyphId = glyphId;
			Scale = scale;
			Ascent = ascent;
		}
	}

	private readonly Dictionary<int, GlyphInfo> _glyphs = new Dictionary<int, GlyphInfo>();
	private Packer _packer;
	private int _padding = 1;

	public int Padding
	{
		get => _padding;

		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			_padding = value;
		}
	}

	public void Begin()
	{
		_glyphs.Clear();
		_packer = new Packer(256, 256);
	}

	public void Add(byte[] ttf, float fontPixelHeight, IEnumerable<CharacterRange> characterRanges)
	{
		if (ttf == null || ttf.Length == 0)
			throw new ArgumentNullException(nameof(ttf));

		if (fontPixelHeight <= 0)
			throw new ArgumentOutOfRangeException(nameof(fontPixelHeight));

		if (characterRanges == null)
			throw new ArgumentNullException(nameof(characterRanges));

		if (!characterRanges.Any())
			throw new ArgumentException("characterRanges must have a least one value.");

		var font = new STBFont(ttf);
		var scale = font.ScaleForPixelHeight(fontPixelHeight);

		int ascent, descent, lineGap;
		font.GetFontVMetrics(out ascent, out descent, out lineGap);

		foreach (var range in characterRanges)
		{
			if (range.Start > range.End)
				continue;

			for (var i = range.Start; i <= range.End; ++i)
			{
				var glyph = font.FindGlyphIndex(i);
				if (glyph == 0)
				{
					continue;
				}

				int advanceWidth, leftSideBearing;
				font.GetGlyphHMetrics(glyph, out advanceWidth, out leftSideBearing);
				var glyphInfo = new GlyphInfo
				{
					XAdvance = (int)Math.Round(advanceWidth * scale)
				};

				_glyphs[i] = glyphInfo;

				var box = font.GetGlyphBoxInPixels(glyph, scale, scale);
				if (box == null)
				{
					continue;
				}

				var v = box.Value;
				if (v.Width <= 0 || v.Height <= 0)
				{
					continue;
				}

				var rd = new RectData(font, i, glyph, scale, ascent);
				var pr = _packer.PackRect(v.Width + Padding * 2, v.Height + Padding * 2, rd);
				while (pr == null)
				{
					// If pr is null, it means there's no place for the new rect
					// Double the size of the packer until the new rectangle will fit
					Packer newPacker = new Packer(_packer.Width * 2, _packer.Height * 2);

					// Place existing rectangles
					foreach (PackerRectangle existingRect in _packer.PackRectangles)
					{
						newPacker.PackRect(existingRect.Width, existingRect.Height, existingRect.Data);
					}

					// Now dispose old packer and assign new one
					_packer.Dispose();
					_packer = newPacker;

					// Try to fit the rectangle again
					pr = _packer.PackRect(v.Width, v.Height, rd);
				}
			}
		}
	}

	public FontBakerResult End()
	{
		var bitmap = new byte[_packer.Width * _packer.Height];
		foreach (var pr in _packer.PackRectangles)
		{
			var rd = (RectData)pr.Data;

			var bm = rd.Font.DrawGlyphOnBitmap(rd.GlyphId, bitmap, pr.X + Padding + (pr.Y + Padding) * _packer.Width, _packer.Width, rd.Scale, rd.Scale);
			if (bm == null)
			{
				continue;
			}

			var yOff = (float)bm.YOffset;
			yOff += rd.Ascent * rd.Scale;

			var glyphInfo = _glyphs[rd.Codepoint];
			glyphInfo.X = pr.X + Padding;
			glyphInfo.Y = pr.Y + Padding;
			glyphInfo.Width = bm.Width;
			glyphInfo.Height = bm.Height;
			glyphInfo.XOffset = bm.XOffset;
			glyphInfo.YOffset = (int)yOff;

			_glyphs[rd.Codepoint] = glyphInfo;
		}

		return new FontBakerResult(_glyphs, bitmap, _packer.Width, _packer.Height);
	}
}
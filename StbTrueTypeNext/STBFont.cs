using System;
using System.Text;

namespace StbTrueTypeNext
{
	/// <summary>
	/// Reads a TrueType or OpenType font and exposes its glyphs for measurement, rasterization,
	/// atlas baking and packing, kerning, and name lookup.
	/// </summary>
	/// <remarks>
	/// Parsing happens in the constructor: the 'cmap', 'head', 'hhea', and 'hmtx' tables are required,
	/// and outlines are read from 'glyf'/'loca' for TrueType fonts or from 'CFF ' for CFF-based fonts.
	/// </remarks>
	public partial class STBFont
	{
		private Buffer _cff;
		private Buffer _charstrings;
		private byte[] _data = null;
		private Buffer _fdselect;
		private Buffer _fontdicts;
		private int _fontstart;
		private int _glyf;
		private int _gpos;
		private Buffer _gsubrs;
		private int _head;
		private int _hhea;
		private int _hmtx;
		private int _indexMap;
		private int _indexToLocFormat;
		private int _kern;
		private int _loca;
		private int _numGlyphs;
		private Buffer _subrs;
		private int _svg;

		/// <summary>Gets the raw font file data this font was created from.</summary>
		public byte[] Data => _data;

		/// <summary>
		/// Gets or sets the rasterizer version used when generating glyph bitmaps.
		/// Defaults to <see cref="STBRasterizerVersion.V2" />.
		/// </summary>
		public STBRasterizerVersion RasterizerVersion { get; set; } = STBRasterizerVersion.V2;

		/// <summary>Creates a font from the raw bytes of a TrueType or OpenType font file.</summary>
		/// <param name="data">
		/// Complete contents of a font file, such as the bytes of a .ttf, .otf, or .ttc file. The array is
		/// read in place, not copied, so it must remain unchanged while the font is in use. It must contain
		/// the 'cmap', 'head', 'hhea', and 'hmtx' tables, plus either 'glyf' and 'loca' for TrueType outlines
		/// or 'CFF ' for CFF outlines; 'maxp' is used for the glyph count when present.
		/// </param>
		/// <param name="offset">
		/// Byte offset of the font within <paramref name="data" />; pass a non-zero value to select one font
		/// from a TrueType/OpenType collection.
		/// </param>
		/// <exception cref="Exception">Thrown when a required font table, such as 'cmap', 'head', or 'hhea', is missing.</exception>
		public STBFont(byte[] data, int offset = 0)
		{
			uint cmap = 0;
			uint t = 0;
			var i = 0;
			var numTables = 0;
			_data = data;
			_fontstart = offset;
			_cff = Buffer.Null;

			var dataPtr = new FakePtr<byte>(data);
			cmap = Utility.FindTable(dataPtr, (uint)offset, "cmap");
			_loca = (int)Utility.FindTable(dataPtr, (uint)offset, "loca");
			_head = (int)Utility.FindTable(dataPtr, (uint)offset, "head");
			_glyf = (int)Utility.FindTable(dataPtr, (uint)offset, "glyf");
			_hhea = (int)Utility.FindTable(dataPtr, (uint)offset, "hhea");
			_hmtx = (int)Utility.FindTable(dataPtr, (uint)offset, "hmtx");
			_kern = (int)Utility.FindTable(dataPtr, (uint)offset, "kern");
			_gpos = (int)Utility.FindTable(dataPtr, (uint)offset, "GPOS");

			if (cmap == 0)
			{
				throw new Exception("Could not find 'cmap'");
			}

			if (_head == 0)
			{
				throw new Exception("Could not find 'head'");
			}

			if (_hhea == 0)
			{
				throw new Exception("Could not find 'hhea'");
			}

			if (_hmtx == 0)
			{
				throw new Exception("Could not find 'htmx'");
			}

			if (_glyf != 0)
			{
				if (_loca == 0)
				{
					throw new Exception("Could not find 'loca'");
				}
			}
			else
			{
				var b = new Buffer();
				var topdict = new Buffer();
				var topdictidx = new Buffer();
				uint cff = 0;
				cff = Utility.FindTable(dataPtr, (uint)offset, "CFF ");
				if (cff == 0)
				{
					throw new Exception("Could not find 'CFF '");
				}

				_fontdicts = Buffer.Null;
				_fdselect = Buffer.Null;
				_cff = new Buffer(dataPtr + cff, 512 * 1024 * 1024);
				b = _cff;
				b.Skip(2);
				b.Seek(b.ReadByte());
				b.ReadCffIndex();
				topdictidx = b.ReadCffIndex();
				topdict = Buffer.ReadCffIndexBy(topdictidx, 0);
				b.ReadCffIndex();
				_gsubrs = b.ReadCffIndex();

				uint cstype = 2;
				uint charstrings = 0;
				uint fdarrayoff = 0;
				uint fdselectoff = 0;
				topdict.ReadCffDictUint(17, ref charstrings);
				topdict.ReadCffDictUint(0x100 | 6, ref cstype);
				topdict.ReadCffDictUint(0x100 | 36, ref fdarrayoff);
				topdict.ReadCffDictUint(0x100 | 37, ref fdselectoff);

				_subrs = Buffer.ReadCffSubrs(b, topdict);
				if (cstype != 2)
				{
					throw new Exception("cstype != 2");
				}

				if (charstrings == 0)
				{
					throw new Exception("charstrings == 0");
				}

				if (fdarrayoff != 0)
				{
					if (fdselectoff == 0)
					{
						throw new Exception("fdselectoff == 0");
					}

					b.Seek((int)fdarrayoff);
					_fontdicts = b.ReadCffIndex();
					_fdselect = b.CreateRange((int)fdselectoff, (int)(b.Size - fdselectoff));
				}

				b.Seek((int)charstrings);
				_charstrings = b.ReadCffIndex();
			}

			t = Utility.FindTable(dataPtr, (uint)offset, "maxp");
			if (t != 0)
				_numGlyphs = Utility.ttUSHORT(dataPtr + t + 4);
			else
				_numGlyphs = 0xffff;
			_svg = -1;
			numTables = Utility.ttUSHORT(dataPtr + cmap + 2);
			_indexMap = 0;
			for (i = 0; i < numTables; ++i)
			{
				var encoding_record = (uint)(cmap + 4 + 8 * i);
				switch (Utility.ttUSHORT(dataPtr + encoding_record))
				{
					case STBTT_PLATFORM_ID_MICROSOFT:
						switch (Utility.ttUSHORT(dataPtr + encoding_record + 2))
						{
							case STBTT_MS_EID_UNICODE_BMP:
							case STBTT_MS_EID_UNICODE_FULL:
								_indexMap = (int)(cmap + Utility.ttULONG(dataPtr + encoding_record + 4));
								break;
						}

						break;
					case STBTT_PLATFORM_ID_UNICODE:
						_indexMap = (int)(cmap + Utility.ttULONG(dataPtr + encoding_record + 4));
						break;
				}
			}

			if (_indexMap == 0)
				throw new Exception("The font does not have a table mapping from unicode codepoints to font indices.");
			_indexToLocFormat = Utility.ttUSHORT(dataPtr + _head + 50);
		}

		/// <summary>Finds the glyph index that a Unicode codepoint maps to.</summary>
		/// <param name="unicode_codepoint">Unicode codepoint to look up.</param>
		/// <returns>The glyph index, or 0 if the codepoint is not mapped.</returns>
		public int FindGlyphIndex(int unicode_codepoint)
		{
			var data = new FakePtr<byte>(_data);
			var index_map = (uint)_indexMap;
			var format = Utility.ttUSHORT(data + index_map + 0);
			if (format == 0)
			{
				int bytes = Utility.ttUSHORT(data + index_map + 2);
				if (unicode_codepoint < bytes - 6)
					return (data + index_map + 6 + unicode_codepoint).FirstValue;
				return 0;
			}

			if (format == 6)
			{
				uint first = Utility.ttUSHORT(data + index_map + 6);
				uint count = Utility.ttUSHORT(data + index_map + 8);
				if ((uint)unicode_codepoint >= first && (uint)unicode_codepoint < first + count)
					return Utility.ttUSHORT(data + index_map + 10 + (unicode_codepoint - first) * 2);
				return 0;
			}

			if (format == 2) return 0;

			if (format == 4)
			{
				var segcount = (ushort)(Utility.ttUSHORT(data + index_map + 6) >> 1);
				var searchRange = (ushort)(Utility.ttUSHORT(data + index_map + 8) >> 1);
				var entrySelector = Utility.ttUSHORT(data + index_map + 10);
				var rangeShift = (ushort)(Utility.ttUSHORT(data + index_map + 12) >> 1);
				var endCount = index_map + 14;
				var search = endCount;
				if (unicode_codepoint > 0xffff)
					return 0;
				if (unicode_codepoint >= Utility.ttUSHORT(data + search + rangeShift * 2))
					search += (uint)(rangeShift * 2);
				search -= 2;
				while (entrySelector != 0)
				{
					ushort end = 0;
					searchRange >>= 1;
					end = Utility.ttUSHORT(data + search + searchRange * 2);
					if (unicode_codepoint > end)
						search += (uint)(searchRange * 2);
					--entrySelector;
				}

				search += 2;
				{
					ushort offset = 0;
					ushort start = 0;
					ushort last = 0;
					var item = (ushort)((search - endCount) >> 1);
					start = Utility.ttUSHORT(data + index_map + 14 + segcount * 2 + 2 + 2 * item);
					last = Utility.ttUSHORT(data + endCount + 2 * item);
					if (unicode_codepoint < start || unicode_codepoint > last)
						return 0;
					offset = Utility.ttUSHORT(data + index_map + 14 + segcount * 6 + 2 + 2 * item);
					if (offset == 0)
						return (ushort)(unicode_codepoint +
										 Utility.ttSHORT(data + index_map + 14 + segcount * 4 + 2 + 2 * item));
					return Utility.ttUSHORT(data + offset + (unicode_codepoint - start) * 2 + index_map + 14 + segcount * 6 +
									2 + 2 * item);
				}
			}

			if (format == 12 || format == 13)
			{
				var ngroups = Utility.ttULONG(data + index_map + 12);
				var low = 0;
				var high = 0;
				low = 0;
				high = (int)ngroups;
				while (low < high)
				{
					var mid = low + ((high - low) >> 1);
					var start_char = Utility.ttULONG(data + index_map + 16 + mid * 12);
					var end_char = Utility.ttULONG(data + index_map + 16 + mid * 12 + 4);
					if ((uint)unicode_codepoint < start_char)
					{
						high = mid;
					}
					else if ((uint)unicode_codepoint > end_char)
					{
						low = mid + 1;
					}
					else
					{
						var start_glyph = Utility.ttULONG(data + index_map + 16 + mid * 12 + 8);
						if (format == 12)
							return (int)(start_glyph + unicode_codepoint - start_char);
						return (int)start_glyph;
					}
				}

				return 0;
			}

			return 0;
		}

		/// <summary>Reads a string from the font's name table.</summary>
		/// <param name="platformID">Platform ID of the name record, such as <see cref="STBTT_PLATFORM_ID_MICROSOFT" />.</param>
		/// <param name="encodingID">Encoding ID of the name record for that platform.</param>
		/// <param name="languageID">Language ID of the name record for that platform and encoding.</param>
		/// <param name="nameID">Name ID of the string, for example the family or full font name.</param>
		/// <returns>The matching string, or null if the font has no such name record.</returns>
		public string GetFontNameString(int platformID, int encodingID, int languageID, int nameID)
		{
			var i = 0;
			var count = 0;
			var stringOffset = 0;
			var fc = new FakePtr<byte>(_data);
			var offset = (uint)_fontstart;
			var nm = Utility.FindTable(fc, offset, "name");
			if (nm == 0)
			{
				return null;
			}

			count = Utility.ttUSHORT(fc + nm + 2);
			stringOffset = (int)(nm + Utility.ttUSHORT(fc + nm + 4));
			for (i = 0; i < count; ++i)
			{
				var loc = (uint)(nm + 6 + 12 * i);
				if (platformID == Utility.ttUSHORT(fc + loc + 0) && encodingID == Utility.ttUSHORT(fc + loc + 2) &&
					languageID == Utility.ttUSHORT(fc + loc + 4) && nameID == Utility.ttUSHORT(fc + loc + 6))
				{
					var sb = new StringBuilder();
					var length = Utility.ttUSHORT(fc + loc + 8);

					var ptr = fc + stringOffset + Utility.ttUSHORT(fc + loc + 10);
					for (var j = 0; j < length; ++j)
					{
						sb.Append((char)ptr[j]);
					}

					return sb.ToString();
				}
			}

			return null;
		}

		/// <summary>Computes the scale that maps font units to pixels using the font's units-per-em value.</summary>
		/// <param name="pixels">Desired size of one em, in pixels.</param>
		/// <returns>The scale factor to pass to the glyph bitmap and box methods.</returns>
		public float ScaleForMappingEmToPixels(float pixels)
		{
			var dataPtr = new FakePtr<byte>(_data);
			int unitsPerEm = Utility.ttUSHORT(dataPtr + _head + 18);
			return pixels / unitsPerEm;
		}

		/// <summary>Computes the scale that maps font units to pixels for a desired ascent-to-descent height.</summary>
		/// <param name="height">Desired height of the font, in pixels.</param>
		/// <returns>The scale factor to pass to the glyph bitmap and box methods.</returns>
		public float ScaleForPixelHeight(float height)
		{
			var dataPtr = new FakePtr<byte>(_data);
			var fheight = Utility.ttSHORT(dataPtr + _hhea + 4) - Utility.ttSHORT(dataPtr + _hhea + 6);
			return height / fheight;
		}

		/// <summary>Bakes a contiguous range of codepoints into an 8-bit font atlas bitmap.</summary>
		/// <param name="pixel_height">Desired font height, in pixels.</param>
		/// <param name="pixels">Destination atlas bitmap; must hold at least <paramref name="pw" /> * <paramref name="ph" /> bytes and is cleared first.</param>
		/// <param name="pw">Width of the atlas bitmap, in pixels.</param>
		/// <param name="ph">Height of the atlas bitmap, in pixels.</param>
		/// <param name="first_char">First Unicode codepoint of the baked range.</param>
		/// <param name="num_chars">Number of consecutive codepoints to bake.</param>
		/// <param name="chardata">Receives the packed glyph data, one <see cref="STBBakedChar" /> per character.</param>
		/// <returns>The height of the used atlas rows, or the negated index of the first character that did not fit.</returns>
		public int BakeFontBitmap(float pixel_height, byte[] pixels, int pw, int ph, int first_char, int num_chars, STBBakedChar[] chardata)
		{
			float scale = 0;
			var x = 0;
			var y = 0;
			var bottom_y = 0;
			var i = 0;

			pixels.Set((byte)0, pw * ph);
			x = y = 1;
			bottom_y = 1;
			scale = ScaleForPixelHeight(pixel_height);
			for (i = 0; i < num_chars; ++i)
			{
				var advance = 0;
				var lsb = 0;
				var gw = 0;
				var gh = 0;
				var g = FindGlyphIndex(first_char + i);
				GetGlyphHMetrics(g, out advance, out lsb);
				var box = GetGlyphBitmapBoxSubpixel(g, scale, scale) ?? STBGlyphBitmapBox.Zero;
				gw = box.Width;
				gh = box.Height;
				if (x + gw + 1 >= pw)
				{
					y = bottom_y;
					x = 1;
				}

				if (y + gh + 1 >= ph)
					return -i;
				MakeGlyphBitmap(new FakePtr<byte>(pixels, x + y * pw), gw, gh, pw, scale, scale, g);
				chardata[i].X0 = (ushort)(short)x;
				chardata[i].Y0 = (ushort)(short)y;
				chardata[i].X1 = (ushort)(short)(x + gw);
				chardata[i].Y1 = (ushort)(short)(y + gh);
				chardata[i].XAdvance = scale * advance;
				chardata[i].XOff = box.X0;
				chardata[i].YOff = box.Y0;
				x = x + gw + 1;
				if (y + gh + 1 > bottom_y)
					bottom_y = y + gh + 1;
			}

			return bottom_y;
		}
	}
}

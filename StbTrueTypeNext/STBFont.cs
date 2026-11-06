using System;
using System.IO;
using System.Text;

namespace StbTrueTypeNext
{
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
		public byte[] Data => _data;

		public STBRasterizerVersion RasterizerVersion { get; set; } = STBRasterizerVersion.V2;

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
			cmap = Utility.stbtt__find_table(dataPtr, (uint)offset, "cmap");
			_loca = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "loca");
			_head = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "head");
			_glyf = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "glyf");
			_hhea = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "hhea");
			_hmtx = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "hmtx");
			_kern = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "kern");
			_gpos = (int)Utility.stbtt__find_table(dataPtr, (uint)offset, "GPOS");

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
				cff = Utility.stbtt__find_table(dataPtr, (uint)offset, "CFF ");
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

			t = Utility.stbtt__find_table(dataPtr, (uint)offset, "maxp");
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

		public int stbtt_FindGlyphIndex(int unicode_codepoint)
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

		public string stbtt_GetFontNameString(int platformID, int encodingID, int languageID, int nameID)
		{
			var i = 0;
			var count = 0;
			var stringOffset = 0;
			var fc = new FakePtr<byte>(_data);
			var offset = (uint)_fontstart;
			var nm = Utility.stbtt__find_table(fc, offset, "name");
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

		public float stbtt_ScaleForMappingEmToPixels(float pixels)
		{
			var dataPtr = new FakePtr<byte>(_data);
			int unitsPerEm = Utility.ttUSHORT(dataPtr + _head + 18);
			return pixels / unitsPerEm;
		}

		public float stbtt_ScaleForPixelHeight(float height)
		{
			var dataPtr = new FakePtr<byte>(_data);
			var fheight = Utility.ttSHORT(dataPtr + _hhea + 4) - Utility.ttSHORT(dataPtr + _hhea + 6);
			return height / fheight;
		}

		public int stbtt_BakeFontBitmap(float pixel_height, byte[] pixels, int pw, int ph, int first_char, int num_chars, STBBakedChar[] chardata)
		{
			float scale = 0;
			var x = 0;
			var y = 0;
			var bottom_y = 0;
			var i = 0;

			pixels.Set((byte)0, pw * ph);
			x = y = 1;
			bottom_y = 1;
			scale = stbtt_ScaleForPixelHeight(pixel_height);
			for (i = 0; i < num_chars; ++i)
			{
				var advance = 0;
				var lsb = 0;
				var x0 = 0;
				var y0 = 0;
				var x1 = 0;
				var y1 = 0;
				var gw = 0;
				var gh = 0;
				var g = stbtt_FindGlyphIndex(first_char + i);
				stbtt_GetGlyphHMetrics(g, out advance, out lsb);
				GetGlyphBitmapBox(g, scale, scale, out x0, out y0, out x1, out y1);
				gw = x1 - x0;
				gh = y1 - y0;
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
				chardata[i].XOff = x0;
				chardata[i].YOff = y0;
				x = x + gw + 1;
				if (y + gh + 1 > bottom_y)
					bottom_y = y + gh + 1;
			}

			return bottom_y;
		}
	}
}

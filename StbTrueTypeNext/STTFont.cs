using System;
using System.IO;
using System.Text;

namespace StbTrueTypeNext
{
	public partial class STTFont
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

		public STTRasterizerVersion RasterizerVersion { get; set; } = STTRasterizerVersion.V2;

		private STTFont()
		{
		}

		public static STTFont FromMemory(byte[] data, int offset = 0)
		{
			var info = new STTFont();
			if (info.stbtt_InitFont_internal(data, offset) == 0)
			{
				throw new Exception("Unable to parse font");
			}

			return info;
		}

		public static STTFont FromStream(Stream s)
		{
			using (var ms = new MemoryStream())
			{
				s.CopyTo(ms);

				return FromMemory(ms.ToArray());
			}
		}

		private int stbtt_InitFont_internal(byte[] data, int fontstart)
		{
			uint cmap = 0;
			uint t = 0;
			var i = 0;
			var numTables = 0;
			_data = data;
			_fontstart = fontstart;
			_cff = Buffer.Null;

			var dataPtr = new FakePtr<byte>(data);
			cmap = Utility.stbtt__find_table(dataPtr, (uint)fontstart, "cmap");
			_loca = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "loca");
			_head = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "head");
			_glyf = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "glyf");
			_hhea = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "hhea");
			_hmtx = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "hmtx");
			_kern = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "kern");
			_gpos = (int)Utility.stbtt__find_table(dataPtr, (uint)fontstart, "GPOS");
			if (cmap == 0 || _head == 0 || _hhea == 0 || _hmtx == 0)
				return 0;
			if (_glyf != 0)
			{
				if (_loca == 0)
					return 0;
			}
			else
			{
				var b = new Buffer();
				var topdict = new Buffer();
				var topdictidx = new Buffer();
				uint cff = 0;
				cff = Utility.stbtt__find_table(dataPtr, (uint)fontstart, "CFF ");
				if (cff == 0)
					return 0;
				_fontdicts = Buffer.Null;
				_fdselect = Buffer.Null;
				_cff = new Buffer(dataPtr + cff, 512 * 1024 * 1024);
				b = _cff;
				b.stbtt__buf_skip(2);
				b.stbtt__buf_seek(b.stbtt__buf_get8());
				b.stbtt__cff_get_index();
				topdictidx = b.stbtt__cff_get_index();
				topdict = Buffer.stbtt__cff_index_get(topdictidx, 0);
				b.stbtt__cff_get_index();
				_gsubrs = b.stbtt__cff_get_index();

				uint cstype = 2;
				uint charstrings = 0;
				uint fdarrayoff = 0;
				uint fdselectoff = 0;
				topdict.stbtt__dict_get_ints(17, ref charstrings);
				topdict.stbtt__dict_get_ints(0x100 | 6, ref cstype);
				topdict.stbtt__dict_get_ints(0x100 | 36, ref fdarrayoff);
				topdict.stbtt__dict_get_ints(0x100 | 37, ref fdselectoff);

				_subrs = Buffer.stbtt__get_subrs(b, topdict);
				if (cstype != 2)
					return 0;
				if (charstrings == 0)
					return 0;
				if (fdarrayoff != 0)
				{
					if (fdselectoff == 0)
						return 0;
					b.stbtt__buf_seek((int)fdarrayoff);
					_fontdicts = b.stbtt__cff_get_index();
					_fdselect = b.stbtt__buf_range((int)fdselectoff, (int)(b.size - fdselectoff));
				}

				b.stbtt__buf_seek((int)charstrings);
				_charstrings = b.stbtt__cff_get_index();
			}

			t = Utility.stbtt__find_table(dataPtr, (uint)fontstart, "maxp");
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
			return 1;
		}

		private Buffer stbtt__cid_get_glyph_subrs(int glyph_index)
		{
			var fdselect = _fdselect;
			var nranges = 0;
			var start = 0;
			var end = 0;
			var v = 0;
			var fmt = 0;
			var fdselector = -1;
			var i = 0;
			fdselect.stbtt__buf_seek(0);
			fmt = fdselect.stbtt__buf_get8();
			if (fmt == 0)
			{
				fdselect.stbtt__buf_skip(glyph_index);
				fdselector = fdselect.stbtt__buf_get8();
			}
			else if (fmt == 3)
			{
				nranges = (int)fdselect.stbtt__buf_get(2);
				start = (int)fdselect.stbtt__buf_get(2);
				for (i = 0; i < nranges; i++)
				{
					v = fdselect.stbtt__buf_get8();
					end = (int)fdselect.stbtt__buf_get(2);
					if (glyph_index >= start && glyph_index < end)
					{
						fdselector = v;
						break;
					}

					start = end;
				}
			}

			if (fdselector == -1)
				return Buffer.Null;
			return Buffer.stbtt__get_subrs(_cff, Buffer.stbtt__cff_index_get(_fontdicts, fdselector));
		}

		private int stbtt__GetGlyfOffset(int glyph_index)
		{
			var g1 = 0;
			var g2 = 0;
			if (glyph_index >= _numGlyphs)
				return -1;
			if (_indexToLocFormat >= 2)
				return -1;

			var dataPtr = new FakePtr<byte>(_data);
			if (_indexToLocFormat == 0)
			{
				g1 = _glyf + Utility.ttUSHORT(dataPtr + _loca + glyph_index * 2) * 2;
				g2 = _glyf + Utility.ttUSHORT(dataPtr + _loca + glyph_index * 2 + 2) * 2;
			}
			else
			{
				g1 = (int)(_glyf + Utility.ttULONG(dataPtr + _loca + glyph_index * 4));
				g2 = (int)(_glyf + Utility.ttULONG(dataPtr + _loca + glyph_index * 4 + 4));
			}

			return g1 == g2 ? -1 : g1;
		}

		private int stbtt__run_charstring(int glyph_index, CharStringContext c)
		{
			var in_header = 1;
			var maskbits = 0;
			var subr_stack_height = 0;
			var sp = 0;
			var v = 0;
			var i = 0;
			var b0 = 0;
			var has_subrs = 0;
			var clear_stack = 0;
			var s = new float[48];
			var subr_stack = new Buffer[10];
			var subrs = _subrs;
			var b = new Buffer();
			float f = 0;
			b = Buffer.stbtt__cff_index_get(_charstrings, glyph_index);
			while (b.cursor < b.size)
			{
				i = 0;
				clear_stack = 1;
				b0 = b.stbtt__buf_get8();
				switch (b0)
				{
					case 0x13:
					case 0x14:
						if (in_header != 0)
							maskbits += sp / 2;
						in_header = 0;
						b.stbtt__buf_skip((maskbits + 7) / 8);
						break;
					case 0x01:
					case 0x03:
					case 0x12:
					case 0x17:
						maskbits += sp / 2;
						break;
					case 0x15:
						in_header = 0;
						if (sp < 2)
							return 0;
						c.stbtt__csctx_rmove_to(s[sp - 2], s[sp - 1]);
						break;
					case 0x04:
						in_header = 0;
						if (sp < 1)
							return 0;
						c.stbtt__csctx_rmove_to(0, s[sp - 1]);
						break;
					case 0x16:
						in_header = 0;
						if (sp < 1)
							return 0;
						c.stbtt__csctx_rmove_to(s[sp - 1], 0);
						break;
					case 0x05:
						if (sp < 2)
							return 0;
						for (; i + 1 < sp; i += 2) c.stbtt__csctx_rline_to(s[i], s[i + 1]);

						break;
					case 0x07:
					case 0x06:
						if (sp < 1)
							return 0;
						var goto_vlineto = b0 == 0x07 ? 1 : 0;
						for (; ; )
						{
							if (goto_vlineto == 0)
							{
								if (i >= sp)
									break;
								c.stbtt__csctx_rline_to(s[i], 0);
								i++;
							}

							goto_vlineto = 0;
							if (i >= sp)
								break;
							c.stbtt__csctx_rline_to(0, s[i]);
							i++;
						}

						break;
					case 0x1F:
					case 0x1E:
						if (sp < 4)
							return 0;
						var goto_hvcurveto = b0 == 0x1F ? 1 : 0;
						for (; ; )
						{
							if (goto_hvcurveto == 0)
							{
								if (i + 3 >= sp)
									break;
								c.stbtt__csctx_rccurve_to(0, s[i], s[i + 1], s[i + 2], s[i + 3],
									sp - i == 5 ? s[i + 4] : 0.0f);
								i += 4;
							}

							goto_hvcurveto = 0;
							if (i + 3 >= sp)
								break;
							c.stbtt__csctx_rccurve_to(s[i], 0, s[i + 1], s[i + 2], sp - i == 5 ? s[i + 4] : 0.0f,
								s[i + 3]);
							i += 4;
						}

						break;
					case 0x08:
						if (sp < 6)
							return 0;
						for (; i + 5 < sp; i += 6)
							c.stbtt__csctx_rccurve_to(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);

						break;
					case 0x18:
						if (sp < 8)
							return 0;
						for (; i + 5 < sp - 2; i += 6)
							c.stbtt__csctx_rccurve_to(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);

						if (i + 1 >= sp)
							return 0;
						c.stbtt__csctx_rline_to(s[i], s[i + 1]);
						break;
					case 0x19:
						if (sp < 8)
							return 0;
						for (; i + 1 < sp - 6; i += 2) c.stbtt__csctx_rline_to(s[i], s[i + 1]);

						if (i + 5 >= sp)
							return 0;
						c.stbtt__csctx_rccurve_to(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
						break;
					case 0x1A:
					case 0x1B:
						if (sp < 4)
							return 0;
						f = (float)0.0;
						if ((sp & 1) != 0)
						{
							f = s[i];
							i++;
						}

						for (; i + 3 < sp; i += 4)
						{
							if (b0 == 0x1B)
								c.stbtt__csctx_rccurve_to(s[i], f, s[i + 1], s[i + 2], s[i + 3], (float)0.0);
							else
								c.stbtt__csctx_rccurve_to(f, s[i], s[i + 1], s[i + 2], (float)0.0, s[i + 3]);
							f = (float)0.0;
						}

						break;
					case 0x0A:
					case 0x1D:
						if (b0 == 0x0A && has_subrs == 0)
						{
							if (_fdselect.size != 0)
								subrs = stbtt__cid_get_glyph_subrs(glyph_index);
							has_subrs = 1;
						}

						if (sp < 1)
							return 0;
						v = (int)s[--sp];
						if (subr_stack_height >= 10)
							return 0;
						subr_stack[subr_stack_height++] = b;
						b = Buffer.stbtt__get_subr(b0 == 0x0A ? subrs : _gsubrs, v);
						if (b.size == 0)
							return 0;
						b.cursor = 0;
						clear_stack = 0;
						break;
					case 0x0B:
						if (subr_stack_height <= 0)
							return 0;
						b = subr_stack[--subr_stack_height];
						clear_stack = 0;
						break;
					case 0x0E:
						c.stbtt__csctx_close_shape();
						return 1;
					case 0x0C:
						{
							float dx1 = 0;
							float dx2 = 0;
							float dx3 = 0;
							float dx4 = 0;
							float dx5 = 0;
							float dx6 = 0;
							float dy1 = 0;
							float dy2 = 0;
							float dy3 = 0;
							float dy4 = 0;
							float dy5 = 0;
							float dy6 = 0;
							float dx = 0;
							float dy = 0;
							int b1 = b.stbtt__buf_get8();
							switch (b1)
							{
								case 0x22:
									if (sp < 7)
										return 0;
									dx1 = s[0];
									dx2 = s[1];
									dy2 = s[2];
									dx3 = s[3];
									dx4 = s[4];
									dx5 = s[5];
									dx6 = s[6];
									c.stbtt__csctx_rccurve_to(dx1, 0, dx2, dy2, dx3, 0);
									c.stbtt__csctx_rccurve_to(dx4, 0, dx5, -dy2, dx6, 0);
									break;
								case 0x23:
									if (sp < 13)
										return 0;
									dx1 = s[0];
									dy1 = s[1];
									dx2 = s[2];
									dy2 = s[3];
									dx3 = s[4];
									dy3 = s[5];
									dx4 = s[6];
									dy4 = s[7];
									dx5 = s[8];
									dy5 = s[9];
									dx6 = s[10];
									dy6 = s[11];
									c.stbtt__csctx_rccurve_to(dx1, dy1, dx2, dy2, dx3, dy3);
									c.stbtt__csctx_rccurve_to(dx4, dy4, dx5, dy5, dx6, dy6);
									break;
								case 0x24:
									if (sp < 9)
										return 0;
									dx1 = s[0];
									dy1 = s[1];
									dx2 = s[2];
									dy2 = s[3];
									dx3 = s[4];
									dx4 = s[5];
									dx5 = s[6];
									dy5 = s[7];
									dx6 = s[8];
									c.stbtt__csctx_rccurve_to(dx1, dy1, dx2, dy2, dx3, 0);
									c.stbtt__csctx_rccurve_to(dx4, 0, dx5, dy5, dx6, -(dy1 + dy2 + dy5));
									break;
								case 0x25:
									if (sp < 11)
										return 0;
									dx1 = s[0];
									dy1 = s[1];
									dx2 = s[2];
									dy2 = s[3];
									dx3 = s[4];
									dy3 = s[5];
									dx4 = s[6];
									dy4 = s[7];
									dx5 = s[8];
									dy5 = s[9];
									dx6 = dy6 = s[10];
									dx = dx1 + dx2 + dx3 + dx4 + dx5;
									dy = dy1 + dy2 + dy3 + dy4 + dy5;
									if (Math.Abs(dx) > Math.Abs(dy))
										dy6 = -dy;
									else
										dx6 = -dx;
									c.stbtt__csctx_rccurve_to(dx1, dy1, dx2, dy2, dx3, dy3);
									c.stbtt__csctx_rccurve_to(dx4, dy4, dx5, dy5, dx6, dy6);
									break;
								default:
									return 0;
							}
						}

						break;
					default:
						if (b0 != 255 && b0 != 28 && b0 < 32)
							return 0;
						if (b0 == 255)
						{
							f = (float)(int)b.stbtt__buf_get(4) / 0x10000;
						}
						else
						{
							b.stbtt__buf_skip(-1);
							f = (short)b.stbtt__cff_int();
						}

						if (sp >= 48)
							return 0;
						s[sp++] = f;
						clear_stack = 0;
						break;
				}

				if (clear_stack != 0)
					sp = 0;
			}

			return 0;
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

		public byte[] stbtt_GetCodepointBitmap(float scale_x, float scale_y, int codepoint,
			out int width, out int height, out int xoff, out int yoff)
		{
			return stbtt_GetCodepointBitmapSubpixel(scale_x, scale_y, 0.0f, 0.0f, codepoint,
				out width, out height, out xoff, out yoff);
		}

		public void stbtt_GetCodepointBitmapBox(int codepoint, float scale_x, float scale_y,
			out int ix0, out int iy0, out int ix1, out int iy1)
		{
			stbtt_GetCodepointBitmapBoxSubpixel(codepoint, scale_x, scale_y, 0.0f, 0.0f, out ix0, out iy0, out ix1, out iy1);
		}

		public void stbtt_GetCodepointBitmapBoxSubpixel(int codepoint, float scale_x,
			float scale_y, float shift_x, float shift_y, out int ix0, out int iy0, out int ix1, out int iy1)
		{
			stbtt_GetGlyphBitmapBoxSubpixel(stbtt_FindGlyphIndex(codepoint), scale_x, scale_y, shift_x,
				shift_y, out ix0, out iy0, out ix1, out iy1);
		}

		public byte[] stbtt_GetCodepointBitmapSubpixel(float scale_x, float scale_y,
			float shift_x, float shift_y, int codepoint, out int width, out int height, out int xoff, out int yoff)
		{
			return stbtt_GetGlyphBitmapSubpixel(scale_x, scale_y, shift_x, shift_y,
				stbtt_FindGlyphIndex(codepoint), out width, out height, out xoff, out yoff);
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

		public byte[] stbtt_GetGlyphBitmap(float scale_x, float scale_y, int glyph,
			out int width, out int height, out int xoff, out int yoff)
		{
			return stbtt_GetGlyphBitmapSubpixel(scale_x, scale_y, 0.0f, 0.0f, glyph, out width, out height, out xoff, out yoff);
		}

		public void stbtt_GetGlyphBitmapBox(int glyph, float scale_x, float scale_y,
			out int ix0, out int iy0, out int ix1, out int iy1)
		{
			stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale_x, scale_y, 0.0f, 0.0f, out ix0, out iy0, out ix1, out iy1);
		}

		public void stbtt_GetGlyphBitmapBoxSubpixel(int glyph, float scale_x, float scale_y,
			float shift_x, float shift_y, out int ix0, out int iy0, out int ix1, out int iy1)
		{
			var x0 = 0;
			var y0 = 0;
			var x1 = 0;
			var y1 = 0;
			if (stbtt_GetGlyphBox(glyph, out x0, out y0, out x1, out y1) == 0)
			{
				ix0 = 0;
				iy0 = 0;
				ix1 = 0;
				iy1 = 0;
			}
			else
			{
				ix0 = (int)Math.Floor(x0 * scale_x + shift_x);
				iy0 = (int)Math.Floor(-y1 * scale_y + shift_y);
				ix1 = (int)Math.Ceiling(x1 * scale_x + shift_x);
				iy1 = (int)Math.Ceiling(-y0 * scale_y + shift_y);
			}
		}

		public byte[] stbtt_GetGlyphBitmapSubpixel(float scale_x, float scale_y, float shift_x, float shift_y, int glyph,
			out int width, out int height, out int xoff, out int yoff)
		{
			width = height = xoff = yoff = 0;

			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			STTVertex[] vertices;
			var num_verts = stbtt_GetGlyphShape(glyph, out vertices);
			if (scale_x == 0)
				scale_x = scale_y;
			if (scale_y == 0)
			{
				if (scale_x == 0)
				{
					return null;
				}

				scale_y = scale_x;
			}

			stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale_x, scale_y, shift_x, shift_y, out ix0, out iy0, out ix1, out iy1);

			width = ix1 - ix0;
			height = iy1 - iy0;
			xoff = ix0;
			yoff = iy0;

			if (width <= 0 || height <= 0)
			{
				return null;
			}

			var gbm = new Bitmap(width, height);
			gbm.stbtt_Rasterize(0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, ix0, iy0, 1, RasterizerVersion);

			return gbm.Pixels.Data;
		}

		public void stbtt_MakeCodepointBitmap(byte[] output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, int codepoint)
		{
			stbtt_MakeCodepointBitmapSubpixel(output, out_w, out_h, out_stride, scale_x, scale_y, 0.0f, 0.0f, codepoint);
		}

		public void stbtt_MakeCodepointBitmapSubpixel(byte[] output, int out_w, int out_h,
			int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int codepoint)
		{
			stbtt_MakeGlyphBitmapSubpixel(new FakePtr<byte>(output), out_w, out_h, out_stride,
				scale_x, scale_y, shift_x, shift_y, stbtt_FindGlyphIndex(codepoint));
		}

		public void stbtt_MakeCodepointBitmapSubpixelPrefilter(byte[] output, int out_w,
			int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int oversample_x,
			int oversample_y, out float sub_x, out float sub_y, int codepoint)
		{
			stbtt_MakeGlyphBitmapSubpixelPrefilter(output, out_w, out_h, out_stride, scale_x, scale_y, shift_x,
				shift_y, oversample_x, oversample_y, out sub_x, out sub_y, stbtt_FindGlyphIndex(codepoint));
		}

		public void stbtt_MakeGlyphBitmap(byte[] output, int out_w, int out_h, int out_stride,
			float scale_x, float scale_y, int glyph)
		{
			stbtt_MakeGlyphBitmap(new FakePtr<byte>(output), out_w, out_h, out_stride, scale_x, scale_y, glyph);
		}

		public void stbtt_MakeGlyphBitmap(FakePtr<byte> output, int out_w, int out_h, int out_stride,
			float scale_x, float scale_y, int glyph)
		{
			stbtt_MakeGlyphBitmapSubpixel(output, out_w, out_h, out_stride, scale_x, scale_y, 0.0f, 0.0f, glyph);
		}

		public void stbtt_MakeGlyphBitmapSubpixel(byte[] output, int out_w, int out_h,
			int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int glyph)
		{
			stbtt_MakeGlyphBitmapSubpixel(new FakePtr<byte>(output), out_w, out_h, out_stride, scale_x, scale_y, shift_x, shift_y, glyph);
		}

		public void stbtt_MakeGlyphBitmapSubpixel(FakePtr<byte> output, int out_w, int out_h,
			int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int glyph)
		{
			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			STTVertex[] vertices;
			var num_verts = stbtt_GetGlyphShape(glyph, out vertices);
			stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale_x, scale_y, shift_x, shift_y, out ix0, out iy0, out ix1, out iy1);

			if (out_w != 0 && out_h != 0)
			{
				var gbm = new Bitmap(out_w, out_h, out_stride, output);
				gbm.stbtt_Rasterize(0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, ix0, iy0, 1, RasterizerVersion);
			}
		}

		public void stbtt_MakeGlyphBitmapSubpixelPrefilter(byte[] output, int out_w, int out_h, int out_stride,
			float scale_x, float scale_y, float shift_x, float shift_y, int prefilter_x, int prefilter_y,
			out float sub_x, out float sub_y, int glyph)
		{
			stbtt_MakeGlyphBitmapSubpixel(output, out_w - (prefilter_x - 1), out_h - (prefilter_y - 1),
				out_stride, scale_x, scale_y, shift_x, shift_y, glyph);
			if (prefilter_x > 1)
				Utility.stbtt__h_prefilter(output, out_w, out_h, out_stride, (uint)prefilter_x);
			if (prefilter_y > 1)
				Utility.stbtt__v_prefilter(output, out_w, out_h, out_stride, (uint)prefilter_y);
			sub_x = Utility.stbtt__oversample_shift(prefilter_x);
			sub_y = Utility.stbtt__oversample_shift(prefilter_y);
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

		public int stbtt_BakeFontBitmap(float pixel_height, byte[] pixels, int pw, int ph, int first_char, int num_chars, STTBakedChar[] chardata)
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
				stbtt_GetGlyphBitmapBox(g, scale, scale, out x0, out y0, out x1, out y1);
				gw = x1 - x0;
				gh = y1 - y0;
				if (x + gw + 1 >= pw)
				{
					y = bottom_y;
					x = 1;
				}

				if (y + gh + 1 >= ph)
					return -i;
				stbtt_MakeGlyphBitmap(new FakePtr<byte>(pixels, x + y * pw), gw, gh, pw, scale, scale, g);
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

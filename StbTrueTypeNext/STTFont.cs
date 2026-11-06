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

		private int stbtt__get_svg()
		{
			uint t = 0;

			var dataPtr = new FakePtr<byte>(_data);
			if (_svg < 0)
			{
				t = Utility.stbtt__find_table(dataPtr, (uint)_fontstart, "SVG ");
				if (t != 0)
				{
					var offset = Utility.ttULONG(dataPtr + t + 2);
					_svg = (int)(t + offset);
				}
				else
				{
					_svg = 0;
				}
			}

			return _svg;
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

		private static int stbtt__GetGlyphClass(FakePtr<byte> classDefTable, int glyph)
		{
			var classDefFormat = Utility.ttUSHORT(classDefTable);
			switch (classDefFormat)
			{
				case 1:
					{
						var startGlyphID = Utility.ttUSHORT(classDefTable + 2);
						var glyphCount = Utility.ttUSHORT(classDefTable + 4);
						var classDef1ValueArray = classDefTable + 6;
						if (glyph >= startGlyphID && glyph < startGlyphID + glyphCount)
							return Utility.ttUSHORT(classDef1ValueArray + 2 * (glyph - startGlyphID));
						break;
					}

				case 2:
					{
						var classRangeCount = Utility.ttUSHORT(classDefTable + 2);
						var classRangeRecords = classDefTable + 4;
						var l = 0;
						var r = classRangeCount - 1;
						var m = 0;
						var strawStart = 0;
						var strawEnd = 0;
						var needle = glyph;
						while (l <= r)
						{
							m = (l + r) >> 1;
							var classRangeRecord = classRangeRecords + 6 * m;
							strawStart = Utility.ttUSHORT(classRangeRecord);
							strawEnd = Utility.ttUSHORT(classRangeRecord + 2);
							if (needle < strawStart)
								r = m - 1;
							else if (needle > strawEnd)
								l = m + 1;
							else
								return Utility.ttUSHORT(classRangeRecord + 4);
						}

						break;
					}

				default:
					return -1;
			}

			return 0;
		}

		private static int stbtt__GetCoverageIndex(FakePtr<byte> coverageTable, int glyph)
		{
			var coverageFormat = Utility.ttUSHORT(coverageTable);
			switch (coverageFormat)
			{
				case 1:
					{
						var glyphCount = Utility.ttUSHORT(coverageTable + 2);
						var l = 0;
						var r = glyphCount - 1;
						var m = 0;
						var straw = 0;
						var needle = glyph;
						while (l <= r)
						{
							var glyphArray = coverageTable + 4;
							ushort glyphID = 0;
							m = (l + r) >> 1;
							glyphID = Utility.ttUSHORT(glyphArray + 2 * m);
							straw = glyphID;
							if (needle < straw)
								r = m - 1;
							else if (needle > straw)
								l = m + 1;
							else
								return m;
						}

						break;
					}

				case 2:
					{
						var rangeCount = Utility.ttUSHORT(coverageTable + 2);
						var rangeArray = coverageTable + 4;
						var l = 0;
						var r = rangeCount - 1;
						var m = 0;
						var strawStart = 0;
						var strawEnd = 0;
						var needle = glyph;
						while (l <= r)
						{
							m = (l + r) >> 1;
							var rangeRecord = rangeArray + 6 * m;
							strawStart = Utility.ttUSHORT(rangeRecord);
							strawEnd = Utility.ttUSHORT(rangeRecord + 2);
							if (needle < strawStart)
							{
								r = m - 1;
							}
							else if (needle > strawEnd)
							{
								l = m + 1;
							}
							else
							{
								var startCoverageIndex = Utility.ttUSHORT(rangeRecord + 4);
								return startCoverageIndex + glyph - strawStart;
							}
						}

						break;
					}

				default:
					return -1;
			}

			return -1;
		}

		private int stbtt__GetGlyphGPOSInfoAdvance(int glyph1, int glyph2)
		{
			ushort lookupListOffset = 0;
			ushort lookupCount = 0;
			var i = 0;
			var sti = 0;
			if (_gpos == 0)
				return 0;

			var data = new FakePtr<byte>(_data, _gpos);
			if (Utility.ttUSHORT(data + 0) != 1)
				return 0;
			if (Utility.ttUSHORT(data + 2) != 0)
				return 0;
			lookupListOffset = Utility.ttUSHORT(data + 8);
			var lookupList = data + lookupListOffset;
			lookupCount = Utility.ttUSHORT(lookupList);
			for (i = 0; i < lookupCount; ++i)
			{
				var lookupOffset = Utility.ttUSHORT(lookupList + 2 + 2 * i);
				var lookupTable = lookupList + lookupOffset;
				var lookupType = Utility.ttUSHORT(lookupTable);
				var subTableCount = Utility.ttUSHORT(lookupTable + 4);
				var subTableOffsets = lookupTable + 6;
				if (lookupType != 2)
					continue;
				for (sti = 0; sti < subTableCount; sti++)
				{
					var subtableOffset = Utility.ttUSHORT(subTableOffsets + 2 * sti);
					var table = lookupTable + subtableOffset;
					var posFormat = Utility.ttUSHORT(table);
					var coverageOffset = Utility.ttUSHORT(table + 2);
					var coverageIndex = stbtt__GetCoverageIndex(table + coverageOffset, glyph1);
					if (coverageIndex == -1)
						continue;
					switch (posFormat)
					{
						case 1:
							{
								var l = 0;
								var r = 0;
								var m = 0;
								var straw = 0;
								var needle = 0;
								var valueFormat1 = Utility.ttUSHORT(table + 4);
								var valueFormat2 = Utility.ttUSHORT(table + 6);
								if (valueFormat1 == 4 && valueFormat2 == 0)
								{
									var valueRecordPairSizeInBytes = 2;
									var pairSetCount = Utility.ttUSHORT(table + 8);
									var pairPosOffset = Utility.ttUSHORT(table + 10 + 2 * coverageIndex);
									var pairValueTable = table + pairPosOffset;
									var pairValueCount = Utility.ttUSHORT(pairValueTable);
									var pairValueArray = pairValueTable + 2;
									if (coverageIndex >= pairSetCount)
										return 0;
									needle = glyph2;
									r = pairValueCount - 1;
									l = 0;
									while (l <= r)
									{
										ushort secondGlyph = 0;
										m = (l + r) >> 1;
										var pairValue = pairValueArray + (2 + valueRecordPairSizeInBytes) * m;
										secondGlyph = Utility.ttUSHORT(pairValue);
										straw = secondGlyph;
										if (needle < straw)
										{
											r = m - 1;
										}
										else if (needle > straw)
										{
											l = m + 1;
										}
										else
										{
											var xAdvance = Utility.ttSHORT(pairValue + 2);
											return xAdvance;
										}
									}
								}
								else
								{
									return 0;
								}

								break;
							}

						case 2:
							{
								var valueFormat1 = Utility.ttUSHORT(table + 4);
								var valueFormat2 = Utility.ttUSHORT(table + 6);
								if (valueFormat1 == 4 && valueFormat2 == 0)
								{
									var classDef1Offset = Utility.ttUSHORT(table + 8);
									var classDef2Offset = Utility.ttUSHORT(table + 10);
									var glyph1class = stbtt__GetGlyphClass(table + classDef1Offset, glyph1);
									var glyph2class = stbtt__GetGlyphClass(table + classDef2Offset, glyph2);
									var class1Count = Utility.ttUSHORT(table + 12);
									var class2Count = Utility.ttUSHORT(table + 14);
									short xAdvance = 0;
									if (glyph1class < 0 || glyph1class >= class1Count)
										return 0;
									if (glyph2class < 0 || glyph2class >= class2Count)
										return 0;
									var class1Records = table + 16;
									var class2Records = class1Records + 2 * glyph1class * class2Count;
									xAdvance = Utility.ttSHORT(class2Records + 2 * glyph2class);
									return xAdvance;
								}

								return 0;
							}

						default:
							return 0;
					}
				}
			}

			return 0;
		}

		private int stbtt__GetGlyphInfoT2(int glyph_index, out int x0, out int y0, out int x1, out int y1)
		{
			var c = new CharStringContext
			{
				bounds = 1
			};

			var r = stbtt__run_charstring(glyph_index, c);
			x0 = r != 0 ? c.min_x : 0;
			y0 = r != 0 ? c.min_y : 0;
			x1 = r != 0 ? c.max_x : 0;
			y1 = r != 0 ? c.max_y : 0;

			return r != 0 ? c.num_vertices : 0;
		}

		private int stbtt__GetGlyphKernInfoAdvance(int glyph1, int glyph2)
		{
			var data = new FakePtr<byte>(_data, _kern);
			uint needle = 0;
			uint straw = 0;
			var l = 0;
			var r = 0;
			var m = 0;
			if (_kern == 0)
				return 0;
			if (Utility.ttUSHORT(data + 2) < 1)
				return 0;
			if (Utility.ttUSHORT(data + 8) != 1)
				return 0;
			l = 0;
			r = Utility.ttUSHORT(data + 10) - 1;
			needle = (uint)((glyph1 << 16) | glyph2);
			while (l <= r)
			{
				m = (l + r) >> 1;
				straw = Utility.ttULONG(data + 18 + m * 6);
				if (needle < straw)
					r = m - 1;
				else if (needle > straw)
					l = m + 1;
				else
					return Utility.ttSHORT(data + 22 + m * 6);
			}

			return 0;
		}

		private int stbtt__GetGlyphShapeT2(int glyph_index, out STTVertex[] pvertices)
		{
			var count_ctx = new CharStringContext
			{
				bounds = 1
			};

			if (stbtt__run_charstring(glyph_index, count_ctx) != 0)
			{
				var output_ctx = new CharStringContext();
				pvertices = new STTVertex[count_ctx.num_vertices];
				output_ctx.pvertices = pvertices;
				if (stbtt__run_charstring(glyph_index, output_ctx) != 0)
				{
					return output_ctx.num_vertices;
				}
			}

			pvertices = null;
			return 0;
		}

		private int stbtt__GetGlyphShapeTT(int glyph_index, out STTVertex[] pvertices)
		{
			short numberOfContours = 0;
			var data = new FakePtr<byte>(_data);
			var num_vertices = 0;
			var g = stbtt__GetGlyfOffset(glyph_index);
			pvertices = null;
			if (g < 0)
				return 0;

			STTVertex[] vertices = null;

			numberOfContours = Utility.ttSHORT(data + g);
			if (numberOfContours > 0)
			{
				byte flags = 0;
				byte flagcount = 0;
				var ins = 0;
				var i = 0;
				var j = 0;
				var m = 0;
				var n = 0;
				var next_move = 0;
				var was_off = 0;
				var off = 0;
				var start_off = 0;
				var x = 0;
				var y = 0;
				var cx = 0;
				var cy = 0;
				var sx = 0;
				var sy = 0;
				var scx = 0;
				var scy = 0;
				var endPtsOfContours = data + g + 10;
				ins = Utility.ttUSHORT(data + g + 10 + numberOfContours * 2);
				var points = data + g + 10 + numberOfContours * 2 + 2 + ins;
				n = 1 + Utility.ttUSHORT(endPtsOfContours + numberOfContours * 2 - 2);
				m = n + 2 * numberOfContours;
				vertices = new STTVertex[m];
				next_move = 0;
				flagcount = 0;
				off = m - n;
				for (i = 0; i < n; ++i)
				{
					if (flagcount == 0)
					{
						flags = points.Read();
						if ((flags & 8) != 0)
							flagcount = points.Read();
					}
					else
					{
						--flagcount;
					}

					vertices[off + i].Type = flags;
				}

				x = 0;
				for (i = 0; i < n; ++i)
				{
					flags = vertices[off + i].Type;
					if ((flags & 2) != 0)
					{
						short dx = points.Read();
						x += (flags & 16) != 0 ? dx : -dx;
					}
					else
					{
						if ((flags & 16) == 0)
						{
							x = x + (short)(points[0] * 256 + points[1]);
							points += 2;
						}
					}

					vertices[off + i].X = (short)x;
				}

				y = 0;
				for (i = 0; i < n; ++i)
				{
					flags = vertices[off + i].Type;
					if ((flags & 4) != 0)
					{
						short dy = points.Read();
						y += (flags & 32) != 0 ? dy : -dy;
					}
					else
					{
						if ((flags & 32) == 0)
						{
							y = y + (short)(points[0] * 256 + points[1]);
							points += 2;
						}
					}

					vertices[off + i].Y = (short)y;
				}

				num_vertices = 0;
				sx = sy = cx = cy = scx = scy = 0;
				for (i = 0; i < n; ++i)
				{
					flags = vertices[off + i].Type;
					x = vertices[off + i].X;
					y = vertices[off + i].Y;
					if (next_move == i)
					{
						if (i != 0)
							num_vertices = stbtt__close_shape(vertices, num_vertices, was_off, start_off, sx, sy, scx,
								scy, cx, cy);
						start_off = (flags & 1) != 0 ? 0 : 1;
						if (start_off != 0)
						{
							scx = x;
							scy = y;
							if ((vertices[off + i + 1].Type & 1) == 0)
							{
								sx = (x + vertices[off + i + 1].X) >> 1;
								sy = (y + vertices[off + i + 1].Y) >> 1;
							}
							else
							{
								sx = vertices[off + i + 1].X;
								sy = vertices[off + i + 1].Y;
								++i;
							}
						}
						else
						{
							sx = x;
							sy = y;
						}

						vertices[num_vertices++].Set(STBTT_vmove, sx, sy, 0, 0);
						was_off = 0;
						next_move = 1 + Utility.ttUSHORT(endPtsOfContours + j * 2);
						++j;
					}
					else
					{
						if ((flags & 1) == 0)
						{
							if (was_off != 0)
								vertices[num_vertices++].Set(STBTT_vcurve, (cx + x) >> 1, (cy + y) >> 1,
									cx, cy);
							cx = x;
							cy = y;
							was_off = 1;
						}
						else
						{
							if (was_off != 0)
								vertices[num_vertices++].Set(STBTT_vcurve, x, y, cx, cy);
							else
								vertices[num_vertices++].Set(STBTT_vline, x, y, 0, 0);
							was_off = 0;
						}
					}
				}

				num_vertices = stbtt__close_shape(vertices, num_vertices, was_off, start_off, sx, sy, scx, scy, cx, cy);
			}
			else if (numberOfContours < 0)
			{
				var more = 1;
				var comp = data + g + 10;
				num_vertices = 0;
				vertices = null;

				var mtx = new float[6];

				while (more != 0)
				{
					ushort flags = 0;
					ushort gidx = 0;
					var comp_num_verts = 0;
					var i = 0;
					STTVertex[] comp_verts = null;
					STTVertex[] tmp = null;

					mtx[0] = mtx[3] = 1;
					mtx[1] = mtx[2] = mtx[4] = mtx[5] = 0;
					float m = 0;
					float n = 0;
					flags = (ushort)Utility.ttSHORT(comp);
					comp += 2;
					gidx = (ushort)Utility.ttSHORT(comp);
					comp += 2;
					if ((flags & 2) != 0)
					{
						if ((flags & 1) != 0)
						{
							mtx[4] = Utility.ttSHORT(comp);
							comp += 2;
							mtx[5] = Utility.ttSHORT(comp);
							comp += 2;
						}
						else
						{
							mtx[4] = (sbyte)comp.FirstValue;
							comp += 1;
							mtx[5] = (sbyte)comp.FirstValue;
							comp += 1;
						}
					}

					if ((flags & (1 << 3)) != 0)
					{
						mtx[0] = mtx[3] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[1] = mtx[2] = 0;
					}
					else if ((flags & (1 << 6)) != 0)
					{
						mtx[0] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[1] = mtx[2] = 0;
						mtx[3] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
					}
					else if ((flags & (1 << 7)) != 0)
					{
						mtx[0] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[1] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[2] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
						mtx[3] = Utility.ttSHORT(comp) / 16384.0f;
						comp += 2;
					}

					m = (float)Math.Sqrt(mtx[0] * mtx[0] + mtx[1] * mtx[1]);
					n = (float)Math.Sqrt(mtx[2] * mtx[2] + mtx[3] * mtx[3]);
					comp_num_verts = stbtt_GetGlyphShape(gidx, out comp_verts);
					if (comp_num_verts > 0)
					{
						for (i = 0; i < comp_num_verts; ++i)
						{
							var v = comp_verts[i];
							short x = 0;
							short y = 0;
							x = v.X;
							y = v.Y;
							v.X = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
							v.Y = (short)(n * (mtx[1] * x + mtx[3] * y + mtx[5]));
							x = v.CX;
							y = v.CY;
							v.CX = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
							v.CY = (short)(n * (mtx[1] * x + mtx[3] * y + mtx[5]));

							comp_verts[i] = v;
						}

						tmp = new STTVertex[num_vertices + comp_num_verts];
						if (num_vertices > 0 && vertices != null)
						{
							Array.Copy(vertices, tmp, num_vertices);
						}

						Array.Copy(comp_verts, 0, tmp, num_vertices, comp_num_verts);
						vertices = tmp;
						num_vertices += comp_num_verts;
					}

					more = flags & (1 << 5);
				}
			}

			pvertices = vertices;
			return num_vertices;
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

		private FakePtr<byte> stbtt_FindSVGDoc(int gl)
		{
			var i = 0;
			var data = new FakePtr<byte>(_data);
			var svg_doc_list = data + stbtt__get_svg();
			int numEntries = Utility.ttUSHORT(svg_doc_list);
			var svg_docs = svg_doc_list + 2;
			for (i = 0; i < numEntries; i++)
			{
				var svg_doc = svg_docs + 12 * i;
				if (gl >= Utility.ttUSHORT(svg_doc) && gl <= Utility.ttUSHORT(svg_doc + 2))
					return svg_doc;
			}

			return FakePtr<byte>.Null;
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

		public int stbtt_GetCodepointBox(int codepoint, out int x0, out int y0, out int x1, out int y1)
		{
			return stbtt_GetGlyphBox(stbtt_FindGlyphIndex(codepoint), out x0, out y0, out x1, out y1);
		}

		public void stbtt_GetCodepointHMetrics(int codepoint, out int advanceWidth, out int leftSideBearing)
		{
			stbtt_GetGlyphHMetrics(stbtt_FindGlyphIndex(codepoint), out advanceWidth, out leftSideBearing);
		}

		public int stbtt_GetCodepointKernAdvance(int ch1, int ch2)
		{
			if (_kern == 0 && _gpos == 0)
				return 0;
			return stbtt_GetGlyphKernAdvance(stbtt_FindGlyphIndex(ch1), stbtt_FindGlyphIndex(ch2));
		}

		public byte[] stbtt_GetCodepointSDF(float scale, int codepoint, int padding,
			byte onedge_value, float pixel_dist_scale, out int width, out int height, out int xoff, out int yoff)
		{
			return stbtt_GetGlyphSDF(scale, stbtt_FindGlyphIndex(codepoint), padding, onedge_value,
				pixel_dist_scale, out width, out height, out xoff, out yoff);
		}

		public int stbtt_GetCodepointShape(int unicode_codepoint, out STTVertex[] vertices)
		{
			return stbtt_GetGlyphShape(stbtt_FindGlyphIndex(unicode_codepoint), out vertices);
		}

		public int stbtt_GetCodepointSVG(int unicode_codepoint, ref FakePtr<byte> svg)
		{
			return stbtt_GetGlyphSVG(stbtt_FindGlyphIndex(unicode_codepoint), ref svg);
		}

		public void stbtt_GetFontBoundingBox(out int x0, out int y0, out int x1, out int y1)
		{
			var dataPtr = new FakePtr<byte>(_data);
			x0 = Utility.ttSHORT(dataPtr + _head + 36);
			y0 = Utility.ttSHORT(dataPtr + _head + 38);
			x1 = Utility.ttSHORT(dataPtr + _head + 40);
			y1 = Utility.ttSHORT(dataPtr + _head + 42);
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

		public void stbtt_GetFontVMetrics(out int ascent, out int descent, out int lineGap)
		{
			var dataPtr = new FakePtr<byte>(_data);

			ascent = Utility.ttSHORT(dataPtr + _hhea + 4);
			descent = Utility.ttSHORT(dataPtr + _hhea + 6);
			lineGap = Utility.ttSHORT(dataPtr + _hhea + 8);
		}

		public int stbtt_GetFontVMetricsOS2(out int typoAscent, out int typoDescent, out int typoLineGap)
		{
			typoAscent = typoDescent = typoLineGap = 0;
			var dataPtr = new FakePtr<byte>(_data);
			var tab = (int)Utility.stbtt__find_table(dataPtr, (uint)_fontstart, "OS/2");
			if (tab == 0)
				return 0;

			typoAscent = Utility.ttSHORT(dataPtr + tab + 68);
			typoDescent = Utility.ttSHORT(dataPtr + tab + 70);
			typoLineGap = Utility.ttSHORT(dataPtr + tab + 72);

			return 1;
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

		public int stbtt_GetGlyphBox(int glyph_index, out int x0, out int y0, out int x1, out int y1)
		{
			x0 = y0 = x1 = y1 = 0;
			if (_cff.size != 0)
			{
				stbtt__GetGlyphInfoT2(glyph_index, out x0, out y0, out x1, out y1);
			}
			else
			{
				var g = stbtt__GetGlyfOffset(glyph_index);
				if (g < 0)
					return 0;

				var dataPtr = new FakePtr<byte>(_data);
				x0 = Utility.ttSHORT(dataPtr + g + 2);
				y0 = Utility.ttSHORT(dataPtr + g + 4);
				x1 = Utility.ttSHORT(dataPtr + g + 6);
				y1 = Utility.ttSHORT(dataPtr + g + 8);
			}

			return 1;
		}

		public void stbtt_GetGlyphHMetrics(int glyph_index, out int advanceWidth, out int leftSideBearing)
		{
			var dataPtr = new FakePtr<byte>(_data);
			var numOfLongHorMetrics = Utility.ttUSHORT(dataPtr + _hhea + 34);
			if (glyph_index < numOfLongHorMetrics)
			{
				advanceWidth = Utility.ttSHORT(dataPtr + _hmtx + 4 * glyph_index);
				leftSideBearing = Utility.ttSHORT(dataPtr + _hmtx + 4 * glyph_index + 2);
			}
			else
			{
				advanceWidth = Utility.ttSHORT(dataPtr + _hmtx + 4 * (numOfLongHorMetrics - 1));
				leftSideBearing = Utility.ttSHORT(dataPtr + _hmtx + 4 * numOfLongHorMetrics + 2 * (glyph_index - numOfLongHorMetrics));
			}
		}

		public int stbtt_GetGlyphKernAdvance(int g1, int g2)
		{
			var xAdvance = 0;
			if (_gpos != 0)
			{
				xAdvance += stbtt__GetGlyphGPOSInfoAdvance(g1, g2);
			}
			else if (_kern != 0)
			{
				xAdvance += stbtt__GetGlyphKernInfoAdvance(g1, g2);
			}
			return xAdvance;
		}

		public byte[] stbtt_GetGlyphSDF(float scale, int glyph, int padding, byte onedge_value, float pixel_dist_scale,
			out int width, out int height, out int xoff, out int yoff)
		{
			width = height = xoff = yoff = 0;
			byte[] data = null;

			var scale_x = scale;
			var scale_y = scale;
			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			var w = 0;
			var h = 0;
			if (scale == 0)
				return null;
			stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale, scale, 0.0f, 0.0f, out ix0, out iy0, out ix1, out iy1);
			if (ix0 == ix1 || iy0 == iy1)
				return null;
			ix0 -= padding;
			iy0 -= padding;
			ix1 += padding;
			iy1 += padding;
			w = ix1 - ix0;
			h = iy1 - iy0;

			width = w;
			height = h;
			xoff = ix0;
			yoff = iy0;
			scale_y = -scale_y;
			{
				var x = 0;
				var y = 0;
				var i = 0;
				var j = 0;
				STTVertex[] verts;
				var num_verts = stbtt_GetGlyphShape(glyph, out verts);
				data = new byte[w * h];
				var precompute = new float[num_verts];
				for (i = 0, j = num_verts - 1; i < num_verts; j = i++)
					if (verts[i].Type == STBTT_vline)
					{
						var x0 = verts[i].X * scale_x;
						var y0 = verts[i].Y * scale_y;
						var x1 = verts[j].X * scale_x;
						var y1 = verts[j].Y * scale_y;
						var dist = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
						precompute[i] = dist == 0 ? 0.0f : 1.0f / dist;
					}
					else if (verts[i].Type == STBTT_vcurve)
					{
						var x2 = verts[j].X * scale_x;
						var y2 = verts[j].Y * scale_y;
						var x1 = verts[i].CX * scale_x;
						var y1 = verts[i].CY * scale_y;
						var x0 = verts[i].X * scale_x;
						var y0 = verts[i].Y * scale_y;
						var bx = x0 - 2 * x1 + x2;
						var by = y0 - 2 * y1 + y2;
						var len2 = bx * bx + by * by;
						if (len2 != 0.0f)
							precompute[i] = 1.0f / (bx * bx + by * by);
						else
							precompute[i] = 0.0f;
					}
					else
					{
						precompute[i] = 0.0f;
					}

				var res = new float[3];
				for (y = iy0; y < iy1; ++y)
				{
					for (x = ix0; x < ix1; ++x)
					{
						float val = 0;
						var min_dist = 999999.0f;
						var sx = x + 0.5f;
						var sy = y + 0.5f;
						var x_gspace = sx / scale_x;
						var y_gspace = sy / scale_y;
						var winding = stbtt__compute_crossings_x(x_gspace, y_gspace, num_verts, verts);
						for (i = 0; i < num_verts; ++i)
						{
							var x0 = verts[i].X * scale_x;
							var y0 = verts[i].Y * scale_y;
							if (verts[i].Type == STBTT_vline && precompute[i] != 0.0f)
							{
								var x1 = verts[i - 1].X * scale_x;
								var y1 = verts[i - 1].Y * scale_y;
								float dist = 0;
								var dist2 = (x0 - sx) * (x0 - sx) + (y0 - sy) * (y0 - sy);
								if (dist2 < min_dist * min_dist)
									min_dist = (float)Math.Sqrt(dist2);
								dist = Math.Abs((x1 - x0) * (y0 - sy) - (y1 - y0) * (x0 - sx)) * precompute[i];
								if (dist < min_dist)
								{
									var dx = x1 - x0;
									var dy = y1 - y0;
									var px = x0 - sx;
									var py = y0 - sy;
									var t = -(px * dx + py * dy) / (dx * dx + dy * dy);
									if (t >= 0.0f && t <= 1.0f)
										min_dist = dist;
								}
							}
							else if (verts[i].Type == STBTT_vcurve)
							{
								var x2 = verts[i - 1].X * scale_x;
								var y2 = verts[i - 1].Y * scale_y;
								var x1 = verts[i].CX * scale_x;
								var y1 = verts[i].CY * scale_y;
								var box_x0 = (x0 < x1 ? x0 : x1) < x2 ? x0 < x1 ? x0 : x1 : x2;
								var box_y0 = (y0 < y1 ? y0 : y1) < y2 ? y0 < y1 ? y0 : y1 : y2;
								var box_x1 = (x0 < x1 ? x1 : x0) < x2 ? x2 : x0 < x1 ? x1 : x0;
								var box_y1 = (y0 < y1 ? y1 : y0) < y2 ? y2 : y0 < y1 ? y1 : y0;
								if (sx > box_x0 - min_dist && sx < box_x1 + min_dist && sy > box_y0 - min_dist &&
									sy < box_y1 + min_dist)
								{
									var num = 0;
									var ax = x1 - x0;
									var ay = y1 - y0;
									var bx = x0 - 2 * x1 + x2;
									var by = y0 - 2 * y1 + y2;
									var mx = x0 - sx;
									var my = y0 - sy;

									res.Set(0.0f);

									float px = 0;
									float py = 0;
									float t = 0;
									float it = 0;
									float dist2 = 0;
									var a_inv = precompute[i];
									if (a_inv == 0.0)
									{
										var a = 3 * (ax * bx + ay * by);
										var b = 2 * (ax * ax + ay * ay) + (mx * bx + my * by);
										var c = mx * ax + my * ay;
										if (a == 0.0)
										{
											if (b != 0.0) res[num++] = -c / b;
										}
										else
										{
											var discriminant = b * b - 4 * a * c;
											if (discriminant < 0)
											{
												num = 0;
											}
											else
											{
												var root = (float)Math.Sqrt(discriminant);
												res[0] = (-b - root) / (2 * a);
												res[1] = (-b + root) / (2 * a);
												num = 2;
											}
										}
									}
									else
									{
										var b = 3 * (ax * bx + ay * by) * a_inv;
										var c = (2 * (ax * ax + ay * ay) + (mx * bx + my * by)) * a_inv;
										var d = (mx * ax + my * ay) * a_inv;
										num = stbtt__solve_cubic(b, c, d, res);
									}

									dist2 = (x0 - sx) * (x0 - sx) + (y0 - sy) * (y0 - sy);
									if (dist2 < min_dist * min_dist)
										min_dist = (float)Math.Sqrt(dist2);
									if (num >= 1 && res[0] >= 0.0f && res[0] <= 1.0f)
									{
										t = res[0];
										it = 1.0f - t;
										px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
										py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
										dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
										if (dist2 < min_dist * min_dist)
											min_dist = (float)Math.Sqrt(dist2);
									}

									if (num >= 2 && res[1] >= 0.0f && res[1] <= 1.0f)
									{
										t = res[1];
										it = 1.0f - t;
										px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
										py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
										dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
										if (dist2 < min_dist * min_dist)
											min_dist = (float)Math.Sqrt(dist2);
									}

									if (num >= 3 && res[2] >= 0.0f && res[2] <= 1.0f)
									{
										t = res[2];
										it = 1.0f - t;
										px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
										py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
										dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
										if (dist2 < min_dist * min_dist)
											min_dist = (float)Math.Sqrt(dist2);
									}
								}
							}
						}

						if (winding == 0)
							min_dist = -min_dist;
						val = onedge_value + pixel_dist_scale * min_dist;
						if (val < 0)
							val = 0;
						else if (val > 255)
							val = 255;
						data[(y - iy0) * w + (x - ix0)] = (byte)val;
					}
				}
			}

			return data;
		}

		public int stbtt_GetGlyphShape(int glyph_index, out STTVertex[] pvertices)
		{
			if (_cff.size == 0)
			{
				return stbtt__GetGlyphShapeTT(glyph_index, out pvertices);
			}

			return stbtt__GetGlyphShapeT2(glyph_index, out pvertices);
		}

		public int stbtt_GetGlyphSVG(int gl, ref FakePtr<byte> svg)
		{
			var data = new FakePtr<byte>(_data);
			if (_svg == 0)
				return 0;
			var svg_doc = stbtt_FindSVGDoc(gl);
			if (!svg_doc.IsNull)
			{
				svg = data + _svg + Utility.ttULONG(svg_doc + 4);
				return (int)Utility.ttULONG(svg_doc + 8);
			}

			return 0;
		}

		public STTKerningEntry[] stbtt_GetKerningTable()
		{
			var data = new FakePtr<byte>(_data, _kern);
			var k = 0;
			var length = 0;
			if (_kern == 0)
				return null;
			if (Utility.ttUSHORT(data + 2) < 1)
				return null;
			if (Utility.ttUSHORT(data + 8) != 1)
				return null;
			length = Utility.ttUSHORT(data + 10);

			var table = new STTKerningEntry[length];
			for (k = 0; k < length; k++)
			{
				table[k].Glyph1 = Utility.ttUSHORT(data + 18 + k * 6);
				table[k].Glyph2 = Utility.ttUSHORT(data + 20 + k * 6);
				table[k].Advance = Utility.ttSHORT(data + 22 + k * 6);
			}

			return table;
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

		public int stbtt_IsGlyphEmpty(int glyph_index)
		{
			short numberOfContours = 0;
			var g = 0;
			if (_cff.size != 0)
			{
				int x0, y0, x1, y1;
				return stbtt__GetGlyphInfoT2(glyph_index, out x0, out y0, out x1, out y1) == 0 ? 1 : 0;
			}
			g = stbtt__GetGlyfOffset(glyph_index);
			if (g < 0)
				return 1;
			numberOfContours = Utility.ttSHORT(new FakePtr<byte>(_data, g));
			return numberOfContours == 0 ? 1 : 0;
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

		public void stbtt_GetScaledFontVMetrics(float size, out float ascent, out float descent, out float lineGap)
		{
			var i_ascent = 0;
			var i_descent = 0;
			var i_lineGap = 0;
			float scale = 0;
			scale = size > 0 ? stbtt_ScaleForPixelHeight(size) : stbtt_ScaleForMappingEmToPixels(-size);
			stbtt_GetFontVMetrics(out i_ascent, out i_descent, out i_lineGap);

			ascent = i_ascent * scale;
			descent = i_descent * scale;
			lineGap = i_lineGap * scale;
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

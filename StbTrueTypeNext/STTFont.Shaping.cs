using System;

namespace StbTrueTypeNext
{
	partial class STTFont
	{
		public int stbtt_GetGlyphShape(int glyph_index, out STTVertex[] pvertices)
		{
			if (_cff.size == 0)
			{
				return stbtt__GetGlyphShapeTT(glyph_index, out pvertices);
			}

			return stbtt__GetGlyphShapeT2(glyph_index, out pvertices);
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

						vertices[num_vertices++].Set(Constants.STBTT_vmove, sx, sy, 0, 0);
						was_off = 0;
						next_move = 1 + Utility.ttUSHORT(endPtsOfContours + j * 2);
						++j;
					}
					else
					{
						if ((flags & 1) == 0)
						{
							if (was_off != 0)
								vertices[num_vertices++].Set(Constants.STBTT_vcurve, (cx + x) >> 1, (cy + y) >> 1,
									cx, cy);
							cx = x;
							cy = y;
							was_off = 1;
						}
						else
						{
							if (was_off != 0)
								vertices[num_vertices++].Set(Constants.STBTT_vcurve, x, y, cx, cy);
							else
								vertices[num_vertices++].Set(Constants.STBTT_vline, x, y, 0, 0);
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

		private static int stbtt__close_shape(STTVertex[] vertices, int num_vertices, int was_off, int start_off, int sx, int sy, int scx, int scy, int cx, int cy)
		{
			if (start_off != 0)
			{
				if (was_off != 0)
					vertices[num_vertices++].Set(Constants.STBTT_vcurve, (cx + scx) >> 1, (cy + scy) >> 1, cx, cy);

				vertices[num_vertices++].Set(Constants.STBTT_vcurve, sx, sy, scx, scy);
			}
			else
			{
				if (was_off != 0)
					vertices[num_vertices++].Set(Constants.STBTT_vcurve, sx, sy, cx, cy);
				else
					vertices[num_vertices++].Set(Constants.STBTT_vline, sx, sy, 0, 0);
			}

			return num_vertices;
		}
	}
}

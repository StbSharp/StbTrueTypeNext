using System;

namespace StbTrueTypeNext
{
	partial class STBFont
	{
		/// <summary>Gets the bounding box of a glyph's bitmap, in pixels, relative to the glyph origin.</summary>
		/// <param name="glyph">Index of the glyph.</param>
		/// <param name="scale_x">Horizontal scale, as returned by <see cref="ScaleForPixelHeight" />.</param>
		/// <param name="scale_y">Vertical scale, as returned by <see cref="ScaleForPixelHeight" />.</param>
		/// <param name="shift_x">Horizontal subpixel shift, in pixels, applied before rounding.</param>
		/// <param name="shift_y">Vertical subpixel shift, in pixels, applied before rounding.</param>
		/// <returns>The pixel bounding box, or null if the glyph has no bounding box.</returns>
		public STBGlyphBitmapBox? GetGlyphBoxInPixels(int glyph, float scale_x, float scale_y, float shift_x = 0, float shift_y = 0)
		{
			var box = GetGlyphBoxInFontUnits(glyph);
			if (box == null)
			{
				return null;
			}

			var v = box.Value;

			return new STBGlyphBitmapBox
			{
				X0 = (int)Math.Floor(v.X0 * scale_x + shift_x),
				Y0 = (int)Math.Floor(-v.Y1 * scale_y + shift_y),
				X1 = (int)Math.Ceiling(v.X1 * scale_x + shift_x),
				Y1 = (int)Math.Ceiling(-v.Y0 * scale_y + shift_y)
			};
		}

		/// <summary>Gets the bounding box of a glyph's outline in font units, using either the TrueType or CFF loader.</summary>
		/// <param name="glyph_index">Index of the glyph.</param>
		/// <returns>The bounding box in font units, or null if the glyph has no outline data.</returns>
		public STBGlyphBitmapBox? GetGlyphBoxInFontUnits(int glyph_index)
		{
			int x0, y0, x1, y1;
			if (_cff.Size != 0)
			{
				GetGlyphInfoT2(glyph_index, out x0, out y0, out x1, out y1);
			}
			else
			{
				var g = GetGlyphOffset(glyph_index);
				if (g < 0)
				{
					return null;
				}

				var dataPtr = new FakePtr<byte>(_data);
				x0 = Utility.ttSHORT(dataPtr + g + 2);
				y0 = Utility.ttSHORT(dataPtr + g + 4);
				x1 = Utility.ttSHORT(dataPtr + g + 6);
				y1 = Utility.ttSHORT(dataPtr + g + 8);
			}

			return new STBGlyphBitmapBox(x0, y0, x1, y1);
		}

		/// <summary>Rasterizes a glyph into a newly allocated bitmap, with subpixel shifts.</summary>
		/// <param name="glyph">Index of the glyph to rasterize.</param>
		/// <param name="scale_x">Horizontal scale, as returned by <see cref="ScaleForPixelHeight" />.</param>
		/// <param name="scale_y">Vertical scale, as returned by <see cref="ScaleForPixelHeight" />.</param>
		/// <param name="shift_x">Horizontal subpixel shift, in pixels, applied before rasterizing.</param>
		/// <param name="shift_y">Vertical subpixel shift, in pixels, applied before rasterizing.</param>
		/// <returns>The rasterized bitmap, or null if the glyph is empty or has no area at the requested scale.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="scale_x" /> or <paramref name="scale_y" /> is zero or negative.</exception>
		public STBGlyphBitmap? GetGlyphBitmapSubpixel(int glyph, float scale_x, float scale_y, float shift_x = 0, float shift_y = 0)
		{
			if (scale_x <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(scale_x));
			}

			if (scale_y <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(scale_y));
			}

			var box = GetGlyphBoxInPixels(glyph, scale_x, scale_y, shift_x, shift_y);
			if (box == null)
			{
				return null;
			}

			var v = box.Value;
			if (v.Width <= 0 || v.Height <= 0)
			{
				return null;
			}

			STBVertex[] vertices;
			var num_verts = GetGlyphShape(glyph, out vertices);

			var gbm = new Bitmap(v.Width, v.Height);
			gbm.Rasterize(0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, v.X0, v.Y0, 1, RasterizerVersion);

			return new STBGlyphBitmap
			{
				Bitmap = gbm.Pixels.Data,
				Width = v.Width,
				Height = v.Height,
				XOffset = v.X0,
				YOffset = v.Y0
			};
		}

		/// <summary>
		/// Rasterizes a glyph into a caller-supplied buffer, starting at the given byte offset, with subpixel
		/// shifts and optional oversampling prefiltering. The bitmap size is the glyph's pixel bounding box.
		/// </summary>
		/// <param name="glyph">Index of the glyph to rasterize.</param>
		/// <param name="buffer">Destination buffer; must hold the glyph's bitmap rows from <paramref name="offset" />.</param>
		/// <param name="offset">Byte offset in <paramref name="buffer" /> at which the bitmap starts.</param>
		/// <param name="out_stride">Distance between rows of the destination bitmap, in bytes.</param>
		/// <param name="scale_x">Horizontal scale, as returned by <see cref="ScaleForPixelHeight" />.</param>
		/// <param name="scale_y">Vertical scale, as returned by <see cref="ScaleForPixelHeight" />.</param>
		/// <param name="shift_x">Horizontal subpixel shift, in pixels, applied before rasterizing.</param>
		/// <param name="shift_y">Vertical subpixel shift, in pixels, applied before rasterizing.</param>
		/// <param name="prefilter_x">Horizontal oversampling factor used before downscaling; values below 2 disable it.</param>
		/// <param name="prefilter_y">Vertical oversampling factor used before downscaling; values below 2 disable it.</param>
		/// <returns>
		/// Describes the rasterized bitmap in <paramref name="buffer" />, including the subpixel offsets introduced by
		/// prefiltering, or null if the glyph has no bounding box.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="out_stride" />, <paramref name="scale_x" /> or <paramref name="scale_y" /> is zero or negative.
		/// </exception>
		public STBGlyphBitmap? MakeGlyphBitmapSubpixel(int glyph, byte[] buffer, int offset, int out_stride, float scale_x, float scale_y,
			float shift_x = 0, float shift_y = 0, int prefilter_x = 0, int prefilter_y = 0)
		{
			if (out_stride <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(out_stride));
			}

			if (scale_x <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(scale_x));
			}

			if (scale_y <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(scale_y));
			}

			var box = GetGlyphBoxInPixels(glyph, scale_x, scale_y, shift_x, shift_y);
			if (box == null)
			{
				return null;
			}

			var boxV = box.Value;
			STBVertex[] vertices;
			var num_verts = GetGlyphShape(glyph, out vertices);

			var output = new FakePtr<byte>(buffer, offset);
			var gbm = new Bitmap(boxV.Width, boxV.Height, out_stride, output);

			var v = box.Value;
			gbm.Rasterize(0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, v.X0, v.Y0, 1, RasterizerVersion);

			if (prefilter_x > 1)
			{
				Utility.HorizontalPrefilter(output, boxV.Width, boxV.Height, out_stride, (uint)prefilter_x);
			}

			if (prefilter_y > 1)
			{
				Utility.VerticalPrefilter(output, boxV.Width, boxV.Height, out_stride, (uint)prefilter_y);
			}

			return new STBGlyphBitmap
			{
				Bitmap = buffer,
				BitmapOffset = offset,
				Width = v.Width,
				Height = v.Height,
				XOffset = v.X0,
				YOffset = v.Y0,
				XSubpixelOffset = Utility.OversampleShift(prefilter_x),
				YSubpixelOffset = Utility.OversampleShift(prefilter_y)
			};
		}

		/// <summary>Gets a glyph's outline as an array of vertices, using either the TrueType or CFF loader.</summary>
		/// <param name="glyph_index">Index of the glyph.</param>
		/// <param name="pvertices">Receives the outline vertices.</param>
		/// <returns>The number of vertices, or 0 if the glyph has no outline.</returns>
		public int GetGlyphShape(int glyph_index, out STBVertex[] pvertices)
		{
			if (_cff.Size == 0)
			{
				return GetGlyphShapeTT(glyph_index, out pvertices);
			}

			return GetGlyphShapeT2(glyph_index, out pvertices);
		}

		/// <summary>Checks whether a glyph has no outline.</summary>
		/// <param name="glyph_index">Index of the glyph.</param>
		/// <returns>True if the glyph is empty or the index is invalid; otherwise false.</returns>
		public bool IsGlyphEmpty(int glyph_index)
		{
			short numberOfContours = 0;
			var g = 0;
			if (_cff.Size != 0)
			{
				int x0, y0, x1, y1;
				return GetGlyphInfoT2(glyph_index, out x0, out y0, out x1, out y1) == 0;
			}
			g = GetGlyphOffset(glyph_index);
			if (g < 0)
				return true;
			numberOfContours = Utility.ttSHORT(new FakePtr<byte>(_data, g));
			return numberOfContours == 0;
		}

		private int GetGlyphShapeTT(int glyph_index, out STBVertex[] pvertices)
		{
			short numberOfContours = 0;
			var data = new FakePtr<byte>(_data);
			var num_vertices = 0;
			var g = GetGlyphOffset(glyph_index);
			pvertices = null;
			if (g < 0)
				return 0;

			STBVertex[] vertices = null;

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
				vertices = new STBVertex[m];
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
							num_vertices = CloseShape(vertices, num_vertices, was_off, start_off, sx, sy, scx,
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

				num_vertices = CloseShape(vertices, num_vertices, was_off, start_off, sx, sy, scx, scy, cx, cy);
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
					STBVertex[] comp_verts = null;
					STBVertex[] tmp = null;

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
					comp_num_verts = GetGlyphShape(gidx, out comp_verts);
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

						tmp = new STBVertex[num_vertices + comp_num_verts];
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

		private int GetGlyphShapeT2(int glyph_index, out STBVertex[] pvertices)
		{
			var count_ctx = new CharStringContext();
			if (RunCharString(glyph_index, count_ctx) != 0)
			{
				pvertices = count_ctx.ToArray();
				return pvertices.Length;
			}

			pvertices = null;
			return 0;
		}

		private static int CloseShape(STBVertex[] vertices, int num_vertices, int was_off, int start_off, int sx, int sy, int scx, int scy, int cx, int cy)
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

		private int GetGlyphInfoT2(int glyph_index, out int x0, out int y0, out int x1, out int y1)
		{
			var c = new CharStringContext
			{
				Mode = CharStringContextMode.CalculateBounds
			};

			var r = RunCharString(glyph_index, c);
			x0 = r != 0 ? c.MinX : 0;
			y0 = r != 0 ? c.MinY : 0;
			x1 = r != 0 ? c.MaxX : 0;
			y1 = r != 0 ? c.MaxY : 0;

			return r != 0 ? c.VerticesCount : 0;
		}

		private Buffer GetCffCidSubrsForGlyph(int glyph_index)
		{
			var fdselect = _fdselect;
			var nranges = 0;
			var start = 0;
			var end = 0;
			var v = 0;
			var fmt = 0;
			var fdselector = -1;
			var i = 0;
			fdselect.Seek(0);
			fmt = fdselect.ReadByte();
			if (fmt == 0)
			{
				fdselect.Skip(glyph_index);
				fdselector = fdselect.ReadByte();
			}
			else if (fmt == 3)
			{
				nranges = (int)fdselect.ReadUint(2);
				start = (int)fdselect.ReadUint(2);
				for (i = 0; i < nranges; i++)
				{
					v = fdselect.ReadByte();
					end = (int)fdselect.ReadUint(2);
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
			return Buffer.ReadCffSubrs(_cff, Buffer.ReadCffIndexBy(_fontdicts, fdselector));
		}

		private int RunCharString(int glyph_index, CharStringContext c)
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
			b = Buffer.ReadCffIndexBy(_charstrings, glyph_index);
			while (b.Cursor < b.Size)
			{
				i = 0;
				clear_stack = 1;
				b0 = b.ReadByte();
				switch (b0)
				{
					case 0x13:
					case 0x14:
						if (in_header != 0)
							maskbits += sp / 2;
						in_header = 0;
						b.Skip((maskbits + 7) / 8);
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
						c.RelativeMoveTo(s[sp - 2], s[sp - 1]);
						break;
					case 0x04:
						in_header = 0;
						if (sp < 1)
							return 0;
						c.RelativeMoveTo(0, s[sp - 1]);
						break;
					case 0x16:
						in_header = 0;
						if (sp < 1)
							return 0;
						c.RelativeMoveTo(s[sp - 1], 0);
						break;
					case 0x05:
						if (sp < 2)
							return 0;
						for (; i + 1 < sp; i += 2) c.RelativeLineTo(s[i], s[i + 1]);

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
								c.RelativeLineTo(s[i], 0);
								i++;
							}

							goto_vlineto = 0;
							if (i >= sp)
								break;
							c.RelativeLineTo(0, s[i]);
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
								c.RelativeCubicCurveTo(0, s[i], s[i + 1], s[i + 2], s[i + 3],
									sp - i == 5 ? s[i + 4] : 0.0f);
								i += 4;
							}

							goto_hvcurveto = 0;
							if (i + 3 >= sp)
								break;
							c.RelativeCubicCurveTo(s[i], 0, s[i + 1], s[i + 2], sp - i == 5 ? s[i + 4] : 0.0f,
								s[i + 3]);
							i += 4;
						}

						break;
					case 0x08:
						if (sp < 6)
							return 0;
						for (; i + 5 < sp; i += 6)
							c.RelativeCubicCurveTo(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);

						break;
					case 0x18:
						if (sp < 8)
							return 0;
						for (; i + 5 < sp - 2; i += 6)
							c.RelativeCubicCurveTo(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);

						if (i + 1 >= sp)
							return 0;
						c.RelativeLineTo(s[i], s[i + 1]);
						break;
					case 0x19:
						if (sp < 8)
							return 0;
						for (; i + 1 < sp - 6; i += 2) c.RelativeLineTo(s[i], s[i + 1]);

						if (i + 5 >= sp)
							return 0;
						c.RelativeCubicCurveTo(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
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
								c.RelativeCubicCurveTo(s[i], f, s[i + 1], s[i + 2], s[i + 3], (float)0.0);
							else
								c.RelativeCubicCurveTo(f, s[i], s[i + 1], s[i + 2], (float)0.0, s[i + 3]);
							f = (float)0.0;
						}

						break;
					case 0x0A:
					case 0x1D:
						if (b0 == 0x0A && has_subrs == 0)
						{
							if (_fdselect.Size != 0)
								subrs = GetCffCidSubrsForGlyph(glyph_index);
							has_subrs = 1;
						}

						if (sp < 1)
							return 0;
						v = (int)s[--sp];
						if (subr_stack_height >= 10)
							return 0;
						subr_stack[subr_stack_height++] = b;
						b = Buffer.ReadCffSubrBy(b0 == 0x0A ? subrs : _gsubrs, v);
						if (b.Size == 0)
							return 0;
						b.Cursor = 0;
						clear_stack = 0;
						break;
					case 0x0B:
						if (subr_stack_height <= 0)
							return 0;
						b = subr_stack[--subr_stack_height];
						clear_stack = 0;
						break;
					case 0x0E:
						c.CloseShape();
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
							int b1 = b.ReadByte();
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
									c.RelativeCubicCurveTo(dx1, 0, dx2, dy2, dx3, 0);
									c.RelativeCubicCurveTo(dx4, 0, dx5, -dy2, dx6, 0);
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
									c.RelativeCubicCurveTo(dx1, dy1, dx2, dy2, dx3, dy3);
									c.RelativeCubicCurveTo(dx4, dy4, dx5, dy5, dx6, dy6);
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
									c.RelativeCubicCurveTo(dx1, dy1, dx2, dy2, dx3, 0);
									c.RelativeCubicCurveTo(dx4, 0, dx5, dy5, dx6, -(dy1 + dy2 + dy5));
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
									c.RelativeCubicCurveTo(dx1, dy1, dx2, dy2, dx3, dy3);
									c.RelativeCubicCurveTo(dx4, dy4, dx5, dy5, dx6, dy6);
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
							f = (float)(int)b.ReadUint(4) / 0x10000;
						}
						else
						{
							b.Skip(-1);
							f = (short)b.ReadCffUint();
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

		private int GetGlyphOffset(int glyph_index)
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
	}
}

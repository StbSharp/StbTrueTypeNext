using static StbTrueTypeNext.StbTrueType;

namespace StbTrueTypeNext
{
	public class stbtt_pack_context
	{
		private uint _h_oversample;
		private uint _v_oversample;
		private int _width;
		private int _height;
		private stbrp_node[] _nodes;
		private stbrp_context _pack_info;
		private int _padding;
		private byte[] _pixels;
		private int _skip_missing;
		private int _stride_in_bytes;

		public void stbtt_PackBegin(byte[] _pixels, int pw, int ph, int _stride_in_bytes, int _padding)
		{
			var context = new stbrp_context();
			var num_nodes = pw - _padding;

			_width = pw;
			_height = ph;
			this._pixels = _pixels;
			_pack_info = context;
			_nodes = new stbrp_node[num_nodes];
			this._padding = _padding;
			this._stride_in_bytes = _stride_in_bytes != 0 ? _stride_in_bytes : pw;
			_h_oversample = 1;
			_v_oversample = 1;
			_skip_missing = 0;
			context.stbrp_init_target(pw - _padding, ph - _padding);

			_pixels.Set((byte)0, pw * ph);
		}

		public void stbtt_PackEnd()
		{
		}

		public int stbtt_PackFontRange(byte[] fontdata, int font_index, float font_size,
			int first_unicode_codepoint_in_range, int num_chars_in_range, stbtt_packedchar[] chardata_for_range)
		{
			var range = new stbtt_pack_range
			{
				first_unicode_codepoint_in_range = first_unicode_codepoint_in_range,
				array_of_unicode_codepoints = null,
				num_chars = num_chars_in_range,
				chardata_for_range = chardata_for_range,
				font_size = font_size
			};

			var ranges = new stbtt_pack_range[] { range };

			return stbtt_PackFontRanges(fontdata, font_index, ranges);
		}

		public int stbtt_PackFontRanges(byte[] fontdata, int font_index, stbtt_pack_range[] ranges)
		{
			var info = new stbtt_fontinfo();
			var i = 0;
			var j = 0;
			var n = 0;
			var return_value = 1;

			var num_ranges = ranges.Length;
			for (i = 0; i < num_ranges; ++i)
			{
				for (j = 0; j < ranges[i].num_chars; ++j)
				{
					ranges[i].chardata_for_range[j].x0 = ranges[i].chardata_for_range[j].y0 =
						ranges[i].chardata_for_range[j].x1 = ranges[i].chardata_for_range[j].y1 = 0;
				}
			}

			n = 0;
			for (i = 0; i < num_ranges; ++i)
			{
				n += ranges[i].num_chars;
			}

			var rects = new stbrp_rect[n];

			stbtt_InitFont(info, fontdata, stbtt_GetFontOffsetForIndex(fontdata, font_index));
			n = stbtt_PackFontRangesGatherRects(this, info, ranges, rects);
			stbtt_PackFontRangesPackRects(this, rects, n);
			return_value = stbtt_PackFontRangesRenderIntoRects(this, info, ranges, rects);

			return return_value;
		}

		private static int stbtt_PackFontRangesGatherRects(stbtt_pack_context spc, stbtt_fontinfo info,
			stbtt_pack_range[] ranges, stbrp_rect[] rects)
		{
			var i = 0;
			var j = 0;
			var k = 0;
			var missing_glyph_added = 0;
			k = 0;

			var num_ranges = ranges.Length;
			for (i = 0; i < num_ranges; ++i)
			{
				var fh = ranges[i].font_size;
				var scale = fh > 0 ? stbtt_ScaleForPixelHeight(info, fh) : stbtt_ScaleForMappingEmToPixels(info, -fh);
				ranges[i].h_oversample = (byte)spc._h_oversample;
				ranges[i].v_oversample = (byte)spc._v_oversample;
				for (j = 0; j < ranges[i].num_chars; ++j)
				{
					var x0 = 0;
					var y0 = 0;
					var x1 = 0;
					var y1 = 0;
					var codepoint = ranges[i].array_of_unicode_codepoints == null
						? ranges[i].first_unicode_codepoint_in_range + j
						: ranges[i].array_of_unicode_codepoints[j];
					var glyph = stbtt_FindGlyphIndex(info, codepoint);
					if (glyph == 0 && (spc._skip_missing != 0 || missing_glyph_added != 0))
					{
						rects[k].w = rects[k].h = 0;
					}
					else
					{
						stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale * spc._h_oversample, scale * spc._v_oversample,
							0, 0, out x0, out y0, out x1, out y1);
						rects[k].w = (int)(x1 - x0 + spc._padding + spc._h_oversample - 1);
						rects[k].h = (int)(y1 - y0 + spc._padding + spc._v_oversample - 1);
						if (glyph == 0)
							missing_glyph_added = 1;
					}

					++k;
				}
			}

			return k;
		}

		private static void stbtt_PackFontRangesPackRects(stbtt_pack_context spc, stbrp_rect[] rects, int num_rects) => spc._pack_info.stbrp_pack_rects(rects, num_rects);

		private static int stbtt_PackFontRangesRenderIntoRects(stbtt_pack_context spc, stbtt_fontinfo info,
			stbtt_pack_range[] ranges, stbrp_rect[] rects)
		{
			var i = 0;
			var j = 0;
			var k = 0;
			var missing_glyph = -1;
			var return_value = 1;
			var old_h_over = (int)spc._h_oversample;
			var old_v_over = (int)spc._v_oversample;
			k = 0;

			var num_ranges = ranges.Length;
			for (i = 0; i < num_ranges; ++i)
			{
				var fh = ranges[i].font_size;
				var scale = fh > 0 ? stbtt_ScaleForPixelHeight(info, fh) : stbtt_ScaleForMappingEmToPixels(info, -fh);
				float recip_h = 0;
				float recip_v = 0;
				float sub_x = 0;
				float sub_y = 0;
				spc._h_oversample = ranges[i].h_oversample;
				spc._v_oversample = ranges[i].v_oversample;
				recip_h = 1.0f / spc._h_oversample;
				recip_v = 1.0f / spc._v_oversample;
				sub_x = stbtt__oversample_shift((int)spc._h_oversample);
				sub_y = stbtt__oversample_shift((int)spc._v_oversample);
				for (j = 0; j < ranges[i].num_chars; ++j)
				{
					var r = rects[k];
					if (r.was_packed != 0 && r.w != 0 && r.h != 0)
					{
						var advance = 0;
						var lsb = 0;
						var x0 = 0;
						var y0 = 0;
						var x1 = 0;
						var y1 = 0;
						var codepoint = ranges[i].array_of_unicode_codepoints == null
							? ranges[i].first_unicode_codepoint_in_range + j
							: ranges[i].array_of_unicode_codepoints[j];
						var glyph = stbtt_FindGlyphIndex(info, codepoint);
						var pad = spc._padding;
						r.x += pad;
						r.y += pad;
						r.w -= pad;
						r.h -= pad;
						stbtt_GetGlyphHMetrics(info, glyph, out advance, out lsb);
						stbtt_GetGlyphBitmapBox(info, glyph, scale * spc._h_oversample, scale * spc._v_oversample,
							out x0, out y0, out x1, out y1);
						stbtt_MakeGlyphBitmapSubpixel(info, new FakePtr<byte>(spc._pixels, r.x + r.y * spc._stride_in_bytes),
							(int)(r.w - spc._h_oversample + 1), (int)(r.h - spc._v_oversample + 1),
							spc._stride_in_bytes, scale * spc._h_oversample, scale * spc._v_oversample, 0, 0, glyph);
						if (spc._h_oversample > 1)
							stbtt__h_prefilter(new FakePtr<byte>(spc._pixels, r.x + r.y * spc._stride_in_bytes), r.w, r.h,
								spc._stride_in_bytes, spc._h_oversample);
						if (spc._v_oversample > 1)
							stbtt__v_prefilter(new FakePtr<byte>(spc._pixels, r.x + r.y * spc._stride_in_bytes), r.w, r.h,
								spc._stride_in_bytes, spc._v_oversample);

						var bc = ranges[i].chardata_for_range[j];
						bc.x0 = (ushort)(short)r.x;
						bc.y0 = (ushort)(short)r.y;
						bc.x1 = (ushort)(short)(r.x + r.w);
						bc.y1 = (ushort)(short)(r.y + r.h);
						bc.xadvance = scale * advance;
						bc.xoff = x0 * recip_h + sub_x;
						bc.yoff = y0 * recip_v + sub_y;
						bc.xoff2 = (x0 + r.w) * recip_h + sub_x;
						bc.yoff2 = (y0 + r.h) * recip_v + sub_y;
						ranges[i].chardata_for_range[j] = bc;
						if (glyph == 0)
							missing_glyph = j;
					}
					else if (spc._skip_missing != 0)
					{
						return_value = 0;
					}
					else if (r.was_packed != 0 && r.w == 0 && r.h == 0 && missing_glyph >= 0)
					{
						ranges[i].chardata_for_range[j] = ranges[i].chardata_for_range[missing_glyph];
					}
					else
					{
						return_value = 0;
					}

					rects[k] = r;

					++k;
				}
			}

			spc._h_oversample = (uint)old_h_over;
			spc._v_oversample = (uint)old_v_over;
			return return_value;
		}

		public static void stbtt_PackSetOversampling(stbtt_pack_context spc, uint h_oversample, uint v_oversample)
		{
			if (h_oversample <= 8)
				spc._h_oversample = h_oversample;
			if (v_oversample <= 8)
				spc._v_oversample = v_oversample;
		}

		public static void stbtt_PackSetSkipMissingCodepoints(stbtt_pack_context spc, int skip)
		{
			spc._skip_missing = skip;
		}

		private class stbrp_context
		{
			public int width;
			public int height;
			public int x;
			public int y;
			public int bottom_y;

			public void stbrp_init_target(int pw, int ph)
			{
				width = pw;
				height = ph;
				x = 0;
				y = 0;
				bottom_y = 0;
			}

			public void stbrp_pack_rects(stbrp_rect[] rects, int num_rects)
			{
				var i = 0;
				for (i = 0; i < num_rects; ++i)
				{
					if (x + rects[i].w > width)
					{
						x = 0;
						y = bottom_y;
					}

					if (y + rects[i].h > height)
						break;
					rects[i].x = x;
					rects[i].y = y;
					rects[i].was_packed = 1;
					x += rects[i].w;
					if (y + rects[i].h > bottom_y)
						bottom_y = y + rects[i].h;
				}

				for (; i < num_rects; ++i) rects[i].was_packed = 0;
			}
		}

		private struct stbrp_node
		{
			public byte x;
		}

		private struct stbrp_rect
		{
			public int x;
			public int y;
			public int id;
			public int w;
			public int h;
			public int was_packed;
		}
	}
}

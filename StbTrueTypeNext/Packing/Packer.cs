using static StbTrueTypeNext.stbtt_fontinfo;

namespace StbTrueTypeNext.Packing
{
	public class Packer
	{
		private uint _h_oversample;
		private uint _v_oversample;
		private RectPackContext _pack_info;
		private int _padding;
		private byte[] _pixels;
		private int _stride_in_bytes;

		public bool SkipMissing { get; set; }

		public int Width { get; private set; }
		public int Height { get; private set; }

		public void stbtt_PackBegin(byte[] pixels, int width, int height, int stride_in_bytes, int padding)
		{
			var context = new RectPackContext();
			var num_nodes = width - padding;

			Width = width;
			Height = height;
			_pixels = pixels;
			_pack_info = context;
			_padding = padding;
			_stride_in_bytes = stride_in_bytes != 0 ? stride_in_bytes : width;
			_h_oversample = 1;
			_v_oversample = 1;
			SkipMissing = false;
			context.stbrp_init_target(width - padding, height - padding);

			pixels.Set((byte)0, width * height);
		}

		public void stbtt_PackEnd()
		{
		}

		public int stbtt_PackFontRange(byte[] fontdata, int font_index, float font_size,
			int first_unicode_codepoint_in_range, int num_chars_in_range, PackedChar[] chardata_for_range)
		{
			var range = new PackRange
			{
				first_unicode_codepoint_in_range = first_unicode_codepoint_in_range,
				array_of_unicode_codepoints = null,
				num_chars = num_chars_in_range,
				chardata_for_range = chardata_for_range,
				font_size = font_size
			};

			var ranges = new PackRange[] { range };

			return stbtt_PackFontRanges(fontdata, font_index, ranges);
		}

		public int stbtt_PackFontRanges(byte[] fontdata, int font_index, PackRange[] ranges)
		{
			var i = 0;
			var j = 0;
			var n = 0;

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

			var rects = new PackRect[n];

			var info = stbtt_fontinfo.FromMemory(fontdata, stbtt_GetFontOffsetForIndex(fontdata, font_index));
			n = stbtt_PackFontRangesGatherRects(info, ranges, rects);
			stbtt_PackFontRangesPackRects(rects, n);

			return stbtt_PackFontRangesRenderIntoRects(info, ranges, rects);
		}

		private int stbtt_PackFontRangesGatherRects(stbtt_fontinfo info, PackRange[] ranges, PackRect[] rects)
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
				ranges[i].h_oversample = (byte)_h_oversample;
				ranges[i].v_oversample = (byte)_v_oversample;
				for (j = 0; j < ranges[i].num_chars; ++j)
				{
					var x0 = 0;
					var y0 = 0;
					var x1 = 0;
					var y1 = 0;
					var codepoint = ranges[i].array_of_unicode_codepoints == null
						? ranges[i].first_unicode_codepoint_in_range + j
						: ranges[i].array_of_unicode_codepoints[j];
					var glyph = info.stbtt_FindGlyphIndex(codepoint);
					if (glyph == 0 && (SkipMissing || missing_glyph_added != 0))
					{
						rects[k].w = rects[k].h = 0;
					}
					else
					{
						stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale * _h_oversample, scale * _v_oversample,
							0, 0, out x0, out y0, out x1, out y1);
						rects[k].w = (int)(x1 - x0 + _padding + _h_oversample - 1);
						rects[k].h = (int)(y1 - y0 + _padding + _v_oversample - 1);
						if (glyph == 0)
						{
							missing_glyph_added = 1;
						}
					}

					++k;
				}
			}

			return k;
		}

		private void stbtt_PackFontRangesPackRects(PackRect[] rects, int num_rects) => _pack_info.stbrp_pack_rects(rects, num_rects);

		private int stbtt_PackFontRangesRenderIntoRects(stbtt_fontinfo info, PackRange[] ranges, PackRect[] rects)
		{
			var i = 0;
			var j = 0;
			var k = 0;
			var missing_glyph = -1;
			var return_value = 1;
			var old_h_over = (int)_h_oversample;
			var old_v_over = (int)_v_oversample;
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
				_h_oversample = ranges[i].h_oversample;
				_v_oversample = ranges[i].v_oversample;
				recip_h = 1.0f / _h_oversample;
				recip_v = 1.0f / _v_oversample;
				sub_x = stbtt__oversample_shift((int)_h_oversample);
				sub_y = stbtt__oversample_shift((int)_v_oversample);
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
						var glyph = info.stbtt_FindGlyphIndex(codepoint);
						var pad = _padding;
						r.x += pad;
						r.y += pad;
						r.w -= pad;
						r.h -= pad;
						stbtt_GetGlyphHMetrics(info, glyph, out advance, out lsb);
						stbtt_GetGlyphBitmapBox(info, glyph, scale * _h_oversample, scale * _v_oversample,
							out x0, out y0, out x1, out y1);
						stbtt_MakeGlyphBitmapSubpixel(info, new FakePtr<byte>(_pixels, r.x + r.y * _stride_in_bytes),
							(int)(r.w - _h_oversample + 1), (int)(r.h - _v_oversample + 1),
							_stride_in_bytes, scale * _h_oversample, scale * _v_oversample, 0, 0, glyph);
						if (_h_oversample > 1)
							stbtt__h_prefilter(new FakePtr<byte>(_pixels, r.x + r.y * _stride_in_bytes), r.w, r.h,
								_stride_in_bytes, _h_oversample);
						if (_v_oversample > 1)
							stbtt__v_prefilter(new FakePtr<byte>(_pixels, r.x + r.y * _stride_in_bytes), r.w, r.h,
								_stride_in_bytes, _v_oversample);

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
					else if (SkipMissing)
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

			_h_oversample = (uint)old_h_over;
			_v_oversample = (uint)old_v_over;
			return return_value;
		}

		public void stbtt_PackSetOversampling(uint h_oversample, uint v_oversample)
		{
			if (h_oversample <= 8)
			{
				_h_oversample = h_oversample;
			}

			if (v_oversample <= 8)
			{
				_v_oversample = v_oversample;
			}
		}
	}
}

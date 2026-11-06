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

		public void Begin(byte[] pixels, int width, int height, int stride_in_bytes, int padding)
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
			context.InitTarget(width - padding, height - padding);

			pixels.Set((byte)0, width * height);
		}

		public void End()
		{
		}

		public int PackFontRange(STBFont font, float font_size,
			int first_unicode_codepoint_in_range, int num_chars_in_range, PackedChar[] chardata_for_range)
		{
			var range = new PackRange
			{
				FirstUnicodeCodepointInRange = first_unicode_codepoint_in_range,
				ArrayOfUnicodeCodepoints = null,
				NumChars = num_chars_in_range,
				ChardataForRange = chardata_for_range,
				FontSize = font_size
			};

			var ranges = new PackRange[] { range };

			return PackFontRanges(font, ranges);
		}

		public int PackFontRanges(STBFont font, PackRange[] ranges)
		{
			var i = 0;
			var j = 0;
			var n = 0;

			var num_ranges = ranges.Length;
			for (i = 0; i < num_ranges; ++i)
			{
				for (j = 0; j < ranges[i].NumChars; ++j)
				{
					ranges[i].ChardataForRange[j].X0 = ranges[i].ChardataForRange[j].Y0 =
						ranges[i].ChardataForRange[j].X1 = ranges[i].ChardataForRange[j].Y1 = 0;
				}
			}

			n = 0;
			for (i = 0; i < num_ranges; ++i)
			{
				n += ranges[i].NumChars;
			}

			var rects = new PackRect[n];

			n = PackFontRangesGatherRects(font, ranges, rects);
			PackFontRangesPackRects(rects, n);

			return PackFontRangesRenderIntoRects(font, ranges, rects);
		}

		private int PackFontRangesGatherRects(STBFont info, PackRange[] ranges, PackRect[] rects)
		{
			var i = 0;
			var j = 0;
			var k = 0;
			var missing_glyph_added = 0;
			k = 0;

			var num_ranges = ranges.Length;
			for (i = 0; i < num_ranges; ++i)
			{
				var fh = ranges[i].FontSize;
				var scale = fh > 0 ? info.stbtt_ScaleForPixelHeight(fh) : info.stbtt_ScaleForMappingEmToPixels(-fh);
				ranges[i].HOversample = (byte)_h_oversample;
				ranges[i].VOversample = (byte)_v_oversample;
				for (j = 0; j < ranges[i].NumChars; ++j)
				{
					var x0 = 0;
					var y0 = 0;
					var x1 = 0;
					var y1 = 0;
					var codepoint = ranges[i].ArrayOfUnicodeCodepoints == null
						? ranges[i].FirstUnicodeCodepointInRange + j
						: ranges[i].ArrayOfUnicodeCodepoints[j];
					var glyph = info.stbtt_FindGlyphIndex(codepoint);
					if (glyph == 0 && (SkipMissing || missing_glyph_added != 0))
					{
						rects[k].W = rects[k].H = 0;
					}
					else
					{
						info.GetGlyphBitmapBoxSubpixel(glyph, scale * _h_oversample, scale * _v_oversample,
							0, 0, out x0, out y0, out x1, out y1);
						rects[k].W = (int)(x1 - x0 + _padding + _h_oversample - 1);
						rects[k].H = (int)(y1 - y0 + _padding + _v_oversample - 1);
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

		private void PackFontRangesPackRects(PackRect[] rects, int num_rects) => _pack_info.PackRects(rects, num_rects);

		private int PackFontRangesRenderIntoRects(STBFont info, PackRange[] ranges, PackRect[] rects)
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
				var fh = ranges[i].FontSize;
				var scale = fh > 0 ? info.stbtt_ScaleForPixelHeight(fh) : info.stbtt_ScaleForMappingEmToPixels(-fh);
				float recip_h = 0;
				float recip_v = 0;
				float sub_x = 0;
				float sub_y = 0;
				_h_oversample = ranges[i].HOversample;
				_v_oversample = ranges[i].VOversample;
				recip_h = 1.0f / _h_oversample;
				recip_v = 1.0f / _v_oversample;
				sub_x = Utility.stbtt__oversample_shift((int)_h_oversample);
				sub_y = Utility.stbtt__oversample_shift((int)_v_oversample);
				for (j = 0; j < ranges[i].NumChars; ++j)
				{
					var r = rects[k];
					if (r.WasPacked != 0 && r.W != 0 && r.H != 0)
					{
						var advance = 0;
						var lsb = 0;
						var x0 = 0;
						var y0 = 0;
						var x1 = 0;
						var y1 = 0;
						var codepoint = ranges[i].ArrayOfUnicodeCodepoints == null
							? ranges[i].FirstUnicodeCodepointInRange + j
							: ranges[i].ArrayOfUnicodeCodepoints[j];
						var glyph = info.stbtt_FindGlyphIndex(codepoint);
						var pad = _padding;
						r.X += pad;
						r.Y += pad;
						r.W -= pad;
						r.H -= pad;
						info.stbtt_GetGlyphHMetrics(glyph, out advance, out lsb);
						info.GetGlyphBitmapBox(glyph, scale * _h_oversample, scale * _v_oversample,
							out x0, out y0, out x1, out y1);
						info.MakeGlyphBitmapSubpixel(new FakePtr<byte>(_pixels, r.X + r.Y * _stride_in_bytes),
							(int)(r.W - _h_oversample + 1), (int)(r.H - _v_oversample + 1),
							_stride_in_bytes, scale * _h_oversample, scale * _v_oversample, 0, 0, glyph);
						if (_h_oversample > 1)
							Utility.stbtt__h_prefilter(new FakePtr<byte>(_pixels, r.X + r.Y * _stride_in_bytes), r.W, r.H,
								_stride_in_bytes, _h_oversample);
						if (_v_oversample > 1)
							Utility.stbtt__v_prefilter(new FakePtr<byte>(_pixels, r.X + r.Y * _stride_in_bytes), r.W, r.H,
								_stride_in_bytes, _v_oversample);

						var bc = ranges[i].ChardataForRange[j];
						bc.X0 = (ushort)(short)r.X;
						bc.Y0 = (ushort)(short)r.Y;
						bc.X1 = (ushort)(short)(r.X + r.W);
						bc.Y1 = (ushort)(short)(r.Y + r.H);
						bc.XAdvance = scale * advance;
						bc.XOff = x0 * recip_h + sub_x;
						bc.YOff = y0 * recip_v + sub_y;
						bc.XOff2 = (x0 + r.W) * recip_h + sub_x;
						bc.YOff2 = (y0 + r.H) * recip_v + sub_y;
						ranges[i].ChardataForRange[j] = bc;
						if (glyph == 0)
							missing_glyph = j;
					}
					else if (SkipMissing)
					{
						return_value = 0;
					}
					else if (r.WasPacked != 0 && r.W == 0 && r.H == 0 && missing_glyph >= 0)
					{
						ranges[i].ChardataForRange[j] = ranges[i].ChardataForRange[missing_glyph];
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

		public void PackSetOversampling(uint h_oversample, uint v_oversample)
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

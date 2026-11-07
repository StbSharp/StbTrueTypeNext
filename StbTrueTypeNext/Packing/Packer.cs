namespace StbTrueTypeNext.Packing
{
	/// <summary>
	/// Packs glyphs from a font into a single-channel (8-bit) font atlas bitmap and reports
	/// each glyph's atlas rectangle and drawing metrics as <see cref="PackedChar" /> data.
	/// </summary>
	public class Packer
	{
		private uint _h_oversample;
		private uint _v_oversample;
		private RectPackContext _pack_info;
		private int _padding;
		private byte[] _pixels;
		private int _stride_in_bytes;

		/// <summary>
		/// Gets or sets a value indicating whether glyphs missing from the font are skipped
		/// instead of being replaced with the font's missing-glyph glyph.
		/// </summary>
		public bool SkipMissing { get; set; }

		/// <summary>Gets the width of the atlas bitmap, in pixels.</summary>
		public int Width { get; private set; }

		/// <summary>Gets the height of the atlas bitmap, in pixels.</summary>
		public int Height { get; private set; }

		/// <summary>
		/// Starts a packing session on the given atlas bitmap, clearing it and initializing the
		/// packer's state. Call <see cref="End" /> when packing is complete.
		/// </summary>
		/// <param name="pixels">Destination atlas bitmap; must hold at least <paramref name="width" /> * <paramref name="height" /> bytes.</param>
		/// <param name="width">Width of the atlas bitmap, in pixels.</param>
		/// <param name="height">Height of the atlas bitmap, in pixels.</param>
		/// <param name="stride_in_bytes">Distance between rows of the atlas, in bytes; pass 0 to use <paramref name="width" />.</param>
		/// <param name="padding">Padding, in pixels, left around each packed glyph.</param>
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

		/// <summary>Finishes the packing session started by <see cref="Begin" />.</summary>
		public void End()
		{
		}

		/// <summary>
		/// Packs a contiguous range of Unicode codepoints at a single font size into the atlas and
		/// fills the supplied packed character data for the range.
		/// </summary>
		/// <param name="font">Font whose glyphs are packed.</param>
		/// <param name="font_size">Font size in pixels; when negative, its absolute value is treated as a mapping from em to pixels.</param>
		/// <param name="first_unicode_codepoint_in_range">Unicode codepoint of the first character in the range.</param>
		/// <param name="num_chars_in_range">Number of consecutive codepoints in the range.</param>
		/// <param name="chardata_for_range">Array that receives the packed quad data, one entry per character.</param>
		/// <returns>True if every glyph was packed; false if any glyph could not be packed.</returns>
		public bool PackFontRange(STBFont font, float font_size,
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

		/// <summary>
		/// Packs one or more character ranges into the atlas and fills each range's packed
		/// character data.
		/// </summary>
		/// <param name="font">Font whose glyphs are packed.</param>
		/// <param name="ranges">Ranges to pack; each range's character data array receives its results.</param>
		/// <returns>True if every glyph was packed; false if any glyph could not be packed.</returns>
		public bool PackFontRanges(STBFont font, PackRange[] ranges)
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
				var scale = fh > 0 ? info.ScaleForPixelHeight(fh) : info.ScaleForMappingEmToPixels(-fh);
				ranges[i].HOversample = (byte)_h_oversample;
				ranges[i].VOversample = (byte)_v_oversample;
				for (j = 0; j < ranges[i].NumChars; ++j)
				{
					var codepoint = ranges[i].ArrayOfUnicodeCodepoints == null
						? ranges[i].FirstUnicodeCodepointInRange + j
						: ranges[i].ArrayOfUnicodeCodepoints[j];
					var glyph = info.FindGlyphIndex(codepoint);
					if (glyph == 0 && (SkipMissing || missing_glyph_added != 0))
					{
						rects[k].W = rects[k].H = 0;
					}
					else
					{
						var box = info.GetGlyphBoxInPixels(glyph, scale * _h_oversample, scale * _v_oversample) ?? STBGlyphBitmapBox.Zero;
						rects[k].W = (int)(box.Width + _padding + _h_oversample - 1);
						rects[k].H = (int)(box.Height + _padding + _v_oversample - 1);
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

		private bool PackFontRangesRenderIntoRects(STBFont info, PackRange[] ranges, PackRect[] rects)
		{
			var i = 0;
			var j = 0;
			var k = 0;
			var missing_glyph = -1;
			var return_value = true;
			var old_h_over = (int)_h_oversample;
			var old_v_over = (int)_v_oversample;
			k = 0;

			var num_ranges = ranges.Length;
			for (i = 0; i < num_ranges; ++i)
			{
				var fh = ranges[i].FontSize;
				var scale = fh > 0 ? info.ScaleForPixelHeight(fh) : info.ScaleForMappingEmToPixels(-fh);
				float recip_h = 0;
				float recip_v = 0;
				float sub_x = 0;
				float sub_y = 0;
				_h_oversample = ranges[i].HOversample;
				_v_oversample = ranges[i].VOversample;
				recip_h = 1.0f / _h_oversample;
				recip_v = 1.0f / _v_oversample;
				sub_x = Utility.OversampleShift((int)_h_oversample);
				sub_y = Utility.OversampleShift((int)_v_oversample);
				for (j = 0; j < ranges[i].NumChars; ++j)
				{
					var r = rects[k];
					if (r.WasPacked != 0 && r.W != 0 && r.H != 0)
					{
						var advance = 0;
						var lsb = 0;
						var codepoint = ranges[i].ArrayOfUnicodeCodepoints == null
							? ranges[i].FirstUnicodeCodepointInRange + j
							: ranges[i].ArrayOfUnicodeCodepoints[j];
						var glyph = info.FindGlyphIndex(codepoint);
						var pad = _padding;
						r.X += pad;
						r.Y += pad;
						r.W -= pad;
						r.H -= pad;
						info.GetGlyphHMetrics(glyph, out advance, out lsb);
						var box = info.GetGlyphBoxInPixels(glyph, scale * _h_oversample, scale * _v_oversample) ?? STBGlyphBitmapBox.Zero;
						info.MakeGlyphBitmapSubpixel(new FakePtr<byte>(_pixels, r.X + r.Y * _stride_in_bytes),
							(int)(r.W - _h_oversample + 1), (int)(r.H - _v_oversample + 1),
							_stride_in_bytes, scale * _h_oversample, scale * _v_oversample, 0, 0, glyph);
						if (_h_oversample > 1)
							Utility.HorizontalPrefilter(new FakePtr<byte>(_pixels, r.X + r.Y * _stride_in_bytes), r.W, r.H,
								_stride_in_bytes, _h_oversample);
						if (_v_oversample > 1)
							Utility.VerticalPrefilter(new FakePtr<byte>(_pixels, r.X + r.Y * _stride_in_bytes), r.W, r.H,
								_stride_in_bytes, _v_oversample);

						var bc = ranges[i].ChardataForRange[j];
						bc.X0 = (ushort)(short)r.X;
						bc.Y0 = (ushort)(short)r.Y;
						bc.X1 = (ushort)(short)(r.X + r.W);
						bc.Y1 = (ushort)(short)(r.Y + r.H);
						bc.XAdvance = scale * advance;
						bc.XOff = box.X0 * recip_h + sub_x;
						bc.YOff = box.Y0 * recip_v + sub_y;
						bc.XOff2 = (box.X0 + r.W) * recip_h + sub_x;
						bc.YOff2 = (box.Y0 + r.H) * recip_v + sub_y;
						ranges[i].ChardataForRange[j] = bc;
						if (glyph == 0)
							missing_glyph = j;
					}
					else if (SkipMissing)
					{
						return_value = false;
					}
					else if (r.WasPacked != 0 && r.W == 0 && r.H == 0 && missing_glyph >= 0)
					{
						ranges[i].ChardataForRange[j] = ranges[i].ChardataForRange[missing_glyph];
					}
					else
					{
						return_value = false;
					}

					rects[k] = r;

					++k;
				}
			}

			_h_oversample = (uint)old_h_over;
			_v_oversample = (uint)old_v_over;
			return return_value;
		}

		/// <summary>Sets the horizontal and vertical oversampling used when rasterizing glyphs.</summary>
		/// <param name="h_oversample">Horizontal oversampling factor; values greater than 8 are ignored.</param>
		/// <param name="v_oversample">Vertical oversampling factor; values greater than 8 are ignored.</param>
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

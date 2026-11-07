namespace StbTrueTypeNext
{
	partial class STBFont
	{
		/// <summary>Gets the horizontal metrics, in font units, of the glyph for a Unicode codepoint.</summary>
		/// <param name="codepoint">Unicode codepoint of the glyph.</param>
		/// <param name="advanceWidth">Receives the advance width of the glyph.</param>
		/// <param name="leftSideBearing">Receives the left side bearing of the glyph.</param>
		public void GetCodepointHMetrics(int codepoint, out int advanceWidth, out int leftSideBearing)
		{
			GetGlyphHMetrics(FindGlyphIndex(codepoint), out advanceWidth, out leftSideBearing);
		}

		/// <summary>Gets the font's bounding box, in font units.</summary>
		/// <param name="x0">Receives the left edge of the box.</param>
		/// <param name="y0">Receives the bottom edge of the box.</param>
		/// <param name="x1">Receives the right edge of the box.</param>
		/// <param name="y1">Receives the top edge of the box.</param>
		public void GetFontBoundingBox(out int x0, out int y0, out int x1, out int y1)
		{
			var dataPtr = new FakePtr<byte>(_data);
			x0 = Utility.ttSHORT(dataPtr + _head + 36);
			y0 = Utility.ttSHORT(dataPtr + _head + 38);
			x1 = Utility.ttSHORT(dataPtr + _head + 40);
			y1 = Utility.ttSHORT(dataPtr + _head + 42);
		}

		/// <summary>Gets the font's vertical metrics, in font units: ascent, descent, and line gap.</summary>
		/// <param name="ascent">Receives the ascent above the baseline; typically positive.</param>
		/// <param name="descent">Receives the descent below the baseline; typically negative.</param>
		/// <param name="lineGap">Receives the gap between consecutive lines.</param>
		public void GetFontVMetrics(out int ascent, out int descent, out int lineGap)
		{
			var dataPtr = new FakePtr<byte>(_data);

			ascent = Utility.ttSHORT(dataPtr + _hhea + 4);
			descent = Utility.ttSHORT(dataPtr + _hhea + 6);
			lineGap = Utility.ttSHORT(dataPtr + _hhea + 8);
		}

		/// <summary>Gets the typographic vertical metrics from the font's 'OS/2' table, in font units.</summary>
		/// <param name="typoAscent">Receives the typographic ascent above the baseline.</param>
		/// <param name="typoDescent">Receives the typographic descent below the baseline; typically negative.</param>
		/// <param name="typoLineGap">Receives the typographic line gap.</param>
		/// <returns>1 on success; 0 if the font has no 'OS/2' table.</returns>
		public int GetFontVMetricsOS2(out int typoAscent, out int typoDescent, out int typoLineGap)
		{
			typoAscent = typoDescent = typoLineGap = 0;
			var dataPtr = new FakePtr<byte>(_data);
			var tab = (int)Utility.FindTable(dataPtr, (uint)_fontstart, "OS/2");
			if (tab == 0)
				return 0;

			typoAscent = Utility.ttSHORT(dataPtr + tab + 68);
			typoDescent = Utility.ttSHORT(dataPtr + tab + 70);
			typoLineGap = Utility.ttSHORT(dataPtr + tab + 72);

			return 1;
		}

		/// <summary>Gets the horizontal metrics, in font units, of a glyph.</summary>
		/// <param name="glyph_index">Index of the glyph.</param>
		/// <param name="advanceWidth">Receives the advance width of the glyph.</param>
		/// <param name="leftSideBearing">Receives the left side bearing of the glyph.</param>
		public void GetGlyphHMetrics(int glyph_index, out int advanceWidth, out int leftSideBearing)
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

		/// <summary>Gets the font's vertical metrics scaled to a given font size, in pixels.</summary>
		/// <param name="size">Font size in pixels; when negative, its absolute value is treated as an em-to-pixel mapping.</param>
		/// <param name="ascent">Receives the scaled ascent above the baseline.</param>
		/// <param name="descent">Receives the scaled descent below the baseline; typically negative.</param>
		/// <param name="lineGap">Receives the scaled gap between consecutive lines.</param>
		public void GetScaledFontVMetrics(float size, out float ascent, out float descent, out float lineGap)
		{
			var i_ascent = 0;
			var i_descent = 0;
			var i_lineGap = 0;
			float scale = 0;
			scale = size > 0 ? ScaleForPixelHeight(size) : ScaleForMappingEmToPixels(-size);
			GetFontVMetrics(out i_ascent, out i_descent, out i_lineGap);

			ascent = i_ascent * scale;
			descent = i_descent * scale;
			lineGap = i_lineGap * scale;
		}
	}
}

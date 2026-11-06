namespace StbTrueTypeNext
{
	partial class STBFont
	{
		public void stbtt_GetCodepointHMetrics(int codepoint, out int advanceWidth, out int leftSideBearing)
		{
			stbtt_GetGlyphHMetrics(stbtt_FindGlyphIndex(codepoint), out advanceWidth, out leftSideBearing);
		}

		public void stbtt_GetFontBoundingBox(out int x0, out int y0, out int x1, out int y1)
		{
			var dataPtr = new FakePtr<byte>(_data);
			x0 = Utility.ttSHORT(dataPtr + _head + 36);
			y0 = Utility.ttSHORT(dataPtr + _head + 38);
			x1 = Utility.ttSHORT(dataPtr + _head + 40);
			y1 = Utility.ttSHORT(dataPtr + _head + 42);
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
	}
}

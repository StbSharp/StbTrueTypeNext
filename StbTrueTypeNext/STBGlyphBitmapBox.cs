namespace StbTrueTypeNext
{
	/// <summary>Integer bounding box of a glyph bitmap, in pixels, relative to the glyph origin.</summary>
	public struct STBGlyphBitmapBox
	{
		/// <summary>An empty box whose bounds are all zero.</summary>
		public static readonly STBGlyphBitmapBox Zero = new STBGlyphBitmapBox(0, 0, 0, 0);

		/// <summary>Left edge of the box, relative to the glyph origin.</summary>
		public int X0;

		/// <summary>Top edge of the box, relative to the glyph origin.</summary>
		public int Y0;

		/// <summary>Right edge of the box, relative to the glyph origin.</summary>
		public int X1;

		/// <summary>Bottom edge of the box, relative to the glyph origin.</summary>
		public int Y1;

		/// <summary>Width of the box, in pixels.</summary>
		public int Width => X1 - X0;

		/// <summary>Height of the box, in pixels.</summary>
		public int Height => Y1 - Y0;

		/// <summary>Creates a bitmap box from its edges.</summary>
		/// <param name="x0">Left edge of the box, relative to the glyph origin.</param>
		/// <param name="y0">Top edge of the box, relative to the glyph origin.</param>
		/// <param name="x1">Right edge of the box, relative to the glyph origin.</param>
		/// <param name="y1">Bottom edge of the box, relative to the glyph origin.</param>
		public STBGlyphBitmapBox(int x0, int y0, int x1, int y1)
		{
			X0 = x0;
			Y0 = y0;
			X1 = x1;
			Y1 = y1;
		}
	}
}

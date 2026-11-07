namespace StbTrueTypeNext
{
	public struct STBGlyphBitmapBox
	{
		public static readonly STBGlyphBitmapBox Zero = new STBGlyphBitmapBox(0, 0, 0, 0);

		public int X0;
		public int Y0;
		public int X1;
		public int Y1;

		public int Width => X1 - X0;
		public int Height => Y1 - Y0;

		public STBGlyphBitmapBox(int x0, int y0, int x1, int y1)
		{
			X0 = x0;
			Y0 = y0;
			X1 = x1;
			Y1 = y1;
		}
	}
}

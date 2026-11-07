namespace StbTrueTypeNext
{
	/// <summary>Describes a single kerning pair read from a font's legacy 'kern' table.</summary>
	public struct STBKerningEntry
	{
		/// <summary>Index of the first glyph of the pair.</summary>
		public int Glyph1;

		/// <summary>Index of the second glyph of the pair.</summary>
		public int Glyph2;

		/// <summary>Kerning adjustment to apply between the two glyphs, in font units.</summary>
		public int Advance;
	}
}

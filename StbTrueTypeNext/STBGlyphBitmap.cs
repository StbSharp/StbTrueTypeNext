namespace StbTrueTypeNext
{
	/// <summary>Describes a rasterized glyph bitmap produced by <see cref="STBFont" />.</summary>
	public class STBGlyphBitmap
	{
		/// <summary>Buffer holding the rasterized pixels; the glyph bitmap starts at <see cref="BitmapOffset" />.</summary>
		public byte[] Bitmap;

		/// <summary>Byte offset in <see cref="Bitmap" /> at which the rasterized glyph starts.</summary>
		public int BitmapOffset;

		/// <summary>Horizontal offset of the bitmap from the glyph origin, in pixels.</summary>
		public int XOffset;

		/// <summary>Vertical offset of the bitmap from the glyph origin, in pixels.</summary>
		public int YOffset;

		/// <summary>Width of the bitmap, in pixels.</summary>
		public int Width;

		/// <summary>Height of the bitmap, in pixels.</summary>
		public int Height;

		/// <summary>Horizontal subpixel offset introduced by oversampling and prefiltering, in pixels.</summary>
		public float XSubpixelOffset;

		/// <summary>Vertical subpixel offset introduced by oversampling and prefiltering, in pixels.</summary>
		public float YSubpixelOffset;
	}
}

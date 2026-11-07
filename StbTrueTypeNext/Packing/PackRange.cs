namespace StbTrueTypeNext.Packing
{
	/// <summary>
	/// Describes a group of characters to pack into a font atlas together: either a contiguous
	/// range of Unicode codepoints or an explicit list, at a single font size.
	/// </summary>
	public class PackRange
	{
		/// <summary>Font size in pixels used to pack the range; when negative, its absolute value is treated as an em-to-pixel mapping.</summary>
		public float FontSize;

		/// <summary>Unicode codepoint of the first character in the range; used when <see cref="ArrayOfUnicodeCodepoints" /> is null.</summary>
		public int FirstUnicodeCodepointInRange;

		/// <summary>Explicit codepoints to pack; when null, codepoints are taken consecutively from <see cref="FirstUnicodeCodepointInRange" />.</summary>
		public int[] ArrayOfUnicodeCodepoints;

		/// <summary>Number of characters to pack from this range.</summary>
		public int NumChars;

		/// <summary>Packed glyph data for the range; receives one <see cref="PackedChar" /> per character when the range is packed.</summary>
		public PackedChar[] ChardataForRange;

		/// <summary>Horizontal oversampling factor the packer used while gathering this range.</summary>
		public byte HOversample;

		/// <summary>Vertical oversampling factor the packer used while gathering this range.</summary>
		public byte VOversample;
	}
}

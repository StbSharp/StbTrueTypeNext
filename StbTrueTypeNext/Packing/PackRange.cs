namespace StbTrueTypeNext.Packing
{
	public class PackRange
	{
		public float FontSize;
		public int FirstUnicodeCodepointInRange;
		public int[] ArrayOfUnicodeCodepoints;
		public int NumChars;
		public PackedChar[] ChardataForRange;
		public byte HOversample;
		public byte VOversample;
	}
}

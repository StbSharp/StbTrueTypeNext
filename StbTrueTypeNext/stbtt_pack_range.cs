namespace StbTrueTypeNext
{
	public class stbtt_pack_range
	{
		public float font_size;
		public int first_unicode_codepoint_in_range;
		public int[] array_of_unicode_codepoints;
		public int num_chars;
		public stbtt_packedchar[] chardata_for_range;
		public byte h_oversample;
		public byte v_oversample;
	}
}

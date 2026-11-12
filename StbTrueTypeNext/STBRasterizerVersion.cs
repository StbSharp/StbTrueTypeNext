namespace StbTrueTypeNext
{
	/// <summary>Selects which rasterizer implementation is used when generating glyph bitmaps.</summary>
	public enum STBRasterizerVersion
	{
		/// <summary>Uses the first rasterizer version.</summary>
		V1,

		/// <summary>Uses the second rasterizer version, which is the default when generating glyph bitmaps.</summary>
		V2
	}
}

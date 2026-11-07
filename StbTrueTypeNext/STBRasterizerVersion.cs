namespace StbTrueTypeNext
{
	/// <summary>Selects which rasterizer implementation is used when generating glyph bitmaps.</summary>
	public enum STBRasterizerVersion
	{
		/// <summary>Uses the first rasterizer version.</summary>
		V1,

		/// <summary>Uses the second rasterizer version; this is the default, as set by <see cref="STBFont.RasterizerVersion" />.</summary>
		V2
	}
}

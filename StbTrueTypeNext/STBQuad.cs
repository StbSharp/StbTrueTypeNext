namespace StbTrueTypeNext
{
	/// <summary>
	/// Describes the textured quad of a single glyph: four screen-space corners and the matching
	/// texture coordinates.
	/// </summary>
	public struct STBQuad
	{
		/// <summary>X coordinate of the first corner in screen space, in pixels.</summary>
		public float X0;

		/// <summary>Y coordinate of the first corner in screen space, in pixels.</summary>
		public float Y0;

		/// <summary>Horizontal texture coordinate matching the first corner, in the range 0-1.</summary>
		public float S0;

		/// <summary>Vertical texture coordinate matching the first corner, in the range 0-1.</summary>
		public float T0;

		/// <summary>X coordinate of the opposite corner in screen space, in pixels.</summary>
		public float X1;

		/// <summary>Y coordinate of the opposite corner in screen space, in pixels.</summary>
		public float Y1;

		/// <summary>Horizontal texture coordinate matching the opposite corner, in the range 0-1.</summary>
		public float S1;

		/// <summary>Vertical texture coordinate matching the opposite corner, in the range 0-1.</summary>
		public float T1;
	}
}

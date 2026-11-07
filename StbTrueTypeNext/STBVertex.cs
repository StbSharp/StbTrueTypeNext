namespace StbTrueTypeNext
{
	/// <summary>
	/// Describes one point of a glyph outline as returned by STBFont.GetGlyphShape: the end point of a
	/// line, or the control and end points of a curve, together with the vertex type.
	/// </summary>
	public struct STBVertex
	{
		/// <summary>X coordinate of the vertex's end point, in font units.</summary>
		public short X;

		/// <summary>Y coordinate of the vertex's end point, in font units.</summary>
		public short Y;

		/// <summary>X coordinate of the curve control point, in font units.</summary>
		public short CX;

		/// <summary>Y coordinate of the curve control point, in font units.</summary>
		public short CY;

		/// <summary>X coordinate of the second control point, for outline formats that carry two.</summary>
		public short CX1;

		/// <summary>Y coordinate of the second control point, for outline formats that carry two.</summary>
		public short CY1;

		/// <summary>Vertex type marker, such as the move, line, or curve constants used by the outline loaders.</summary>
		public byte Type;

		/// <summary>Unused padding byte kept for layout compatibility with the native vertex structure.</summary>
		public byte Padding;

		/// <summary>Sets the vertex type and coordinates, narrowing the coordinates to 16-bit values.</summary>
		/// <param name="type">Vertex type marker, such as the move, line, or curve constant.</param>
		/// <param name="x">X coordinate of the end point, in font units.</param>
		/// <param name="y">Y coordinate of the end point, in font units.</param>
		/// <param name="cx">X coordinate of the control point, in font units.</param>
		/// <param name="cy">Y coordinate of the control point, in font units.</param>
		public void Set(byte type, int x, int y, int cx, int cy)
		{
			Type = type;
			X = (short)x;
			Y = (short)y;
			CX = (short)cx;
			CY = (short)cy;
		}
	}
}

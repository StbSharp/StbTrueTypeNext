using System;

namespace StbTrueTypeNext.Packing
{
	/// <summary>
	/// Describes a single glyph packed into a font atlas bitmap, including its atlas rectangle
	/// and the offsets and advance needed to draw it.
	/// </summary>
	public struct PackedChar
	{
		/// <summary>X coordinate of the left edge of the glyph within the atlas bitmap, in pixels.</summary>
		public ushort X0;

		/// <summary>Y coordinate of the top edge of the glyph within the atlas bitmap, in pixels.</summary>
		public ushort Y0;

		/// <summary>X coordinate of the right edge of the glyph within the atlas bitmap, in pixels.</summary>
		public ushort X1;

		/// <summary>Y coordinate of the bottom edge of the glyph within the atlas bitmap, in pixels.</summary>
		public ushort Y1;

		/// <summary>Horizontal offset from the drawing position to the glyph, in pixels.</summary>
		public float XOff;

		/// <summary>Vertical offset from the drawing position to the glyph, in pixels.</summary>
		public float YOff;

		/// <summary>Horizontal advance to apply to the drawing position after the glyph, in pixels.</summary>
		public float XAdvance;

		/// <summary>Horizontal offset of the far corner of the glyph relative to the drawing position, in pixels.</summary>
		public float XOff2;

		/// <summary>Vertical offset of the far corner of the glyph relative to the drawing position, in pixels.</summary>
		public float YOff2;

		/// <summary>
		/// Computes the screen-space quad for this glyph at the given position, fills <paramref name="q" /> with it,
		/// and advances <paramref name="xpos" /> by <see cref="XAdvance" />.
		/// </summary>
		/// <param name="pw">Width of the atlas bitmap, used to normalize the texture coordinates.</param>
		/// <param name="ph">Height of the atlas bitmap, used to normalize the texture coordinates.</param>
		/// <param name="xpos">Current horizontal drawing position; advanced by the glyph's advance.</param>
		/// <param name="ypos">Current vertical drawing position.</param>
		/// <param name="q">The quad that receives the computed screen and texture coordinates.</param>
		/// <param name="align_to_integer">True to snap the quad position to whole pixels; false to keep subpixel positions.</param>
		public void GetPackedQuad(int pw, int ph, ref float xpos, ref float ypos, ref STBQuad q, bool align_to_integer)
		{
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			if (align_to_integer)
			{
				float x = (int)Math.Floor(xpos + XOff + 0.5f);
				float y = (int)Math.Floor(ypos + YOff + 0.5f);
				q.X0 = x;
				q.Y0 = y;
				q.X1 = x + XOff2 - XOff;
				q.Y1 = y + YOff2 - YOff;
			}
			else
			{
				q.X0 = xpos + XOff;
				q.Y0 = ypos + YOff;
				q.X1 = xpos + XOff2;
				q.Y1 = ypos + YOff2;
			}

			q.S0 = X0 * ipw;
			q.T0 = Y0 * iph;
			q.S1 = X1 * ipw;
			q.T1 = Y1 * iph;
			xpos += XAdvance;
		}
	}
}

using System;

namespace StbTrueTypeNext
{
	/// <summary>
	/// Describes a glyph baked into a font atlas by STBFont.BakeFontBitmap: its rectangle in the
	/// atlas plus the offsets and advance needed to draw it.
	/// </summary>
	public struct STBBakedChar
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

		/// <summary>
		/// Computes the screen-space quad for this glyph at the given position, fills <paramref name="q" /> with it,
		/// and advances <paramref name="xpos" /> by <see cref="XAdvance" />. Positions are rounded to whole pixels.
		/// </summary>
		/// <param name="pw">Width of the atlas bitmap, used to normalize the texture coordinates.</param>
		/// <param name="ph">Height of the atlas bitmap, used to normalize the texture coordinates.</param>
		/// <param name="xpos">Current horizontal drawing position; advanced by the glyph's advance.</param>
		/// <param name="ypos">Current vertical drawing position.</param>
		/// <param name="q">The quad that receives the computed screen and texture coordinates.</param>
		/// <param name="opengl_fillrule">True to use the OpenGL fill rule; false to apply the half-pixel Direct3D bias.</param>
		public void GetBakedQuad(int pw, int ph, ref float xpos, ref float ypos, ref STBQuad q, bool opengl_fillrule)
		{
			var d3d_bias = opengl_fillrule ? 0 : -0.5f;
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			var round_x = (int)Math.Floor(xpos + XOff + 0.5f);
			var round_y = (int)Math.Floor(ypos + YOff + 0.5f);
			q.X0 = round_x + d3d_bias;
			q.Y0 = round_y + d3d_bias;
			q.X1 = round_x + X1 - X0 + d3d_bias;
			q.Y1 = round_y + Y1 - Y0 + d3d_bias;
			q.S0 = X0 * ipw;
			q.T0 = Y0 * iph;
			q.S1 = X1 * ipw;
			q.T1 = Y1 * iph;
			xpos += XAdvance;
		}
	}
}

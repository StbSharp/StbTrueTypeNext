using System;

namespace StbTrueTypeNext
{
	public struct stbtt_bakedchar
	{
		public ushort X0;
		public ushort Y0;
		public ushort X1;
		public ushort Y1;
		public float XOff;
		public float YOff;
		public float XAdvance;

		public void stbtt_GetBakedQuad(int pw, int ph, ref float xpos, ref float ypos, ref stbtt_aligned_quad q, int opengl_fillrule)
		{
			var d3d_bias = opengl_fillrule != 0 ? 0 : -0.5f;
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			var round_x = (int)Math.Floor(xpos + XOff + 0.5f);
			var round_y = (int)Math.Floor(ypos + YOff + 0.5f);
			q.x0 = round_x + d3d_bias;
			q.y0 = round_y + d3d_bias;
			q.x1 = round_x + X1 - X0 + d3d_bias;
			q.y1 = round_y + Y1 - Y0 + d3d_bias;
			q.s0 = X0 * ipw;
			q.t0 = Y0 * iph;
			q.s1 = X1 * ipw;
			q.t1 = Y1 * iph;
			xpos += XAdvance;
		}
	}
}

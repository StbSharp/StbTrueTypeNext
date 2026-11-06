using System;

namespace StbTrueTypeNext
{
	public struct STTBakedChar
	{
		public ushort X0;
		public ushort Y0;
		public ushort X1;
		public ushort Y1;
		public float XOff;
		public float YOff;
		public float XAdvance;

		public void stbtt_GetBakedQuad(int pw, int ph, ref float xpos, ref float ypos, ref STTQuad q, int opengl_fillrule)
		{
			var d3d_bias = opengl_fillrule != 0 ? 0 : -0.5f;
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

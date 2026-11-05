using System;

namespace StbTrueTypeNext.Packing
{
	public struct PackedChar
	{
		public ushort X0;
		public ushort Y0;
		public ushort X1;
		public ushort Y1;
		public float XOff;
		public float YOff;
		public float XAdvance;
		public float XOff2;
		public float YOff2;

		public void stbtt_GetPackedQuad(int pw, int ph, ref float xpos, ref float ypos, ref stbtt_aligned_quad q, int align_to_integer)
		{
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			if (align_to_integer != 0)
			{
				float x = (int)Math.Floor(xpos + XOff + 0.5f);
				float y = (int)Math.Floor(ypos + YOff + 0.5f);
				q.x0 = x;
				q.y0 = y;
				q.x1 = x + XOff2 - XOff;
				q.y1 = y + YOff2 - YOff;
			}
			else
			{
				q.x0 = xpos + XOff;
				q.y0 = ypos + YOff;
				q.x1 = xpos + XOff2;
				q.y1 = ypos + YOff2;
			}

			q.s0 = X0 * ipw;
			q.t0 = Y0 * iph;
			q.s1 = X1 * ipw;
			q.t1 = Y1 * iph;
			xpos += XAdvance;
		}
	}
}

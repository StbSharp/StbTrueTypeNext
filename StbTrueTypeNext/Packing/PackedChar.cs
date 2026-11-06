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

		public void GetPackedQuad(int pw, int ph, ref float xpos, ref float ypos, ref STBQuad q, int align_to_integer)
		{
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			if (align_to_integer != 0)
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

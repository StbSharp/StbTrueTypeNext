namespace StbTrueTypeNext
{
	public struct STBVertex
	{
		public short X;
		public short Y;
		public short CX;
		public short CY;
		public short CX1;
		public short CY1;
		public byte Type;
		public byte Padding;

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

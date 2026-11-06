namespace StbTrueTypeNext.Packing
{
	internal class RectPackContext
	{
		public int Width;
		public int Height;
		public int X;
		public int Y;
		public int BottomY;

		public void InitTarget(int pw, int ph)
		{
			Width = pw;
			Height = ph;
			X = 0;
			Y = 0;
			BottomY = 0;
		}

		public void PackRects(PackRect[] rects, int num_rects)
		{
			var i = 0;
			for (i = 0; i < num_rects; ++i)
			{
				if (X + rects[i].W > Width)
				{
					X = 0;
					Y = BottomY;
				}

				if (Y + rects[i].H > Height)
					break;
				rects[i].X = X;
				rects[i].Y = Y;
				rects[i].WasPacked = 1;
				X += rects[i].W;
				if (Y + rects[i].H > BottomY)
					BottomY = Y + rects[i].H;
			}

			for (; i < num_rects; ++i) rects[i].WasPacked = 0;
		}
	}
}

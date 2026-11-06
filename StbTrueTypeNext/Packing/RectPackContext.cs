namespace StbTrueTypeNext.Packing
{
	internal class RectPackContext
	{
		public int width;
		public int height;
		public int x;
		public int y;
		public int bottom_y;

		public void InitTarget(int pw, int ph)
		{
			width = pw;
			height = ph;
			x = 0;
			y = 0;
			bottom_y = 0;
		}

		public void PackRects(PackRect[] rects, int num_rects)
		{
			var i = 0;
			for (i = 0; i < num_rects; ++i)
			{
				if (x + rects[i].W > width)
				{
					x = 0;
					y = bottom_y;
				}

				if (y + rects[i].H > height)
					break;
				rects[i].X = x;
				rects[i].Y = y;
				rects[i].WasPacked = 1;
				x += rects[i].W;
				if (y + rects[i].H > bottom_y)
					bottom_y = y + rects[i].H;
			}

			for (; i < num_rects; ++i) rects[i].WasPacked = 0;
		}
	}
}

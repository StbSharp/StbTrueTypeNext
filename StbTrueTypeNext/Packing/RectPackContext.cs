namespace StbTrueTypeNext.Packing
{
	internal class RectPackContext
	{
		public int width;
		public int height;
		public int x;
		public int y;
		public int bottom_y;

		public void stbrp_init_target(int pw, int ph)
		{
			width = pw;
			height = ph;
			x = 0;
			y = 0;
			bottom_y = 0;
		}

		public void stbrp_pack_rects(PackRect[] rects, int num_rects)
		{
			var i = 0;
			for (i = 0; i < num_rects; ++i)
			{
				if (x + rects[i].w > width)
				{
					x = 0;
					y = bottom_y;
				}

				if (y + rects[i].h > height)
					break;
				rects[i].x = x;
				rects[i].y = y;
				rects[i].was_packed = 1;
				x += rects[i].w;
				if (y + rects[i].h > bottom_y)
					bottom_y = y + rects[i].h;
			}

			for (; i < num_rects; ++i) rects[i].was_packed = 0;
		}
	}
}

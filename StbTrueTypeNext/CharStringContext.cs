namespace StbTrueTypeNext
{
	internal enum CharStringContextMode
	{
		Draw,
		CalculateBounds
	}

	internal class CharStringContext
	{
		private bool _started;
		public float first_x;
		public float first_y;
		public float x;
		public float y;
		public int min_x;
		public int max_x;
		public int min_y;
		public int max_y;
		public STBVertex[] pvertices;
		public int num_vertices;

		public CharStringContextMode Mode { get; set; } = CharStringContextMode.Draw;

		public void CloseShape()
		{
			if (first_x != x || first_y != y)
				AddVertex(Constants.STBTT_vline, (int)first_x, (int)first_y, 0, 0, 0, 0);
		}

		public void RelativeCubicCurveTo(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
		{
			var cx1 = x + dx1;
			var cy1 = y + dy1;
			var cx2 = cx1 + dx2;
			var cy2 = cy1 + dy2;
			x = cx2 + dx3;
			y = cy2 + dy3;
			AddVertex(Constants.STBTT_vcubic, (int)x, (int)y, (int)cx1, (int)cy1, (int)cx2, (int)cy2);
		}

		public void RelativeLineTo(float dx, float dy)
		{
			x += dx;
			y += dy;
			AddVertex(Constants.STBTT_vline, (int)x, (int)y, 0, 0, 0, 0);
		}

		public void RelativeMoveTo(float dx, float dy)
		{
			CloseShape();
			first_x = x = x + dx;
			first_y = y = y + dy;
			AddVertex(Constants.STBTT_vmove, (int)x, (int)y, 0, 0, 0, 0);
		}

		public void AddVertex(byte type, int x, int y, int cx, int cy, int cx1, int cy1)
		{
			if (Mode == CharStringContextMode.CalculateBounds)
			{
				TrackVertex(x, y);
				if (type == Constants.STBTT_vcubic)
				{
					TrackVertex(cx, cy);
					TrackVertex(cx1, cy1);
				}
			}
			else
			{
				pvertices[num_vertices].Set(type, x, y, cx, cy);
				pvertices[num_vertices].CX1 = (short)cx1;
				pvertices[num_vertices].CY1 = (short)cy1;
			}

			num_vertices++;
		}

		public void TrackVertex(int x, int y)
		{
			if (x > max_x || !_started)
				max_x = x;
			if (y > max_y || !_started)
				max_y = y;
			if (x < min_x || !_started)
				min_x = x;
			if (y < min_y || !_started)
				min_y = y;

			_started = true;
		}
	}
}

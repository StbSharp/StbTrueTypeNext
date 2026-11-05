namespace StbTrueTypeNext
{
	internal class stbtt__csctx
	{
		public int bounds;
		public int started;
		public float first_x;
		public float first_y;
		public float x;
		public float y;
		public int min_x;
		public int max_x;
		public int min_y;
		public int max_y;
		public STTVertex[] pvertices;
		public int num_vertices;

		public void stbtt__csctx_close_shape()
		{
			if (first_x != x || first_y != y)
				stbtt__csctx_v(this, StbTrueType.STBTT_vline, (int)first_x, (int)first_y, 0, 0, 0, 0);
		}

		public void stbtt__csctx_rccurve_to(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
		{
			var cx1 = x + dx1;
			var cy1 = y + dy1;
			var cx2 = cx1 + dx2;
			var cy2 = cy1 + dy2;
			x = cx2 + dx3;
			y = cy2 + dy3;
			stbtt__csctx_v(this, StbTrueType.STBTT_vcubic, (int)x, (int)y, (int)cx1, (int)cy1, (int)cx2, (int)cy2);
		}

		public void stbtt__csctx_rline_to(float dx, float dy)
		{
			x += dx;
			y += dy;
			stbtt__csctx_v(this, StbTrueType.STBTT_vline, (int)x, (int)y, 0, 0, 0, 0);
		}

		public void stbtt__csctx_rmove_to(float dx, float dy)
		{
			stbtt__csctx_close_shape();
			first_x = x = x + dx;
			first_y = y = y + dy;
			stbtt__csctx_v(this, StbTrueType.STBTT_vmove, (int)x, (int)y, 0, 0, 0, 0);
		}

		public static void stbtt__csctx_v(stbtt__csctx c, byte type, int x, int y, int cx, int cy, int cx1, int cy1)
		{
			if (c.bounds != 0)
			{
				stbtt__track_vertex(c, x, y);
				if (type == StbTrueType.STBTT_vcubic)
				{
					stbtt__track_vertex(c, cx, cy);
					stbtt__track_vertex(c, cx1, cy1);
				}
			}
			else
			{
				StbTrueType.stbtt_setvertex(ref c.pvertices[c.num_vertices], type, x, y, cx, cy);
				c.pvertices[c.num_vertices].cx1 = (short)cx1;
				c.pvertices[c.num_vertices].cy1 = (short)cy1;
			}

			c.num_vertices++;
		}

		public static void stbtt__track_vertex(stbtt__csctx c, int x, int y)
		{
			if (x > c.max_x || c.started == 0)
				c.max_x = x;
			if (y > c.max_y || c.started == 0)
				c.max_y = y;
			if (x < c.min_x || c.started == 0)
				c.min_x = x;
			if (y < c.min_y || c.started == 0)
				c.min_y = y;
			c.started = 1;
		}
	}
}

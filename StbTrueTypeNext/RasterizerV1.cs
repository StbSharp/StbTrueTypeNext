using System;
using System.Collections.Generic;
using static StbTrueTypeNext.STTFont;

namespace StbTrueTypeNext
{
	internal static class RasterizerV1
	{
		private class stbtt__active_edge_old_rasterizer
		{
			public int x;
			public int dx;
			public float ey;
			public int direction;
		}

		private static stbtt__active_edge_old_rasterizer stbtt__new_active_old_rasterizer(ref Edge e, int off_x, float start_point)
		{
			var z = new stbtt__active_edge_old_rasterizer();
			float dxdy = (e.x1 - e.x0) / (e.y1 - e.y0);
			if ((dxdy) < (0))
				z.dx = (int)(-((int)(Math.Floor((double)((1 << 10) * -dxdy)))));
			else
				z.dx = ((int)(Math.Floor((double)((1 << 10) * dxdy))));

			z.x = ((int)(Math.Floor((double)((1 << 10) * e.x0 + z.dx * (start_point - e.y0)))));
			z.x -= (int)(off_x * (1 << 10));
			z.ey = (float)(e.y1);
			z.direction = (int)((e.invert) != 0 ? 1 : -1);

			return z;
		}

		private static void stbtt__fill_active_edges_old_rasterizer(byte[] scanline, int len, List<stbtt__active_edge_old_rasterizer> edges, int max_weight)
		{
			int x0 = 0;
			int w = 0;

			for (var k = 0; k < edges.Count; ++k)
			{
				var e = edges[k];
				if ((w) == (0))
				{
					x0 = (int)(e.x);
					w += (int)(e.direction);
				}
				else
				{
					int x1 = e.x;
					w += (int)(e.direction);
					if ((w) == (0))
					{
						int i = x0 >> 10;
						int j = x1 >> 10;
						if (((i) < (len)) && ((j) >= (0)))
						{
							if ((i) == (j))
							{
								scanline[i] = (byte)(scanline[i] + (byte)((x1 - x0) * max_weight >> 10));
							}
							else
							{
								if ((i) >= (0))
									scanline[i] = (byte)(scanline[i] + (byte)((((1 << 10) - (x0 & ((1 << 10) - 1))) * max_weight) >> 10));
								else
									i = (int)(-1);
								if ((j) < (len))
									scanline[j] = (byte)(scanline[j] + (byte)(((x1 & ((1 << 10) - 1)) * max_weight) >> 10));
								else
									j = (int)(len);
								for (++i; (i) < (j); ++i)
								{
									scanline[i] = (byte)(scanline[i] + (byte)(max_weight));
								}
							}
						}
					}
				}
			}
		}

		public static void Rasterize(Bitmap result, List<Edge> es, int vsubsample, int off_x, int off_y)
		{
			var edges = new List<stbtt__active_edge_old_rasterizer>();
			int y = 0; int j = 0;
			int max_weight = (255 / vsubsample); // weight per vertical scanline
			int s = 0; // vertical subsample index
			var scanline_data = new byte[512];
			byte[] scanline;

			if ((result.Width) > (512))
			{
				scanline = new byte[result.Width];
			}
			else
			{
				scanline = scanline_data;
			}

			y = off_y * vsubsample;

			var e = es[es.Count - 1];
			e.y0 = (off_y + result.Height) * (float)(vsubsample) + 1;
			es[es.Count - 1] = e;

			var ei = 0;
			while ((j) < (result.Height))
			{
				scanline.Set((byte)0, result.Width);
				for (s = (int)(0); (s) < (vsubsample); ++s)
				{
					// find center of pixel for this scanline
					float scan_y = y + 0.5f;

					// update all active edges;
					// remove all active edges that terminate before the center of this scanline
					for (var i = edges.Count - 1; i >= 0; --i)
					{
						var z = edges[i];
						if ((z.ey) <= (scan_y))
						{
							edges.RemoveAt(i); // delete from list
						}
						else
						{
							z.x += (int)(z.dx); // advance to position for current scanline
						}
					}

					// resort the list if needed
					edges.Sort((a, b) => a.x.CompareTo(b.x));

					// insert all edges that start before the center of this scanline -- omit ones that also end on this scanline
					for (; (es[ei].y0) <= (scan_y); ++ei)
					{
						e = es[ei];
						if ((e.y1) > (scan_y))
						{
							var z = stbtt__new_active_old_rasterizer(ref e, (int)(off_x), (float)(scan_y));

							// find insertion point
							if (edges.Count == 0)
							{
								edges.Add(z);
							}
							else if ((z.x) < edges[0].x)
							{
								// insert at front
								edges.Insert(0, z);
							}
							else
							{
								var i = 0;
								for (; i < edges.Count && edges[i].x < z.x; ++i)
								{
								}

								edges.Insert(i, z);
							}
						}
					}

					stbtt__fill_active_edges_old_rasterizer(scanline, (int)(result.Width), edges, (int)(max_weight));
					++y;
				}

				var pixelsPtr = result.Pixels + j * result.Stride;
				pixelsPtr.Copy(scanline, result.Width);

				++j;
			}
		}
	}
}
using System;
using System.Collections.Generic;

namespace StbTrueTypeNext
{
	internal static class RasterizerV2
	{
		private class stbtt__active_edge
		{
			public float fx;
			public float fdx;
			public float fdy;
			public float direction;
			public float sy;
			public float ey;
		}

		private static stbtt__active_edge stbtt__new_active(ref Edge e, int off_x, float start_point)
		{
			var dxdy = (e.x1 - e.x0) / (e.y1 - e.y0);

			return new stbtt__active_edge
			{
				fdx = dxdy,
				fdy = dxdy != 0.0f ? 1.0f / dxdy : 0.0f,
				fx = e.x0 + dxdy * (start_point - e.y0) - off_x,
				direction = e.invert != 0 ? 1.0f : -1.0f,
				sy = e.y0,
				ey = e.y1
			};
		}

		private static void stbtt__fill_active_edges_new(FakePtr<float> scanline, FakePtr<float> scanline_fill, int len, List<stbtt__active_edge> es, float y_top)
		{
			var y_bottom = y_top + 1;

			for (var i = 0; i < es.Count; ++i)
			{
				var e = es[i];
				if (e.fdx == 0)
				{
					var x0 = e.fx;
					if (x0 < len)
					{
						if (x0 >= 0)
						{
							stbtt__handle_clipped_edge(scanline, (int)x0, e, x0, y_top, x0, y_bottom);
							stbtt__handle_clipped_edge(scanline_fill - 1, (int)x0 + 1, e, x0, y_top, x0, y_bottom);
						}
						else
						{
							stbtt__handle_clipped_edge(scanline_fill - 1, 0, e, x0, y_top, x0, y_bottom);
						}
					}
				}
				else
				{
					var x0 = e.fx;
					var dx = e.fdx;
					var xb = x0 + dx;
					float x_top = 0;
					float x_bottom = 0;
					float sy0 = 0;
					float sy1 = 0;
					var dy = e.fdy;
					if (e.sy > y_top)
					{
						x_top = x0 + dx * (e.sy - y_top);
						sy0 = e.sy;
					}
					else
					{
						x_top = x0;
						sy0 = y_top;
					}

					if (e.ey < y_bottom)
					{
						x_bottom = x0 + dx * (e.ey - y_top);
						sy1 = e.ey;
					}
					else
					{
						x_bottom = xb;
						sy1 = y_bottom;
					}

					if (x_top >= 0 && x_bottom >= 0 && x_top < len && x_bottom < len)
					{
						if ((int)x_top == (int)x_bottom)
						{
							float height = 0;
							var x = (int)x_top;
							height = (sy1 - sy0) * e.direction;
							scanline[x] += stbtt__position_trapezoid_area(height, x_top, x + 1.0f, x_bottom, x + 1.0f);
							scanline_fill[x] += height;
						}
						else
						{
							var x = 0;
							var x1 = 0;
							var x2 = 0;
							float y_crossing = 0;
							float y_final = 0;
							float step = 0;
							float sign = 0;
							float area = 0;
							if (x_top > x_bottom)
							{
								float t = 0;
								sy0 = y_bottom - (sy0 - y_top);
								sy1 = y_bottom - (sy1 - y_top);
								t = sy0;
								sy0 = sy1;
								sy1 = t;
								t = x_bottom;
								x_bottom = x_top;
								x_top = t;
								dx = -dx;
								dy = -dy;
								t = x0;
								x0 = xb;
								xb = t;
							}

							x1 = (int)x_top;
							x2 = (int)x_bottom;
							y_crossing = y_top + dy * (x1 + 1 - x0);
							y_final = y_top + dy * (x2 - x0);
							if (y_crossing > y_bottom)
								y_crossing = y_bottom;
							sign = e.direction;
							area = sign * (y_crossing - sy0);
							scanline[x1] += stbtt__sized_triangle_area(area, x1 + 1 - x_top);
							if (y_final > y_bottom)
							{
								y_final = y_bottom;
								dy = (y_final - y_crossing) / (x2 - (x1 + 1));
							}

							step = sign * dy * 1;
							for (x = x1 + 1; x < x2; ++x)
							{
								scanline[x] += area + step / 2;
								area += step;
							}

							scanline[x2] += area + sign *
								stbtt__position_trapezoid_area(sy1 - y_final, x2, x2 + 1.0f, x_bottom, x2 + 1.0f);
							scanline_fill[x2] += sign * (sy1 - sy0);
						}
					}
					else
					{
						var x = 0;
						for (x = 0; x < len; ++x)
						{
							var y0 = y_top;
							float x1 = x;
							float x2 = x + 1;
							var x3 = xb;
							var y3 = y_bottom;
							var y1 = (x - x0) / dx + y_top;
							var y2 = (x + 1 - x0) / dx + y_top;
							if (x0 < x1 && x3 > x2)
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x1, y1);
								stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x2, y2);
								stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x3, y3);
							}
							else if (x3 < x1 && x0 > x2)
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x2, y2);
								stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x1, y1);
								stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x3, y3);
							}
							else if (x0 < x1 && x3 > x1)
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x1, y1);
								stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x3, y3);
							}
							else if (x3 < x1 && x0 > x1)
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x1, y1);
								stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x3, y3);
							}
							else if (x0 < x2 && x3 > x2)
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x2, y2);
								stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x3, y3);
							}
							else if (x3 < x2 && x0 > x2)
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x2, y2);
								stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x3, y3);
							}
							else
							{
								stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x3, y3);
							}
						}
					}
				}
			}
		}

		private static void stbtt__handle_clipped_edge(FakePtr<float> scanline, int x, stbtt__active_edge e, float x0, float y0,
			float x1, float y1)
		{
			if (y0 == y1)
				return;
			if (y0 > e.ey)
				return;
			if (y1 < e.sy)
				return;
			if (y0 < e.sy)
			{
				x0 += (x1 - x0) * (e.sy - y0) / (y1 - y0);
				y0 = e.sy;
			}

			if (y1 > e.ey)
			{
				x1 += (x1 - x0) * (e.ey - y1) / (y1 - y0);
				y1 = e.ey;
			}

			if (x0 <= x && x1 <= x)
			{
				scanline[x] += e.direction * (y1 - y0);
			}
			else if (x0 >= x + 1 && x1 >= x + 1)
			{
			}
			else
			{
				scanline[x] += e.direction * (y1 - y0) * (1 - (x0 - x + (x1 - x)) / 2);
			}
		}

		public static void Rasterize(Bitmap result, List<Edge> es, int vsubsample, int off_x, int off_y)
		{
			var y = 0;
			var j = 0;

			FakePtr<float> scanline;
			if (result.Width > 64)
			{
				scanline = new FakePtr<float>(result.Width * 2 + 1);
			}
			else
			{
				scanline = new FakePtr<float>(129);
			}

			var scanline2 = scanline + result.Width;
			y = off_y;

			var e = es[es.Count - 1];
			e.y0 = (float)(off_y + result.Height) + 1;
			es[es.Count - 1] = e;

			var ei = 0;
			var edges = new List<stbtt__active_edge>();
			while (j < result.Height)
			{
				// find center of pixel for this scanline
				var scan_y_top = y + 0.0f;
				var scan_y_bottom = y + 1.0f;

				scanline.Set(0, result.Width);
				scanline2.Set(0, result.Width + 1);

				// update all active edges;
				// remove all active edges that terminate before the top of this scanline
				for (var i = edges.Count - 1; i >= 0; --i)
				{
					var z = edges[i];
					if (z.ey <= scan_y_top)
					{
						edges.RemoveAt(i); // delete from list
					}
				}

				// insert all edges that start before the bottom of this scanline
				for (; es[ei].y0 <= scan_y_bottom; ++ei)
				{
					e = es[ei];
					if (e.y0 != e.y1)
					{
						var z = stbtt__new_active(ref e, off_x, scan_y_top);
						if (j == 0 && off_y != 0)
						{
							if (z.ey < scan_y_top)
							{
								// this can happen due to subpixel positioning and some kind of fp rounding error i think
								z.ey = scan_y_top;
							}
						}

						// insert at front
						edges.Insert(0, z);
					}
				}

				// now process all active edges
				stbtt__fill_active_edges_new(scanline, scanline2 + 1, result.Width, edges, scan_y_top);

				float sum = 0;
				for (var i = 0; i < result.Width; ++i)
				{
					float k = 0;
					var m = 0;
					sum += scanline2[i];
					k = scanline[i] + sum;
					k = Math.Abs(k) * 255 + 0.5f;
					m = (int)k;
					if (m > 255)
						m = 255;
					result[j * result.Stride + i] = (byte)m;
				}

				// advance all the edges
				for (var i = 0; i < edges.Count; ++i)
				{
					var z = edges[i];
					z.fx += z.fdx;
				}

				++y;
				++j;
			}
		}

		private static float stbtt__position_trapezoid_area(float height, float tx0, float tx1, float bx0, float bx1)
		{
			return stbtt__sized_trapezoid_area(height, tx1 - tx0, bx1 - bx0);
		}

		private static float stbtt__sized_trapezoid_area(float height, float top_width, float bottom_width)
		{
			return (top_width + bottom_width) / 2.0f * height;
		}

		private static float stbtt__sized_triangle_area(float height, float width)
		{
			return height * width / 2;
		}
	}
}

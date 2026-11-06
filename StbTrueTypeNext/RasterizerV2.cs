using System;
using System.Collections.Generic;

namespace StbTrueTypeNext
{
	internal static class RasterizerV2
	{
		private class ActiveEdge
		{
			public float X;
			public float DeltaX;
			public float DeltaY;
			public float Direction;
			public float StartY;
			public float EndY;
		}

		private static ActiveEdge CreateActiveEdge(ref Edge e, int off_x, float start_point)
		{
			var dxdy = (e.X1 - e.X0) / (e.Y1 - e.Y0);

			return new ActiveEdge
			{
				DeltaX = dxdy,
				DeltaY = dxdy != 0.0f ? 1.0f / dxdy : 0.0f,
				X = e.X0 + dxdy * (start_point - e.Y0) - off_x,
				Direction = e.Invert ? 1.0f : -1.0f,
				StartY = e.Y0,
				EndY = e.Y1
			};
		}

		private static void FillActiveEdges(FakePtr<float> scanline, FakePtr<float> scanline_fill, int len, List<ActiveEdge> es, float y_top)
		{
			var y_bottom = y_top + 1;

			for (var i = 0; i < es.Count; ++i)
			{
				var e = es[i];
				if (e.DeltaX == 0)
				{
					var x0 = e.X;
					if (x0 < len)
					{
						if (x0 >= 0)
						{
							HandleClippedEdge(scanline, (int)x0, e, x0, y_top, x0, y_bottom);
							HandleClippedEdge(scanline_fill - 1, (int)x0 + 1, e, x0, y_top, x0, y_bottom);
						}
						else
						{
							HandleClippedEdge(scanline_fill - 1, 0, e, x0, y_top, x0, y_bottom);
						}
					}
				}
				else
				{
					var x0 = e.X;
					var dx = e.DeltaX;
					var xb = x0 + dx;
					float x_top = 0;
					float x_bottom = 0;
					float sy0 = 0;
					float sy1 = 0;
					var dy = e.DeltaY;
					if (e.StartY > y_top)
					{
						x_top = x0 + dx * (e.StartY - y_top);
						sy0 = e.StartY;
					}
					else
					{
						x_top = x0;
						sy0 = y_top;
					}

					if (e.EndY < y_bottom)
					{
						x_bottom = x0 + dx * (e.EndY - y_top);
						sy1 = e.EndY;
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
							height = (sy1 - sy0) * e.Direction;
							scanline[x] += PositionTrapezoidArea(height, x_top, x + 1.0f, x_bottom, x + 1.0f);
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
							sign = e.Direction;
							area = sign * (y_crossing - sy0);
							scanline[x1] += SizedTriangleArea(area, x1 + 1 - x_top);
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
								PositionTrapezoidArea(sy1 - y_final, x2, x2 + 1.0f, x_bottom, x2 + 1.0f);
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
								HandleClippedEdge(scanline, x, e, x0, y0, x1, y1);
								HandleClippedEdge(scanline, x, e, x1, y1, x2, y2);
								HandleClippedEdge(scanline, x, e, x2, y2, x3, y3);
							}
							else if (x3 < x1 && x0 > x2)
							{
								HandleClippedEdge(scanline, x, e, x0, y0, x2, y2);
								HandleClippedEdge(scanline, x, e, x2, y2, x1, y1);
								HandleClippedEdge(scanline, x, e, x1, y1, x3, y3);
							}
							else if (x0 < x1 && x3 > x1)
							{
								HandleClippedEdge(scanline, x, e, x0, y0, x1, y1);
								HandleClippedEdge(scanline, x, e, x1, y1, x3, y3);
							}
							else if (x3 < x1 && x0 > x1)
							{
								HandleClippedEdge(scanline, x, e, x0, y0, x1, y1);
								HandleClippedEdge(scanline, x, e, x1, y1, x3, y3);
							}
							else if (x0 < x2 && x3 > x2)
							{
								HandleClippedEdge(scanline, x, e, x0, y0, x2, y2);
								HandleClippedEdge(scanline, x, e, x2, y2, x3, y3);
							}
							else if (x3 < x2 && x0 > x2)
							{
								HandleClippedEdge(scanline, x, e, x0, y0, x2, y2);
								HandleClippedEdge(scanline, x, e, x2, y2, x3, y3);
							}
							else
							{
								HandleClippedEdge(scanline, x, e, x0, y0, x3, y3);
							}
						}
					}
				}
			}
		}

		private static void HandleClippedEdge(FakePtr<float> scanline, int x, ActiveEdge e, float x0, float y0,
			float x1, float y1)
		{
			if (y0 == y1)
				return;
			if (y0 > e.EndY)
				return;
			if (y1 < e.StartY)
				return;
			if (y0 < e.StartY)
			{
				x0 += (x1 - x0) * (e.StartY - y0) / (y1 - y0);
				y0 = e.StartY;
			}

			if (y1 > e.EndY)
			{
				x1 += (x1 - x0) * (e.EndY - y1) / (y1 - y0);
				y1 = e.EndY;
			}

			if (x0 <= x && x1 <= x)
			{
				scanline[x] += e.Direction * (y1 - y0);
			}
			else if (x0 >= x + 1 && x1 >= x + 1)
			{
			}
			else
			{
				scanline[x] += e.Direction * (y1 - y0) * (1 - (x0 - x + (x1 - x)) / 2);
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
			e.Y0 = (float)(off_y + result.Height) + 1;
			es[es.Count - 1] = e;

			var ei = 0;
			var edges = new List<ActiveEdge>();
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
					if (z.EndY <= scan_y_top)
					{
						edges.RemoveAt(i); // delete from list
					}
				}

				// insert all edges that start before the bottom of this scanline
				for (; es[ei].Y0 <= scan_y_bottom; ++ei)
				{
					e = es[ei];
					if (e.Y0 != e.Y1)
					{
						var z = CreateActiveEdge(ref e, off_x, scan_y_top);
						if (j == 0 && off_y != 0)
						{
							if (z.EndY < scan_y_top)
							{
								// this can happen due to subpixel positioning and some kind of fp rounding error i think
								z.EndY = scan_y_top;
							}
						}

						// insert at front
						edges.Insert(0, z);
					}
				}

				// now process all active edges
				FillActiveEdges(scanline, scanline2 + 1, result.Width, edges, scan_y_top);

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
					z.X += z.DeltaX;
				}

				++y;
				++j;
			}
		}

		private static float PositionTrapezoidArea(float height, float tx0, float tx1, float bx0, float bx1)
		{
			return SizedTrapezoidArea(height, tx1 - tx0, bx1 - bx0);
		}

		private static float SizedTrapezoidArea(float height, float top_width, float bottom_width)
		{
			return (top_width + bottom_width) / 2.0f * height;
		}

		private static float SizedTriangleArea(float height, float width)
		{
			return height * width / 2;
		}
	}
}

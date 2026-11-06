using System;
using System.Collections.Generic;

namespace StbTrueTypeNext
{
	internal class Bitmap
	{
		private FakePtr<byte> _pixels;

		public int Width { get; }
		public int Height { get; }
		public int Stride { get; }

		public FakePtr<byte> Pixels => _pixels;

		public byte this[int index]
		{
			get => _pixels[index];

			set => _pixels[index] = value;
		}

		public Bitmap(int width, int height)
		{
			if (width <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width));
			}

			if (height <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(height));
			}

			Stride = Width = width;
			Height = height;
			_pixels = new FakePtr<byte>(width * height);
		}

		public Bitmap(int width, int height, int stride, FakePtr<byte> pixels)
		{
			if (width <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width));
			}

			if (height <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(height));
			}

			if (stride <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(stride));
			}

			if (pixels.IsNull)
			{
				throw new ArgumentNullException(nameof(pixels));
			}

			Width = width;
			Height = height;
			Stride = stride;
			_pixels = pixels;
		}

		public void Rasterize(float flatness_in_pixels, STBVertex[] vertices,
			int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert, STBRasterizerVersion rasterizerVersion)
		{
			var scale = scale_x > scale_y ? scale_y : scale_x;

			int[] winding_lengths;
			var windings = FlattenCurves(vertices, num_verts, flatness_in_pixels / scale, out winding_lengths);
			if (windings != null)
			{
				RasterizeInternal(windings, winding_lengths, scale_x, scale_y, shift_x, shift_y, x_off, y_off, invert, rasterizerVersion);
			}
		}

		private void RasterizeInternal(Vector2[] pts, int[] wcount,
			float scale_x, float scale_y, float shift_x, float shift_y, int off_x, int off_y, int invert, STBRasterizerVersion rasterizerVersion)
		{
			var y_scale_inv = invert != 0 ? -scale_y : scale_y;
			var i = 0;
			var j = 0;
			var k = 0;
			var m = 0;

			var vsubsample = 1;
			if (rasterizerVersion == STBRasterizerVersion.V1)
			{
				vsubsample = (Height) < (8) ? 15 : 5;
			}

			var windings = wcount.Length;

			var es = new List<Edge>();
			m = 0;
			for (i = 0; i < windings; ++i)
			{
				var p = new FakePtr<Vector2>(pts, m);
				m += wcount[i];
				j = wcount[i] - 1;
				for (k = 0; k < wcount[i]; j = k++)
				{
					var a = k;
					var b = j;
					if (p[j].y == p[k].y)
						continue;

					var e = new Edge();
					e.invert = 0;
					if (invert != 0 && p[j].y > p[k].y ||
						invert == 0 && p[j].y < p[k].y)
					{
						e.invert = 1;
						a = j;
						b = k;
					}

					e.x0 = p[a].x * scale_x + shift_x;
					e.y0 = (p[a].y * y_scale_inv + shift_y) * vsubsample;
					e.x1 = p[b].x * scale_x + shift_x;
					e.y1 = (p[b].y * y_scale_inv + shift_y) * vsubsample;
					es.Add(e);
				}
			}

			es.Sort((a, b) => a.y0.CompareTo(b.y0));

			// We need to add one additional value after the sorting
			// It is somehow used by below methods
			es.Add(new Edge());

			switch (rasterizerVersion)
			{
				case STBRasterizerVersion.V1:
					RasterizerV1.Rasterize(this, es, vsubsample, off_x, off_y);
					break;
				case STBRasterizerVersion.V2:
					RasterizerV2.Rasterize(this, es, vsubsample, off_x, off_y);
					break;
			}
		}

		private static Vector2[] FlattenCurves(STBVertex[] vertices, int num_verts, float objspace_flatness, out int[] contour_lengths)
		{
			Vector2[] points = null;
			var num_points = 0;
			var objspace_flatness_squared = objspace_flatness * objspace_flatness;
			var i = 0;
			var n = 0;
			var start = 0;
			var pass = 0;
			for (i = 0; i < num_verts; ++i)
				if (vertices[i].Type == Constants.STBTT_vmove)
					++n;

			contour_lengths = null;
			if (n == 0)
				return null;
			contour_lengths = new int[n];

			for (pass = 0; pass < 2; ++pass)
			{
				float x = 0;
				float y = 0;
				if (pass == 1)
				{
					points = new Vector2[num_points];
				}

				num_points = 0;
				n = -1;
				for (i = 0; i < num_verts; ++i)
					switch (vertices[i].Type)
					{
						case Constants.STBTT_vmove:
							if (n >= 0)
								contour_lengths[n] = num_points - start;
							++n;
							start = num_points;
							x = vertices[i].X;
							y = vertices[i].Y;
							AddPoint(points, num_points++, x, y);
							break;
						case Constants.STBTT_vline:
							x = vertices[i].X;
							y = vertices[i].Y;
							AddPoint(points, num_points++, x, y);
							break;
						case Constants.STBTT_vcurve:
							TesselateCurve(points, ref num_points, x, y, vertices[i].CX, vertices[i].CY,
								vertices[i].X, vertices[i].Y, objspace_flatness_squared, 0);
							x = vertices[i].X;
							y = vertices[i].Y;
							break;
						case Constants.STBTT_vcubic:
							TesselateCubic(points, ref num_points, x, y, vertices[i].CX, vertices[i].CY,
								vertices[i].CX1, vertices[i].CY1, vertices[i].X, vertices[i].Y,
								objspace_flatness_squared, 0);
							x = vertices[i].X;
							y = vertices[i].Y;
							break;
					}

				contour_lengths[n] = num_points - start;
			}

			return points;
		}

		private static void AddPoint(Vector2[] points, int n, float x, float y)
		{
			if (points == null)
				return;
			points[n].x = x;
			points[n].y = y;
		}

		private static void TesselateCubic(Vector2[] points, ref int num_points, float x0, float y0, float x1,
			float y1, float x2, float y2, float x3, float y3, float objspace_flatness_squared, int n)
		{
			var dx0 = x1 - x0;
			var dy0 = y1 - y0;
			var dx1 = x2 - x1;
			var dy1 = y2 - y1;
			var dx2 = x3 - x2;
			var dy2 = y3 - y2;
			var dx = x3 - x0;
			var dy = y3 - y0;
			var longlen = (float)(Math.Sqrt(dx0 * dx0 + dy0 * dy0) + Math.Sqrt(dx1 * dx1 + dy1 * dy1) +
								   Math.Sqrt(dx2 * dx2 + dy2 * dy2));
			var shortlen = (float)Math.Sqrt(dx * dx + dy * dy);
			var flatness_squared = longlen * longlen - shortlen * shortlen;
			if (n > 16)
				return;
			if (flatness_squared > objspace_flatness_squared)
			{
				var x01 = (x0 + x1) / 2;
				var y01 = (y0 + y1) / 2;
				var x12 = (x1 + x2) / 2;
				var y12 = (y1 + y2) / 2;
				var x23 = (x2 + x3) / 2;
				var y23 = (y2 + y3) / 2;
				var xa = (x01 + x12) / 2;
				var ya = (y01 + y12) / 2;
				var xb = (x12 + x23) / 2;
				var yb = (y12 + y23) / 2;
				var mx = (xa + xb) / 2;
				var my = (ya + yb) / 2;
				TesselateCubic(points, ref num_points, x0, y0, x01, y01, xa, ya, mx, my, objspace_flatness_squared,
					n + 1);
				TesselateCubic(points, ref num_points, mx, my, xb, yb, x23, y23, x3, y3, objspace_flatness_squared,
					n + 1);
			}
			else
			{
				AddPoint(points, num_points, x3, y3);
				++num_points;
			}
		}

		private static int TesselateCurve(Vector2[] points, ref int num_points, float x0, float y0, float x1,
			float y1, float x2, float y2, float objspace_flatness_squared, int n)
		{
			var mx = (x0 + 2 * x1 + x2) / 4;
			var my = (y0 + 2 * y1 + y2) / 4;
			var dx = (x0 + x2) / 2 - mx;
			var dy = (y0 + y2) / 2 - my;
			if (n > 16)
				return 1;
			if (dx * dx + dy * dy > objspace_flatness_squared)
			{
				TesselateCurve(points, ref num_points, x0, y0, (x0 + x1) / 2.0f, (y0 + y1) / 2.0f, mx, my,
					objspace_flatness_squared, n + 1);
				TesselateCurve(points, ref num_points, mx, my, (x1 + x2) / 2.0f, (y1 + y2) / 2.0f, x2, y2,
					objspace_flatness_squared, n + 1);
			}
			else
			{
				AddPoint(points, num_points, x2, y2);
				++num_points;
			}

			return 1;
		}
	}
}
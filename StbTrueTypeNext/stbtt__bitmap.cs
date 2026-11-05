using System;
using System.Collections.Generic;
using static StbTrueTypeNext.StbTrueType;

namespace StbTrueTypeNext
{
	internal class stbtt__bitmap
	{
		private FakePtr<byte> _pixels;

		public int Width { get; }
		public int Height { get; }
		public int Stride { get; }

		public FakePtr<byte> Pixels => _pixels;

		public byte this[int index]
		{
			get => _pixels[index];

			set =>_pixels[index] = value;
		}

		public stbtt__bitmap(int width, int height)
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

		public stbtt__bitmap(int width, int height, int stride, FakePtr<byte> pixels)
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

		public void stbtt__rasterize(stbtt__point[] pts, int[] wcount,
			float scale_x, float scale_y, float shift_x, float shift_y, int off_x, int off_y, int invert, bool useOldRasterizer)
		{
			var y_scale_inv = invert != 0 ? -scale_y : scale_y;
			var i = 0;
			var j = 0;
			var k = 0;
			var m = 0;

			var vsubsample = 1;
			if (useOldRasterizer)
			{
				vsubsample = (Height) < (8) ? 15 : 5;
			}

			var windings = wcount.Length;

			var es = new List<stbtt__edge>();
			m = 0;
			for (i = 0; i < windings; ++i)
			{
				var p = new FakePtr<stbtt__point>(pts, m);
				m += wcount[i];
				j = wcount[i] - 1;
				for (k = 0; k < wcount[i]; j = k++)
				{
					var a = k;
					var b = j;
					if (p[j].y == p[k].y)
						continue;

					var e = new stbtt__edge();
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
			es.Add(new stbtt__edge());

			if (!useOldRasterizer)
			{
				RasterizerV2.Rasterize(this, es, vsubsample, off_x, off_y);
			}
			else
			{
				RasterizerV1.Rasterize(this, es, vsubsample, off_x, off_y);
			}
		}

		private static stbtt__point[] stbtt_FlattenCurves(stbtt_vertex[] vertices, int num_verts, float objspace_flatness, out int[] contour_lengths)
		{
			stbtt__point[] points = null;
			var num_points = 0;
			var objspace_flatness_squared = objspace_flatness * objspace_flatness;
			var i = 0;
			var n = 0;
			var start = 0;
			var pass = 0;
			for (i = 0; i < num_verts; ++i)
				if (vertices[i].type == STBTT_vmove)
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
					points = new stbtt__point[num_points];
				}

				num_points = 0;
				n = -1;
				for (i = 0; i < num_verts; ++i)
					switch (vertices[i].type)
					{
						case STBTT_vmove:
							if (n >= 0)
								contour_lengths[n] = num_points - start;
							++n;
							start = num_points;
							x = vertices[i].x;
							y = vertices[i].y;
							stbtt__add_point(points, num_points++, x, y);
							break;
						case STBTT_vline:
							x = vertices[i].x;
							y = vertices[i].y;
							stbtt__add_point(points, num_points++, x, y);
							break;
						case STBTT_vcurve:
							stbtt__tesselate_curve(points, ref num_points, x, y, vertices[i].cx, vertices[i].cy,
								vertices[i].x, vertices[i].y, objspace_flatness_squared, 0);
							x = vertices[i].x;
							y = vertices[i].y;
							break;
						case STBTT_vcubic:
							stbtt__tesselate_cubic(points, ref num_points, x, y, vertices[i].cx, vertices[i].cy,
								vertices[i].cx1, vertices[i].cy1, vertices[i].x, vertices[i].y,
								objspace_flatness_squared, 0);
							x = vertices[i].x;
							y = vertices[i].y;
							break;
					}

				contour_lengths[n] = num_points - start;
			}

			return points;
		}

		public void stbtt_Rasterize(float flatness_in_pixels, stbtt_vertex[] vertices,
			int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert, bool useOldRasterizer)
		{
			var scale = scale_x > scale_y ? scale_y : scale_x;

			int[] winding_lengths;
			var windings = stbtt_FlattenCurves(vertices, num_verts, flatness_in_pixels / scale, out winding_lengths);
			if (windings != null)
			{
				stbtt__rasterize(windings, winding_lengths, scale_x, scale_y, shift_x, shift_y, x_off, y_off, invert, useOldRasterizer);
			}
		}
	}
}
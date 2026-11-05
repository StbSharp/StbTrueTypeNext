using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StbTrueTypeNext
{
	partial class StbTrueType
	{
		private static void stbtt__rasterize(stbtt__bitmap result, stbtt__point[] pts, int[] wcount,
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
				vsubsample = (result.h) < (8) ? 15 : 5;
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
				RasterizerV2.Rasterize(result, es, vsubsample, off_x, off_y);
			}
			else
			{
				RasterizerV1.Rasterize(result, es, vsubsample, off_x, off_y);
			}
		}


		public static void stbtt_Rasterize(stbtt__bitmap result, float flatness_in_pixels, stbtt_vertex[] vertices,
			int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert, bool useOldRasterizer)
		{
			var scale = scale_x > scale_y ? scale_y : scale_x;

			int[] winding_lengths;
			var windings = stbtt_FlattenCurves(vertices, num_verts, flatness_in_pixels / scale, out winding_lengths);
			if (windings != null)
			{
				stbtt__rasterize(result, windings, winding_lengths, scale_x, scale_y, shift_x, shift_y, x_off, y_off, invert, useOldRasterizer);
			}
		}

		public class stbtt__bitmap
		{
			public int w;
			public int h;
			public int stride;
			public FakePtr<byte> pixels;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbtt__edge
		{
			public float x0;
			public float y0;
			public float x1;
			public float y1;
			public int invert;
		}
	}
}
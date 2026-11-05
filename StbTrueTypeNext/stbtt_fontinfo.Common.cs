using System;

namespace StbTrueTypeNext
{
	partial class stbtt_fontinfo
	{
		public const int STBTT_MAC_EID_ARABIC = 4;
		public const int STBTT_MAC_EID_CHINESE_TRAD = 2;
		public const int STBTT_MAC_EID_GREEK = 6;
		public const int STBTT_MAC_EID_HEBREW = 5;
		public const int STBTT_MAC_EID_JAPANESE = 1;
		public const int STBTT_MAC_EID_KOREAN = 3;
		public const int STBTT_MAC_EID_ROMAN = 0;
		public const int STBTT_MAC_EID_RUSSIAN = 7;
		public const int STBTT_MAC_LANG_ARABIC = 12;
		public const int STBTT_MAC_LANG_CHINESE_SIMPLIFIED = 33;
		public const int STBTT_MAC_LANG_CHINESE_TRAD = 19;
		public const int STBTT_MAC_LANG_DUTCH = 4;
		public const int STBTT_MAC_LANG_ENGLISH = 0;
		public const int STBTT_MAC_LANG_FRENCH = 1;
		public const int STBTT_MAC_LANG_GERMAN = 2;
		public const int STBTT_MAC_LANG_HEBREW = 10;
		public const int STBTT_MAC_LANG_ITALIAN = 3;
		public const int STBTT_MAC_LANG_JAPANESE = 11;
		public const int STBTT_MAC_LANG_KOREAN = 23;
		public const int STBTT_MAC_LANG_RUSSIAN = 32;
		public const int STBTT_MAC_LANG_SPANISH = 6;
		public const int STBTT_MAC_LANG_SWEDISH = 5;
		public const int STBTT_MS_EID_SHIFTJIS = 2;
		public const int STBTT_MS_EID_SYMBOL = 0;
		public const int STBTT_MS_EID_UNICODE_BMP = 1;
		public const int STBTT_MS_EID_UNICODE_FULL = 10;
		public const int STBTT_MS_LANG_CHINESE = 2052;
		public const int STBTT_MS_LANG_DUTCH = 1043;
		public const int STBTT_MS_LANG_ENGLISH = 1033;
		public const int STBTT_MS_LANG_FRENCH = 1036;
		public const int STBTT_MS_LANG_GERMAN = 1031;
		public const int STBTT_MS_LANG_HEBREW = 1037;
		public const int STBTT_MS_LANG_ITALIAN = 1040;
		public const int STBTT_MS_LANG_JAPANESE = 1041;
		public const int STBTT_MS_LANG_KOREAN = 1042;
		public const int STBTT_MS_LANG_RUSSIAN = 1049;
		public const int STBTT_MS_LANG_SPANISH = 1033;
		public const int STBTT_MS_LANG_SWEDISH = 1053;
		public const int STBTT_PLATFORM_ID_ISO = 2;
		public const int STBTT_PLATFORM_ID_MAC = 1;
		public const int STBTT_PLATFORM_ID_MICROSOFT = 3;
		public const int STBTT_PLATFORM_ID_UNICODE = 0;
		public const int STBTT_UNICODE_EID_ISO_10646 = 2;
		public const int STBTT_UNICODE_EID_UNICODE_1_0 = 0;
		public const int STBTT_UNICODE_EID_UNICODE_1_1 = 1;
		public const int STBTT_UNICODE_EID_UNICODE_2_0_BMP = 3;
		public const int STBTT_UNICODE_EID_UNICODE_2_0_FULL = 4;
		public const int STBTT_vcubic = 4;
		public const int STBTT_vcurve = 3;
		public const int STBTT_vline = 2;
		public const int STBTT_vmove = 1;

		private static int stbtt__close_shape(STTVertex[] vertices, int num_vertices, int was_off, int start_off, int sx, int sy, int scx, int scy, int cx, int cy)
		{
			if (start_off != 0)
			{
				if (was_off != 0)
					vertices[num_vertices++].Set(STBTT_vcurve, (cx + scx) >> 1, (cy + scy) >> 1, cx, cy);

				vertices[num_vertices++].Set(STBTT_vcurve, sx, sy, scx, scy);
			}
			else
			{
				if (was_off != 0)
					vertices[num_vertices++].Set(STBTT_vcurve, sx, sy, cx, cy);
				else
					vertices[num_vertices++].Set(STBTT_vline, sx, sy, 0, 0);
			}

			return num_vertices;
		}

		private static int stbtt__compute_crossings_x(float x, float y, int nverts, STTVertex[] verts)
		{
			var i = 0;
			var orig = new float[2];
			var ray = new float[] { 1, 0 };
			var hits = new float[4];

			float y_frac = 0;
			var winding = 0;

			y_frac = y % 1.0f;
			if (y_frac < 0.01f)
				y += 0.01f;
			else if (y_frac > 0.99f)
				y -= 0.01f;
			orig[0] = x;
			orig[1] = y;
			for (i = 0; i < nverts; ++i)
			{
				if (verts[i].Type == STBTT_vline)
				{
					int x0 = verts[i - 1].X;
					int y0 = verts[i - 1].Y;
					int x1 = verts[i].X;
					int y1 = verts[i].Y;
					if (y > (y0 < y1 ? y0 : y1) && y < (y0 < y1 ? y1 : y0) && x > (x0 < x1 ? x0 : x1))
					{
						var x_inter = (y - y0) / (y1 - y0) * (x1 - x0) + x0;
						if (x_inter < x)
							winding += y0 < y1 ? 1 : -1;
					}
				}

				if (verts[i].Type == STBTT_vcurve)
				{
					int x0 = verts[i - 1].X;
					int y0 = verts[i - 1].Y;
					int x1 = verts[i].CX;
					int y1 = verts[i].CY;
					int x2 = verts[i].X;
					int y2 = verts[i].Y;
					var ax = x0 < (x1 < x2 ? x1 : x2) ? x0 : x1 < x2 ? x1 : x2;
					var ay = y0 < (y1 < y2 ? y1 : y2) ? y0 : y1 < y2 ? y1 : y2;
					var by = y0 < (y1 < y2 ? y2 : y1) ? y1 < y2 ? y2 : y1 : y0;
					if (y > ay && y < by && x > ax)
					{
						var q0 = new Vector2(x0, y0);
						var q1 = new Vector2(x1, y1);
						var q2 = new Vector2(x2, y2);
						if (q0.Equals(q1) || q1.Equals(q2))
						{
							x0 = verts[i - 1].X;
							y0 = verts[i - 1].Y;
							x1 = verts[i].X;
							y1 = verts[i].Y;
							if (y > (y0 < y1 ? y0 : y1) && y < (y0 < y1 ? y1 : y0) && x > (x0 < x1 ? x0 : x1))
							{
								var x_inter = (y - y0) / (y1 - y0) * (x1 - x0) + x0;
								if (x_inter < x)
									winding += y0 < y1 ? 1 : -1;
							}
						}
						else
						{
							var num_hits = stbtt__ray_intersect_bezier(orig, ray, q0, q1, q2, hits);
							if (num_hits >= 1)
								if (hits[0] < 0)
									winding += hits[1] < 0 ? -1 : 1;
							if (num_hits >= 2)
								if (hits[2] < 0)
									winding += hits[3] < 0 ? -1 : 1;
						}
					}
				}
			}

			return winding;
		}

		private static float stbtt__cuberoot(float x)
		{
			if (x < 0)
				return -(float)Math.Pow(-x, 1.0f / 3.0f);
			return (float)Math.Pow(x, 1.0f / 3.0f);
		}

		private static int stbtt__ray_intersect_bezier(float[] orig, float[] ray, Vector2 q0, Vector2 q1, Vector2 q2, float[] hits)
		{
			var q0perp = q0.y * ray[0] - q0.x * ray[1];
			var q1perp = q1.y * ray[0] - q1.x * ray[1];
			var q2perp = q2.y * ray[0] - q2.x * ray[1];
			var roperp = orig[1] * ray[0] - orig[0] * ray[1];
			var a = q0perp - 2 * q1perp + q2perp;
			var b = q1perp - q0perp;
			var c = q0perp - roperp;
			float s0 = 0;
			float s1 = 0;
			var num_s = 0;
			if (a != 0.0)
			{
				var discr = b * b - a * c;
				if (discr > 0.0)
				{
					var rcpna = -1 / a;
					var d = (float)Math.Sqrt(discr);
					s0 = (b + d) * rcpna;
					s1 = (b - d) * rcpna;
					if (s0 >= 0.0 && s0 <= 1.0)
						num_s = 1;
					if (d > 0.0 && s1 >= 0.0 && s1 <= 1.0)
					{
						if (num_s == 0)
							s0 = s1;
						++num_s;
					}
				}
			}
			else
			{
				s0 = c / (-2 * b);
				if (s0 >= 0.0 && s0 <= 1.0)
					num_s = 1;
			}

			if (num_s == 0) return 0;

			var rcp_len2 = 1 / (ray[0] * ray[0] + ray[1] * ray[1]);
			var rayn_x = ray[0] * rcp_len2;
			var rayn_y = ray[1] * rcp_len2;
			var q0d = q0.x * rayn_x + q0.y * rayn_y;
			var q1d = q1.x * rayn_x + q1.y * rayn_y;
			var q2d = q2.x * rayn_x + q2.y * rayn_y;
			var rod = orig[0] * rayn_x + orig[1] * rayn_y;
			var q10d = q1d - q0d;
			var q20d = q2d - q0d;
			var q0rd = q0d - rod;
			hits[0] = q0rd + s0 * (2.0f - 2.0f * s0) * q10d + s0 * s0 * q20d;
			hits[1] = a * s0 + b;
			if (num_s > 1)
			{
				hits[2] = q0rd + s1 * (2.0f - 2.0f * s1) * q10d + s1 * s1 * q20d;
				hits[3] = a * s1 + b;
				return 2;
			}

			return 1;
		}

		private static int stbtt__solve_cubic(float a, float b, float c, float[] r)
		{
			var s = -a / 3;
			var p = b - a * a / 3;
			var q = a * (2 * a * a - 9 * b) / 27 + c;
			var p3 = p * p * p;
			var d = q * q + 4 * p3 / 27;
			if (d >= 0)
			{
				var z = (float)Math.Sqrt(d);
				var u = (-q + z) / 2;
				var v = (-q - z) / 2;
				u = stbtt__cuberoot(u);
				v = stbtt__cuberoot(v);
				r[0] = s + u + v;
				return 1;
			}
			else
			{
				var u = (float)Math.Sqrt(-p / 3);
				var v = (float)Math.Acos(-Math.Sqrt(-27 / p3) * q / 2) / 3;
				var m = (float)Math.Cos(v);
				var n = (float)Math.Cos(v - 3.141592 / 2) * 1.732050808f;
				r[0] = s + u * 2 * m;
				r[1] = s - u * (m + n);
				r[2] = s - u * (m - n);
				return 3;
			}
		}
	}
}
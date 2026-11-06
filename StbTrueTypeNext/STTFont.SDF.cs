using System;

namespace StbTrueTypeNext
{
	partial class STTFont
	{
		public byte[] stbtt_GetCodepointSDF(float scale, int codepoint, int padding,
			byte onedge_value, float pixel_dist_scale, out int width, out int height, out int xoff, out int yoff)
		{
			return stbtt_GetGlyphSDF(scale, stbtt_FindGlyphIndex(codepoint), padding, onedge_value,
				pixel_dist_scale, out width, out height, out xoff, out yoff);
		}

		public byte[] stbtt_GetGlyphSDF(float scale, int glyph, int padding, byte onedge_value, float pixel_dist_scale,
			out int width, out int height, out int xoff, out int yoff)
		{
			width = height = xoff = yoff = 0;
			byte[] data = null;

			var scale_x = scale;
			var scale_y = scale;
			var ix0 = 0;
			var iy0 = 0;
			var ix1 = 0;
			var iy1 = 0;
			var w = 0;
			var h = 0;
			if (scale == 0)
				return null;
			stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale, scale, 0.0f, 0.0f, out ix0, out iy0, out ix1, out iy1);
			if (ix0 == ix1 || iy0 == iy1)
				return null;
			ix0 -= padding;
			iy0 -= padding;
			ix1 += padding;
			iy1 += padding;
			w = ix1 - ix0;
			h = iy1 - iy0;

			width = w;
			height = h;
			xoff = ix0;
			yoff = iy0;
			scale_y = -scale_y;
			{
				var x = 0;
				var y = 0;
				var i = 0;
				var j = 0;
				STTVertex[] verts;
				var num_verts = stbtt_GetGlyphShape(glyph, out verts);
				data = new byte[w * h];
				var precompute = new float[num_verts];
				for (i = 0, j = num_verts - 1; i < num_verts; j = i++)
					if (verts[i].Type == Constants.STBTT_vline)
					{
						var x0 = verts[i].X * scale_x;
						var y0 = verts[i].Y * scale_y;
						var x1 = verts[j].X * scale_x;
						var y1 = verts[j].Y * scale_y;
						var dist = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
						precompute[i] = dist == 0 ? 0.0f : 1.0f / dist;
					}
					else if (verts[i].Type == Constants.STBTT_vcurve)
					{
						var x2 = verts[j].X * scale_x;
						var y2 = verts[j].Y * scale_y;
						var x1 = verts[i].CX * scale_x;
						var y1 = verts[i].CY * scale_y;
						var x0 = verts[i].X * scale_x;
						var y0 = verts[i].Y * scale_y;
						var bx = x0 - 2 * x1 + x2;
						var by = y0 - 2 * y1 + y2;
						var len2 = bx * bx + by * by;
						if (len2 != 0.0f)
							precompute[i] = 1.0f / (bx * bx + by * by);
						else
							precompute[i] = 0.0f;
					}
					else
					{
						precompute[i] = 0.0f;
					}

				var res = new float[3];
				for (y = iy0; y < iy1; ++y)
				{
					for (x = ix0; x < ix1; ++x)
					{
						float val = 0;
						var min_dist = 999999.0f;
						var sx = x + 0.5f;
						var sy = y + 0.5f;
						var x_gspace = sx / scale_x;
						var y_gspace = sy / scale_y;
						var winding = stbtt__compute_crossings_x(x_gspace, y_gspace, num_verts, verts);
						for (i = 0; i < num_verts; ++i)
						{
							var x0 = verts[i].X * scale_x;
							var y0 = verts[i].Y * scale_y;
							if (verts[i].Type == Constants.STBTT_vline && precompute[i] != 0.0f)
							{
								var x1 = verts[i - 1].X * scale_x;
								var y1 = verts[i - 1].Y * scale_y;
								float dist = 0;
								var dist2 = (x0 - sx) * (x0 - sx) + (y0 - sy) * (y0 - sy);
								if (dist2 < min_dist * min_dist)
									min_dist = (float)Math.Sqrt(dist2);
								dist = Math.Abs((x1 - x0) * (y0 - sy) - (y1 - y0) * (x0 - sx)) * precompute[i];
								if (dist < min_dist)
								{
									var dx = x1 - x0;
									var dy = y1 - y0;
									var px = x0 - sx;
									var py = y0 - sy;
									var t = -(px * dx + py * dy) / (dx * dx + dy * dy);
									if (t >= 0.0f && t <= 1.0f)
										min_dist = dist;
								}
							}
							else if (verts[i].Type == Constants.STBTT_vcurve)
							{
								var x2 = verts[i - 1].X * scale_x;
								var y2 = verts[i - 1].Y * scale_y;
								var x1 = verts[i].CX * scale_x;
								var y1 = verts[i].CY * scale_y;
								var box_x0 = (x0 < x1 ? x0 : x1) < x2 ? x0 < x1 ? x0 : x1 : x2;
								var box_y0 = (y0 < y1 ? y0 : y1) < y2 ? y0 < y1 ? y0 : y1 : y2;
								var box_x1 = (x0 < x1 ? x1 : x0) < x2 ? x2 : x0 < x1 ? x1 : x0;
								var box_y1 = (y0 < y1 ? y1 : y0) < y2 ? y2 : y0 < y1 ? y1 : y0;
								if (sx > box_x0 - min_dist && sx < box_x1 + min_dist && sy > box_y0 - min_dist &&
									sy < box_y1 + min_dist)
								{
									var num = 0;
									var ax = x1 - x0;
									var ay = y1 - y0;
									var bx = x0 - 2 * x1 + x2;
									var by = y0 - 2 * y1 + y2;
									var mx = x0 - sx;
									var my = y0 - sy;

									res.Set(0.0f);

									float px = 0;
									float py = 0;
									float t = 0;
									float it = 0;
									float dist2 = 0;
									var a_inv = precompute[i];
									if (a_inv == 0.0)
									{
										var a = 3 * (ax * bx + ay * by);
										var b = 2 * (ax * ax + ay * ay) + (mx * bx + my * by);
										var c = mx * ax + my * ay;
										if (a == 0.0)
										{
											if (b != 0.0) res[num++] = -c / b;
										}
										else
										{
											var discriminant = b * b - 4 * a * c;
											if (discriminant < 0)
											{
												num = 0;
											}
											else
											{
												var root = (float)Math.Sqrt(discriminant);
												res[0] = (-b - root) / (2 * a);
												res[1] = (-b + root) / (2 * a);
												num = 2;
											}
										}
									}
									else
									{
										var b = 3 * (ax * bx + ay * by) * a_inv;
										var c = (2 * (ax * ax + ay * ay) + (mx * bx + my * by)) * a_inv;
										var d = (mx * ax + my * ay) * a_inv;
										num = stbtt__solve_cubic(b, c, d, res);
									}

									dist2 = (x0 - sx) * (x0 - sx) + (y0 - sy) * (y0 - sy);
									if (dist2 < min_dist * min_dist)
										min_dist = (float)Math.Sqrt(dist2);
									if (num >= 1 && res[0] >= 0.0f && res[0] <= 1.0f)
									{
										t = res[0];
										it = 1.0f - t;
										px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
										py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
										dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
										if (dist2 < min_dist * min_dist)
											min_dist = (float)Math.Sqrt(dist2);
									}

									if (num >= 2 && res[1] >= 0.0f && res[1] <= 1.0f)
									{
										t = res[1];
										it = 1.0f - t;
										px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
										py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
										dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
										if (dist2 < min_dist * min_dist)
											min_dist = (float)Math.Sqrt(dist2);
									}

									if (num >= 3 && res[2] >= 0.0f && res[2] <= 1.0f)
									{
										t = res[2];
										it = 1.0f - t;
										px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
										py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
										dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
										if (dist2 < min_dist * min_dist)
											min_dist = (float)Math.Sqrt(dist2);
									}
								}
							}
						}

						if (winding == 0)
							min_dist = -min_dist;
						val = onedge_value + pixel_dist_scale * min_dist;
						if (val < 0)
							val = 0;
						else if (val > 255)
							val = 255;
						data[(y - iy0) * w + (x - ix0)] = (byte)val;
					}
				}
			}

			return data;
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
				if (verts[i].Type == Constants.STBTT_vline)
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

				if (verts[i].Type == Constants.STBTT_vcurve)
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

		private static float stbtt__cuberoot(float x)
		{
			if (x < 0)
				return -(float)Math.Pow(-x, 1.0f / 3.0f);
			return (float)Math.Pow(x, 1.0f / 3.0f);
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

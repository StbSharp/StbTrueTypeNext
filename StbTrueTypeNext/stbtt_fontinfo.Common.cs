using StbTrueTypeNext.Packing;
using System;
using System.Runtime.InteropServices;

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

		private static uint stbtt__find_table(FakePtr<byte> data, uint fontstart, string tag)
		{
			int num_tables = ttUSHORT(data + fontstart + 4);
			var tabledir = fontstart + 12;
			int i;
			for (i = 0; i < num_tables; ++i)
			{
				var loc = (uint)(tabledir + 16 * i);
				if ((data + loc + 0)[0] == tag[0] && (data + loc + 0)[1] == tag[1] &&
					(data + loc + 0)[2] == tag[2] && (data + loc + 0)[3] == tag[3])
					return ttULONG(data + loc + 8);
			}

			return 0;
		}

		private static int stbtt__close_shape(STTVertex[] vertices, int num_vertices, int was_off, int start_off, int sx, int sy, int scx, int scy, int cx, int cy)
		{
			if (start_off != 0)
			{
				if (was_off != 0)
					stbtt_setvertex(ref vertices[num_vertices++], STBTT_vcurve, (cx + scx) >> 1, (cy + scy) >> 1, cx, cy);
				stbtt_setvertex(ref vertices[num_vertices++], STBTT_vcurve, sx, sy, scx, scy);
			}
			else
			{
				if (was_off != 0)
					stbtt_setvertex(ref vertices[num_vertices++], STBTT_vcurve, sx, sy, cx, cy);
				else
					stbtt_setvertex(ref vertices[num_vertices++], STBTT_vline, sx, sy, 0, 0);
			}

			return num_vertices;
		}

		private static int stbtt_CompareUTF8toUTF16_bigendian_internal(FakePtr<byte> s1, int len1, FakePtr<byte> s2, int len2)
		{
			return len1 == stbtt__CompareUTF8toUTF16_bigendian_prefix(s1, len1, s2, len2) ? 1 : 0;
		}

		private static int stbtt__CompareUTF8toUTF16_bigendian_prefix(FakePtr<byte> s1, int len1, FakePtr<byte> s2, int len2)
		{
			var i = 0;
			while (len2 != 0)
			{
				var ch = (ushort)(s2[0] * 256 + s2[1]);
				if (ch < 0x80)
				{
					if (i >= len1)
						return -1;
					if (s1[i++] != ch)
						return -1;
				}
				else if (ch < 0x800)
				{
					if (i + 1 >= len1)
						return -1;
					if (s1[i++] != 0xc0 + (ch >> 6))
						return -1;
					if (s1[i++] != 0x80 + (ch & 0x3f))
						return -1;
				}
				else if (ch >= 0xd800 && ch < 0xdc00)
				{
					uint c = 0;
					var ch2 = (ushort)(s2[2] * 256 + s2[3]);
					if (i + 3 >= len1)
						return -1;
					c = (uint)(((ch - 0xd800) << 10) + (ch2 - 0xdc00) + 0x10000);
					if (s1[i++] != 0xf0 + (c >> 18))
						return -1;
					if (s1[i++] != 0x80 + ((c >> 12) & 0x3f))
						return -1;
					if (s1[i++] != 0x80 + ((c >> 6) & 0x3f))
						return -1;
					if (s1[i++] != 0x80 + (c & 0x3f))
						return -1;
					s2 += 2;
					len2 -= 2;
				}
				else if (ch >= 0xdc00 && ch < 0xe000)
				{
					return -1;
				}
				else
				{
					if (i + 2 >= len1)
						return -1;
					if (s1[i++] != 0xe0 + (ch >> 12))
						return -1;
					if (s1[i++] != 0x80 + ((ch >> 6) & 0x3f))
						return -1;
					if (s1[i++] != 0x80 + (ch & 0x3f))
						return -1;
				}

				s2 += 2;
				len2 -= 2;
			}

			return i;
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
				if (verts[i].type == STBTT_vline)
				{
					int x0 = verts[i - 1].x;
					int y0 = verts[i - 1].y;
					int x1 = verts[i].x;
					int y1 = verts[i].y;
					if (y > (y0 < y1 ? y0 : y1) && y < (y0 < y1 ? y1 : y0) && x > (x0 < x1 ? x0 : x1))
					{
						var x_inter = (y - y0) / (y1 - y0) * (x1 - x0) + x0;
						if (x_inter < x)
							winding += y0 < y1 ? 1 : -1;
					}
				}

				if (verts[i].type == STBTT_vcurve)
				{
					int x0 = verts[i - 1].x;
					int y0 = verts[i - 1].y;
					int x1 = verts[i].cx;
					int y1 = verts[i].cy;
					int x2 = verts[i].x;
					int y2 = verts[i].y;
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
							x0 = verts[i - 1].x;
							y0 = verts[i - 1].y;
							x1 = verts[i].x;
							y1 = verts[i].y;
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

		private static int stbtt__isfont(FakePtr<byte> font)
		{
			if (font[0] == 49 && font[1] == 0 && font[2] == 0 && font[3] == 0)
				return 1;
			if (font[0] == "typ1"[0] && font[1] == "typ1"[1] && font[2] == "typ1"[2] && font[3] == "typ1"[3])
				return 1;
			if (font[0] == "OTTO"[0] && font[1] == "OTTO"[1] && font[2] == "OTTO"[2] && font[3] == "OTTO"[3])
				return 1;
			if (font[0] == 0 && font[1] == 1 && font[2] == 0 && font[3] == 0)
				return 1;
			if (font[0] == "true"[0] && font[1] == "true"[1] && font[2] == "true"[2] && font[3] == "true"[3])
				return 1;
			return 0;
		}

		private static int stbtt__matches(FakePtr<byte> fc, uint offset, byte[] name, int flags)
		{
			var nlen = name.Length;
			uint nm = 0;
			uint hd = 0;
			if (stbtt__isfont(fc + offset) == 0)
				return 0;
			if (flags != 0)
			{
				hd = stbtt__find_table(fc, offset, "head");
				if ((ttUSHORT(fc + hd + 44) & 7) != (flags & 7))
					return 0;
			}

			nm = stbtt__find_table(fc, offset, "name");
			if (nm == 0)
				return 0;
			if (flags != 0)
			{
				if (stbtt__matchpair(fc, nm, name, nlen, 16, -1) != 0)
					return 1;
				if (stbtt__matchpair(fc, nm, name, nlen, 1, -1) != 0)
					return 1;
				if (stbtt__matchpair(fc, nm, name, nlen, 3, -1) != 0)
					return 1;
			}
			else
			{
				if (stbtt__matchpair(fc, nm, name, nlen, 16, 17) != 0)
					return 1;
				if (stbtt__matchpair(fc, nm, name, nlen, 1, 2) != 0)
					return 1;
				if (stbtt__matchpair(fc, nm, name, nlen, 3, -1) != 0)
					return 1;
			}

			return 0;
		}

		private static int stbtt__matchpair(FakePtr<byte> fc, uint nm, byte[] name, int nlen, int target_id, int next_id)
		{
			var i = 0;
			int count = ttUSHORT(fc + nm + 2);
			var stringOffset = (int)(nm + ttUSHORT(fc + nm + 4));
			for (i = 0; i < count; ++i)
			{
				var loc = (uint)(nm + 6 + 12 * i);
				int id = ttUSHORT(fc + loc + 6);
				if (id == target_id)
				{
					int platform = ttUSHORT(fc + loc + 0);
					int encoding = ttUSHORT(fc + loc + 2);
					int language = ttUSHORT(fc + loc + 4);
					if (platform == 0 || platform == 3 && encoding == 1 || platform == 3 && encoding == 10)
					{
						int slen = ttUSHORT(fc + loc + 8);
						int off = ttUSHORT(fc + loc + 10);
						var matchlen =
							stbtt__CompareUTF8toUTF16_bigendian_prefix(new FakePtr<byte>(name), nlen, fc + stringOffset + off, slen);
						if (matchlen >= 0)
						{
							if (i + 1 < count && ttUSHORT(fc + loc + 12 + 6) == next_id &&
								ttUSHORT(fc + loc + 12) == platform && ttUSHORT(fc + loc + 12 + 2) == encoding &&
								ttUSHORT(fc + loc + 12 + 4) == language)
							{
								slen = ttUSHORT(fc + loc + 12 + 8);
								off = ttUSHORT(fc + loc + 12 + 10);
								if (slen == 0)
								{
									if (matchlen == nlen)
										return 1;
								}
								else if (matchlen < nlen && name[matchlen] == 32)
								{
									++matchlen;
									if (stbtt_CompareUTF8toUTF16_bigendian_internal(new FakePtr<byte>(name, matchlen),
										nlen - matchlen, fc + stringOffset + off, slen) != 0)
										return 1;
								}
							}
							else
							{
								if (matchlen == nlen)
									return 1;
							}
						}
					}
				}
			}

			return 0;
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

		public static int stbtt_FindMatchingFont(byte[] fontdata, byte[] name, int flags)
		{
			return stbtt_FindMatchingFont_internal(fontdata, name, flags);
		}

		public static int stbtt_FindMatchingFont_internal(byte[] font_collection, byte[] name_utf8, int flags)
		{
			var i = 0;
			for (i = 0; ; ++i)
			{
				var off = stbtt_GetFontOffsetForIndex(font_collection, i);
				if (off < 0)
					return off;
				if (stbtt__matches(new FakePtr<byte>(font_collection), (uint)off, name_utf8, flags) != 0)
					return off;
			}
		}

		public static void stbtt_GetBakedQuad(stbtt_bakedchar[] chardata, int pw, int ph, int char_index,
			ref float xpos, ref float ypos, ref stbtt_aligned_quad q, int opengl_fillrule)
		{
			var d3d_bias = opengl_fillrule != 0 ? 0 : -0.5f;
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			var b = chardata[char_index];
			var round_x = (int)Math.Floor(xpos + b.xoff + 0.5f);
			var round_y = (int)Math.Floor(ypos + b.yoff + 0.5f);
			q.x0 = round_x + d3d_bias;
			q.y0 = round_y + d3d_bias;
			q.x1 = round_x + b.x1 - b.x0 + d3d_bias;
			q.y1 = round_y + b.y1 - b.y0 + d3d_bias;
			q.s0 = b.x0 * ipw;
			q.t0 = b.y0 * iph;
			q.s1 = b.x1 * ipw;
			q.t1 = b.y1 * iph;
			xpos += b.xadvance;
		}

		public static int stbtt_GetFontOffsetForIndex(byte[] data, int index)
		{
			return stbtt_GetFontOffsetForIndex_internal(new FakePtr<byte>(data), index);
		}

		public static int stbtt_GetFontOffsetForIndex_internal(FakePtr<byte> font_collection, int index)
		{

			if (stbtt__isfont(font_collection) != 0)
				return index == 0 ? 0 : -1;
			if (font_collection[0] == "ttcf"[0] && font_collection[1] == "ttcf"[1] && font_collection[2] == "ttcf"[2] &&
				font_collection[3] == "ttcf"[3])
				if (ttULONG(font_collection + 4) == 0x00010000 || ttULONG(font_collection + 4) == 0x00020000)
				{
					var n = ttLONG(font_collection + 8);
					if (index >= n)
						return -1;
					return (int)ttULONG(font_collection + 12 + index * 4);
				}

			return -1;
		}

		public static int stbtt_GetNumberOfFonts(byte[] data)
		{
			return stbtt_GetNumberOfFonts_internal(new FakePtr<byte>(data));
		}

		public static int stbtt_GetNumberOfFonts_internal(FakePtr<byte> font_collection)
		{
			if (stbtt__isfont(font_collection) != 0)
				return 1;
			if (font_collection[0] == "ttcf"[0] && font_collection[1] == "ttcf"[1] && font_collection[2] == "ttcf"[2] &&
				font_collection[3] == "ttcf"[3])
				if (ttULONG(font_collection + 4) == 0x00010000 || ttULONG(font_collection + 4) == 0x00020000)
					return ttLONG(font_collection + 8);

			return 0;
		}

		public static void stbtt_GetPackedQuad(PackedChar[] chardata, int pw, int ph, int char_index,
			ref float xpos, ref float ypos, ref stbtt_aligned_quad q, int align_to_integer)
		{
			var ipw = 1.0f / pw;
			var iph = 1.0f / ph;
			var b = chardata[char_index];
			if (align_to_integer != 0)
			{
				float x = (int)Math.Floor(xpos + b.xoff + 0.5f);
				float y = (int)Math.Floor(ypos + b.yoff + 0.5f);
				q.x0 = x;
				q.y0 = y;
				q.x1 = x + b.xoff2 - b.xoff;
				q.y1 = y + b.yoff2 - b.yoff;
			}
			else
			{
				q.x0 = xpos + b.xoff;
				q.y0 = ypos + b.yoff;
				q.x1 = xpos + b.xoff2;
				q.y1 = ypos + b.yoff2;
			}

			q.s0 = b.x0 * ipw;
			q.t0 = b.y0 * iph;
			q.s1 = b.x1 * ipw;
			q.t1 = b.y1 * iph;
			xpos += b.xadvance;
		}

		public static void stbtt_GetScaledFontVMetrics(byte[] fontdata, int index, float size,
			out float ascent, out float descent, out float lineGap)
		{
			var i_ascent = 0;
			var i_descent = 0;
			var i_lineGap = 0;
			float scale = 0;
			var info = FromMemory(fontdata, stbtt_GetFontOffsetForIndex(fontdata, index));
			scale = size > 0 ? info.stbtt_ScaleForPixelHeight(size) : info.stbtt_ScaleForMappingEmToPixels(-size);
			info.stbtt_GetFontVMetrics(out i_ascent, out i_descent, out i_lineGap);

			ascent = i_ascent * scale;
			descent = i_descent * scale;
			lineGap = i_lineGap * scale;
		}

		public static void stbtt_setvertex(ref STTVertex v, byte type, int x, int y, int cx, int cy)
		{
			v.type = type;
			v.x = (short)x;
			v.y = (short)y;
			v.cx = (short)cx;
			v.cy = (short)cy;
		}

		public static int ttLONG(FakePtr<byte> p)
		{
			return (p[0] << 24) + (p[1] << 16) + (p[2] << 8) + p[3];
		}

		public static short ttSHORT(FakePtr<byte> p)
		{
			return (short)(p[0] * 256 + p[1]);
		}

		public static uint ttULONG(FakePtr<byte> p)
		{
			return (uint)((p[0] << 24) + (p[1] << 16) + (p[2] << 8) + p[3]);
		}

		public static ushort ttUSHORT(FakePtr<byte> p)
		{
			return (ushort)(p[0] * 256 + p[1]);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbtt_aligned_quad
		{
			public float x0;
			public float y0;
			public float s0;
			public float t0;
			public float x1;
			public float y1;
			public float s1;
			public float t1;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbtt_bakedchar
		{
			public ushort x0;
			public ushort y0;
			public ushort x1;
			public ushort y1;
			public float xoff;
			public float yoff;
			public float xadvance;
		}
	}
}
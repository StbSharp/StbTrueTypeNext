namespace StbTrueTypeNext
{
	public static class FontCollection
	{
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
				hd = Utility.stbtt__find_table(fc, offset, "head");
				if ((Utility.ttUSHORT(fc + hd + 44) & 7) != (flags & 7))
					return 0;
			}

			nm = Utility.stbtt__find_table(fc, offset, "name");
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
			int count = Utility.ttUSHORT(fc + nm + 2);
			var stringOffset = (int)(nm + Utility.ttUSHORT(fc + nm + 4));
			for (i = 0; i < count; ++i)
			{
				var loc = (uint)(nm + 6 + 12 * i);
				int id = Utility.ttUSHORT(fc + loc + 6);
				if (id == target_id)
				{
					int platform = Utility.ttUSHORT(fc + loc + 0);
					int encoding = Utility.ttUSHORT(fc + loc + 2);
					int language = Utility.ttUSHORT(fc + loc + 4);
					if (platform == 0 || platform == 3 && encoding == 1 || platform == 3 && encoding == 10)
					{
						int slen = Utility.ttUSHORT(fc + loc + 8);
						int off = Utility.ttUSHORT(fc + loc + 10);
						var matchlen =
							stbtt__CompareUTF8toUTF16_bigendian_prefix(new FakePtr<byte>(name), nlen, fc + stringOffset + off, slen);
						if (matchlen >= 0)
						{
							if (i + 1 < count && Utility.ttUSHORT(fc + loc + 12 + 6) == next_id &&
								Utility.ttUSHORT(fc + loc + 12) == platform && Utility.ttUSHORT(fc + loc + 12 + 2) == encoding &&
								Utility.ttUSHORT(fc + loc + 12 + 4) == language)
							{
								slen = Utility.ttUSHORT(fc + loc + 12 + 8);
								off = Utility.ttUSHORT(fc + loc + 12 + 10);
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
				if (Utility.ttULONG(font_collection + 4) == 0x00010000 || Utility.ttULONG(font_collection + 4) == 0x00020000)
				{
					var n = Utility.ttLONG(font_collection + 8);
					if (index >= n)
						return -1;
					return (int)Utility.ttULONG(font_collection + 12 + index * 4);
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
				if (Utility.ttULONG(font_collection + 4) == 0x00010000 || Utility.ttULONG(font_collection + 4) == 0x00020000)
					return Utility.ttLONG(font_collection + 8);

			return 0;
		}
	}
}

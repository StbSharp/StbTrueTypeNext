namespace StbTrueTypeNext
{
	/// <summary>
	/// Reads TrueType/OpenType font files and 'ttcf' collections: reports how many fonts they hold,
	/// resolves a font's offset by index, and searches for a font by name.
	/// </summary>
	public static class STBFontCollection
	{
		/// <summary>Finds the first font in a file or collection whose name table matches a given name.</summary>
		/// <param name="font_collection">Raw bytes of a font file or 'ttcf' collection.</param>
		/// <param name="name_utf8">UTF-8 encoded font name to match against the font's name records.</param>
		/// <param name="flags">
		/// Matching flags. When non-zero, the low three bits are compared against the font's 'head' macStyle and
		/// names are matched regardless of language; when zero, no style check is done and specific full-name and
		/// version name pairs are required instead.
		/// </param>
		/// <returns>The byte offset of the first matching font within <paramref name="font_collection" />, or a negative value if none matches.</returns>
		public static int FindMatchingFont(byte[] font_collection, byte[] name_utf8, int flags)
		{
			var i = 0;
			for (i = 0; ; ++i)
			{
				var off = GetFontOffsetForIndex(font_collection, i);
				if (off < 0)
					return off;
				if (Matches(new FakePtr<byte>(font_collection), (uint)off, name_utf8, flags) != 0)
					return off;
			}
		}

		/// <summary>Gets the byte offset of one font inside a font file or 'ttcf' collection.</summary>
		/// <param name="data">Raw bytes of a font file or collection.</param>
		/// <param name="index">Zero-based index of the font; only 0 is valid for a single font file.</param>
		/// <returns>The offset of the requested font, or -1 if the index is out of range or the data is not a recognized font or collection.</returns>
		public static int GetFontOffsetForIndex(byte[] data, int index)
		{
			var dataPtr = new FakePtr<byte>(data);
			if (IsFont(dataPtr) != 0)
				return index == 0 ? 0 : -1;
			if (dataPtr[0] == "ttcf"[0] && dataPtr[1] == "ttcf"[1] && dataPtr[2] == "ttcf"[2] &&
				dataPtr[3] == "ttcf"[3])
				if (Utility.ttULONG(dataPtr + 4) == 0x00010000 || Utility.ttULONG(dataPtr + 4) == 0x00020000)
				{
					var n = Utility.ttLONG(dataPtr + 8);
					if (index >= n)
						return -1;
					return (int)Utility.ttULONG(dataPtr + 12 + index * 4);
				}

			return -1;
		}

		/// <summary>Gets the number of fonts contained in a font file or 'ttcf' collection.</summary>
		/// <param name="data">Raw bytes of a font file or collection.</param>
		/// <returns>1 for a single font, the font count for a 'ttcf' collection, or 0 if the data is not recognized.</returns>
		public static int GetNumberOfFonts(byte[] data)
		{
			var dataPtr = new FakePtr<byte>(data);
			if (IsFont(dataPtr) != 0)
				return 1;
			if (dataPtr[0] == "ttcf"[0] && dataPtr[1] == "ttcf"[1] && dataPtr[2] == "ttcf"[2] &&
				dataPtr[3] == "ttcf"[3])
				if (Utility.ttULONG(dataPtr + 4) == 0x00010000 || Utility.ttULONG(dataPtr + 4) == 0x00020000)
					return Utility.ttLONG(dataPtr + 8);

			return 0;
		}

		private static bool CompareUTF8toUTF16BigEndianInternal(FakePtr<byte> s1, int len1, FakePtr<byte> s2, int len2)
		{
			return len1 == CompareUTF8toUTF16BigEndianPrefix(s1, len1, s2, len2);
		}

		private static int CompareUTF8toUTF16BigEndianPrefix(FakePtr<byte> s1, int len1, FakePtr<byte> s2, int len2)
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

		private static int IsFont(FakePtr<byte> font)
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

		private static int Matches(FakePtr<byte> fc, uint offset, byte[] name, int flags)
		{
			var nlen = name.Length;
			uint nm = 0;
			uint hd = 0;
			if (IsFont(fc + offset) == 0)
				return 0;
			if (flags != 0)
			{
				hd = Utility.FindTable(fc, offset, "head");
				if ((Utility.ttUSHORT(fc + hd + 44) & 7) != (flags & 7))
					return 0;
			}

			nm = Utility.FindTable(fc, offset, "name");
			if (nm == 0)
				return 0;
			if (flags != 0)
			{
				if (MatchPair(fc, nm, name, nlen, 16, -1) != 0)
					return 1;
				if (MatchPair(fc, nm, name, nlen, 1, -1) != 0)
					return 1;
				if (MatchPair(fc, nm, name, nlen, 3, -1) != 0)
					return 1;
			}
			else
			{
				if (MatchPair(fc, nm, name, nlen, 16, 17) != 0)
					return 1;
				if (MatchPair(fc, nm, name, nlen, 1, 2) != 0)
					return 1;
				if (MatchPair(fc, nm, name, nlen, 3, -1) != 0)
					return 1;
			}

			return 0;
		}

		private static int MatchPair(FakePtr<byte> fc, uint nm, byte[] name, int nlen, int target_id, int next_id)
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
							CompareUTF8toUTF16BigEndianPrefix(new FakePtr<byte>(name), nlen, fc + stringOffset + off, slen);
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
									if (CompareUTF8toUTF16BigEndianInternal(new FakePtr<byte>(name, matchlen),
										nlen - matchlen, fc + stringOffset + off, slen))
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
	}
}

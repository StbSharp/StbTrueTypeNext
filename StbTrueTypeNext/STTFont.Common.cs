using System;

namespace StbTrueTypeNext
{
	partial class STTFont
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

		private static int stbtt__close_shape(STTVertex[] vertices, int num_vertices, int was_off, int start_off, int sx, int sy, int scx, int scy, int cx, int cy)
		{
			if (start_off != 0)
			{
				if (was_off != 0)
					vertices[num_vertices++].Set(Constants.STBTT_vcurve, (cx + scx) >> 1, (cy + scy) >> 1, cx, cy);

				vertices[num_vertices++].Set(Constants.STBTT_vcurve, sx, sy, scx, scy);
			}
			else
			{
				if (was_off != 0)
					vertices[num_vertices++].Set(Constants.STBTT_vcurve, sx, sy, cx, cy);
				else
					vertices[num_vertices++].Set(Constants.STBTT_vline, sx, sy, 0, 0);
			}

			return num_vertices;
		}
	}
}
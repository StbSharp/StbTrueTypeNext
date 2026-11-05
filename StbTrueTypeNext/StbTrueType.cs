using System;

namespace StbTrueTypeNext
{
	public static partial class StbTrueType
	{
		internal static bool usedOldRasterizer = false;

		public class stbtt_fontinfo
		{
			internal Buffer cff;
			internal Buffer charstrings;
			public byte[] data = null;
			internal Buffer fdselect;
			internal Buffer fontdicts;
			public int fontstart;
			public int glyf;
			public int gpos;
			internal Buffer gsubrs;
			public int head;
			public int hhea;
			public int hmtx;
			public int index_map;
			public int indexToLocFormat;
			public int kern;
			public int loca;
			public int numGlyphs;
			internal Buffer subrs;
			public int svg;
			public bool useOldRasterizer;
		}

		public static uint stbtt__find_table(FakePtr<byte> data, uint fontstart, string tag)
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

		/// <summary>
		/// Creates and initializes a font from ttf/otf/ttc data
		/// </summary>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <returns>null if the data was invalid</returns>
		public static stbtt_fontinfo CreateFont(byte[] data, int offset)
		{
			var info = new stbtt_fontinfo();
			if (stbtt_InitFont_internal(info, data, offset) == 0)
			{
				return null;
			}

			return info;
		}
	}
}
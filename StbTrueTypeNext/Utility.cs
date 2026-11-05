namespace StbTrueTypeNext
{
	internal static class Utility
	{
		public static void Set<T>(this T[] ptr, T value, int count) where T : unmanaged
		{
			for (int i = 0; i < count; ++i)
			{
				ptr[i] = value;
			}
		}

		public static void Set<T>(this T[] ptr, T value) where T : unmanaged => Set(ptr, value, ptr.Length);

		public static uint stbtt__find_table(this FakePtr<byte> data, uint fontstart, string tag)
		{
			int num_tables = stbtt_fontinfo.ttUSHORT(data + fontstart + 4);
			var tabledir = fontstart + 12;
			int i;
			for (i = 0; i < num_tables; ++i)
			{
				var loc = (uint)(tabledir + 16 * i);
				if ((data + loc + 0)[0] == tag[0] && (data + loc + 0)[1] == tag[1] &&
					(data + loc + 0)[2] == tag[2] && (data + loc + 0)[3] == tag[3])
					return stbtt_fontinfo.ttULONG(data + loc + 8);
			}

			return 0;
		}
	}
}

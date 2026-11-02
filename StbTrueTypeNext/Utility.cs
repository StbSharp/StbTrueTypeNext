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
	}
}

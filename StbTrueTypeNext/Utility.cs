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

		public static void stbtt__h_prefilter(byte[] pixels, int w, int h, int stride_in_bytes, uint kernel_width)
		{
			stbtt__h_prefilter(new FakePtr<byte>(pixels), w, h, stride_in_bytes, kernel_width);
		}

		public static void stbtt__h_prefilter(FakePtr<byte> pixels, int w, int h, int stride_in_bytes, uint kernel_width)
		{
			var buffer = new byte[8];
			var safe_w = (int)(w - kernel_width);
			var j = 0;
			for (j = 0; j < h; ++j)
			{
				var i = 0;
				uint total = 0;
				buffer.Set((byte)0, (int)kernel_width);
				total = 0;
				switch (kernel_width)
				{
					case 2:
						for (i = 0; i <= safe_w; ++i)
						{
							total += (uint)(pixels[i] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i];
							pixels[i] = (byte)(total / 2);
						}

						break;
					case 3:
						for (i = 0; i <= safe_w; ++i)
						{
							total += (uint)(pixels[i] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i];
							pixels[i] = (byte)(total / 3);
						}

						break;
					case 4:
						for (i = 0; i <= safe_w; ++i)
						{
							total += (uint)(pixels[i] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i];
							pixels[i] = (byte)(total / 4);
						}

						break;
					case 5:
						for (i = 0; i <= safe_w; ++i)
						{
							total += (uint)(pixels[i] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i];
							pixels[i] = (byte)(total / 5);
						}

						break;
					default:
						for (i = 0; i <= safe_w; ++i)
						{
							total += (uint)(pixels[i] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i];
							pixels[i] = (byte)(total / kernel_width);
						}

						break;
				}

				for (; i < w; ++i)
				{
					total -= buffer[i & (8 - 1)];
					pixels[i] = (byte)(total / kernel_width);
				}

				pixels += stride_in_bytes;
			}
		}
	}
}

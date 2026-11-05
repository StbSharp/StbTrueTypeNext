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

		public static float stbtt__oversample_shift(int oversample)
		{
			if (oversample == 0)
				return 0.0f;
			return -(oversample - 1) / (2.0f * oversample);
		}

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

		public static void stbtt__v_prefilter(byte[] pixels, int w, int h, int stride_in_bytes, uint kernel_width)
		{
			stbtt__v_prefilter(new FakePtr<byte>(pixels), w, h, stride_in_bytes, kernel_width);
		}

		public static void stbtt__v_prefilter(FakePtr<byte> pixels, int w, int h, int stride_in_bytes, uint kernel_width)
		{
			var buffer = new byte[8];
			var safe_h = (int)(h - kernel_width);
			var j = 0;
			for (j = 0; j < w; ++j)
			{
				var i = 0;
				uint total = 0;
				buffer.Set((byte)0, (int)kernel_width);
				total = 0;
				switch (kernel_width)
				{
					case 2:
						for (i = 0; i <= safe_h; ++i)
						{
							total += (uint)(pixels[i * stride_in_bytes] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i * stride_in_bytes];
							pixels[i * stride_in_bytes] = (byte)(total / 2);
						}

						break;
					case 3:
						for (i = 0; i <= safe_h; ++i)
						{
							total += (uint)(pixels[i * stride_in_bytes] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i * stride_in_bytes];
							pixels[i * stride_in_bytes] = (byte)(total / 3);
						}

						break;
					case 4:
						for (i = 0; i <= safe_h; ++i)
						{
							total += (uint)(pixels[i * stride_in_bytes] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i * stride_in_bytes];
							pixels[i * stride_in_bytes] = (byte)(total / 4);
						}

						break;
					case 5:
						for (i = 0; i <= safe_h; ++i)
						{
							total += (uint)(pixels[i * stride_in_bytes] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i * stride_in_bytes];
							pixels[i * stride_in_bytes] = (byte)(total / 5);
						}

						break;
					default:
						for (i = 0; i <= safe_h; ++i)
						{
							total += (uint)(pixels[i * stride_in_bytes] - buffer[i & (8 - 1)]);
							buffer[(i + kernel_width) & (8 - 1)] = pixels[i * stride_in_bytes];
							pixels[i * stride_in_bytes] = (byte)(total / kernel_width);
						}

						break;
				}

				for (; i < h; ++i)
				{
					total -= buffer[i & (8 - 1)];
					pixels[i * stride_in_bytes] = (byte)(total / kernel_width);
				}

				pixels += 1;
			}
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
	}
}

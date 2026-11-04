using System;

namespace StbTrueTypeNext.Hebron.Runtime
{
	internal static class CRuntime
	{
		public static uint _lrotl(uint x, int y)
		{
			return (x << y) | (x >> (32 - y));
		}

		public static int abs(int v)
		{
			return Math.Abs(v);
		}

		public static double pow(double a, double b)
		{
			return Math.Pow(a, b);
		}

		public static void SetArray<T>(T[] data, T value)
		{
			for (var i = 0; i < data.Length; ++i)
				data[i] = value;
		}

		public static double ldexp(double number, int exponent)
		{
			return number * Math.Pow(2, exponent);
		}

		public static float fabs(double a)
		{
			return (float)Math.Abs(a);
		}

		public static double ceil(double a)
		{
			return Math.Ceiling(a);
		}

		public static double floor(double a)
		{
			return Math.Floor(a);
		}

		public static double cos(double value)
		{
			return Math.Cos(value);
		}

		public static double acos(double value)
		{
			return Math.Acos(value);
		}

		public static double sin(double value)
		{
			return Math.Sin(value);
		}

		public static double sqrt(double val)
		{
			return Math.Sqrt(val);
		}

		public static double fmod(double x, double y)
		{
			return x % y;
		}
	}
}
using System;

namespace StbTrueTypeNext.Hebron.Runtime
{
	internal static class CRuntime
	{
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
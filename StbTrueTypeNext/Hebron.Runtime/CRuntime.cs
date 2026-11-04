using System;

namespace StbTrueTypeNext.Hebron.Runtime
{
	internal static class CRuntime
	{
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
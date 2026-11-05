namespace StbTrueTypeNext
{
	internal struct stbtt__point
	{
		public float x;
		public float y;

		public stbtt__point(float _x, float _y)
		{
			x = _x;
			y = _y;
		}

		public bool Equals(stbtt__point other)
		{
			return x == other.x && y == other.y;
		}
	}
}

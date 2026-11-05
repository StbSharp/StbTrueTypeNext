namespace StbTrueTypeNext
{
	internal struct Vector2
	{
		public float x;
		public float y;

		public Vector2(float _x, float _y)
		{
			x = _x;
			y = _y;
		}

		public bool Equals(Vector2 other)
		{
			return x == other.x && y == other.y;
		}
	}
}

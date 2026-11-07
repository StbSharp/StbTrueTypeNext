namespace StbTrueTypeNext
{
	internal struct Vector2
	{
		public float X;
		public float Y;

		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public bool Equals(Vector2 other)
		{
			return X == other.X && Y == other.Y;
		}
	}
}

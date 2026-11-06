using System.Collections.Generic;

namespace StbTrueTypeNext
{
	internal enum CharStringContextMode
	{
		Draw,
		CalculateBounds
	}

	internal class CharStringContext
	{
		private bool _started;
		private float _firstX;
		private float _firstY;
		private float _x;
		private float _y;
		private readonly List<STBVertex> _vertices = new List<STBVertex>();

		public CharStringContextMode Mode { get; set; } = CharStringContextMode.Draw;

		public int MinX { get; private set; }
		public int MaxX { get; private set; }
		public int MinY { get; private set; }
		public int MaxY { get; private set; }
		public int VerticesCount { get; private set; }

		public void CloseShape()
		{
			if (_firstX != _x || _firstY != _y)
				AddVertex(Constants.STBTT_vline, (int)_firstX, (int)_firstY, 0, 0, 0, 0);
		}

		public void RelativeCubicCurveTo(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
		{
			var cx1 = _x + dx1;
			var cy1 = _y + dy1;
			var cx2 = cx1 + dx2;
			var cy2 = cy1 + dy2;
			_x = cx2 + dx3;
			_y = cy2 + dy3;
			AddVertex(Constants.STBTT_vcubic, (int)_x, (int)_y, (int)cx1, (int)cy1, (int)cx2, (int)cy2);
		}

		public void RelativeLineTo(float dx, float dy)
		{
			_x += dx;
			_y += dy;
			AddVertex(Constants.STBTT_vline, (int)_x, (int)_y, 0, 0, 0, 0);
		}

		public void RelativeMoveTo(float dx, float dy)
		{
			CloseShape();
			_firstX = _x = _x + dx;
			_firstY = _y = _y + dy;
			AddVertex(Constants.STBTT_vmove, (int)_x, (int)_y, 0, 0, 0, 0);
		}

		public void AddVertex(byte type, int x, int y, int cx, int cy, int cx1, int cy1)
		{
			if (Mode == CharStringContextMode.CalculateBounds)
			{
				TrackVertex(x, y);
				if (type == Constants.STBTT_vcubic)
				{
					TrackVertex(cx, cy);
					TrackVertex(cx1, cy1);
				}
			}
			else
			{
				var newVertex = new STBVertex();
				newVertex.Set(type, x, y, cx, cy);
				newVertex.CX1 = (short)cx1;
				newVertex.CY1 = (short)cy1;

				_vertices.Add(newVertex);
			}

			VerticesCount++;
		}

		public void TrackVertex(int x, int y)
		{
			if (x > MaxX || !_started)
				MaxX = x;
			if (y > MaxY || !_started)
				MaxY = y;
			if (x < MinX || !_started)
				MinX = x;
			if (y < MinY || !_started)
				MinY = y;

			_started = true;
		}

		public STBVertex[] ToArray() => _vertices.ToArray();
	}
}

namespace StbTrueTypeNext
{
	internal struct Buffer
	{
		public static readonly Buffer Null = new Buffer();

		public FakePtr<byte> Data;
		public int Cursor;
		public int Size;

		public Buffer(FakePtr<byte> ptr, int sz)
		{
			Data = ptr;
			Size = sz;
			Cursor = 0;
		}

		public byte stbtt__buf_get8()
		{
			if (Cursor >= Size)
				return 0;
			return Data[Cursor++];
		}

		public uint stbtt__buf_get(int n)
		{
			uint v = 0;
			var i = 0;
			for (i = 0; i < n; i++) v = (v << 8) | stbtt__buf_get8();

			return v;
		}

		public byte stbtt__buf_peek8()
		{
			if (Cursor >= Size)
				return 0;
			return Data[Cursor];
		}

		public Buffer stbtt__buf_range(int o, int s)
		{
			var r = Null;
			if (o < 0 || s < 0 || o > Size || s > Size - o)
				return r;
			r.Data = Data + o;
			r.Size = s;
			return r;
		}

		public void stbtt__buf_seek(int o)
		{
			Cursor = o > Size || o < 0 ? Size : o;
		}

		public void stbtt__buf_skip(int o)
		{
			stbtt__buf_seek(Cursor + o);
		}

		public Buffer stbtt__cff_get_index()
		{
			var count = 0;
			var start = 0;
			var offsize = 0;
			start = Cursor;
			count = (int)stbtt__buf_get(2);
			if (count != 0)
			{
				offsize = stbtt__buf_get8();
				stbtt__buf_skip(offsize * count);
				stbtt__buf_skip((int)(stbtt__buf_get(offsize) - 1));
			}

			return stbtt__buf_range(start, Cursor - start);
		}

		public int stbtt__cff_index_count()
		{
			stbtt__buf_seek(0);
			return (int)stbtt__buf_get(2);
		}

		public uint stbtt__cff_int()
		{
			int b0 = stbtt__buf_get8();
			if (b0 >= 32 && b0 <= 246)
				return (uint)(b0 - 139);
			if (b0 >= 247 && b0 <= 250)
				return (uint)((b0 - 247) * 256 + stbtt__buf_get8() + 108);
			if (b0 >= 251 && b0 <= 254)
				return (uint)(-(b0 - 251) * 256 - stbtt__buf_get8() - 108);
			if (b0 == 28)
				return stbtt__buf_get(2);
			if (b0 == 29)
				return stbtt__buf_get(4);
			return 0;
		}

		public void stbtt__cff_skip_operand()
		{
			var v = 0;
			int b0 = stbtt__buf_peek8();
			if (b0 == 30)
			{
				stbtt__buf_skip(1);
				while (Cursor < Size)
				{
					v = stbtt__buf_get8();
					if ((v & 0xF) == 0xF || v >> 4 == 0xF)
						break;
				}
			}
			else
			{
				stbtt__cff_int();
			}
		}

		public Buffer stbtt__dict_get(int key)
		{
			stbtt__buf_seek(0);
			while (Cursor < Size)
			{
				var start = Cursor;
				var end = 0;
				var op = 0;
				while (stbtt__buf_peek8() >= 28) stbtt__cff_skip_operand();

				end = Cursor;
				op = stbtt__buf_get8();
				if (op == 12)
					op = stbtt__buf_get8() | 0x100;
				if (op == key)
					return stbtt__buf_range(start, end - start);
			}

			return stbtt__buf_range(0, 0);
		}

		public void stbtt__dict_get_ints(int key, int outcount, uint[] _out_)
		{
			var i = 0;
			var operands = stbtt__dict_get(key);
			for (i = 0; i < outcount && operands.Cursor < operands.Size; i++)
			{
				_out_[i] = operands.stbtt__cff_int();
			}
		}

		public bool stbtt__dict_get_ints(int key, ref uint _out_)
		{
			var operands = stbtt__dict_get(key);
			if (operands.Cursor < operands.Size)
			{
				_out_ = operands.stbtt__cff_int();
				return true;
			}

			return false;
		}

		public static Buffer stbtt__cff_index_get(Buffer b, int i)
		{
			var count = 0;
			var offsize = 0;
			var start = 0;
			var end = 0;
			b.stbtt__buf_seek(0);
			count = (int)b.stbtt__buf_get(2);
			offsize = b.stbtt__buf_get8();
			b.stbtt__buf_skip(i * offsize);
			start = (int)b.stbtt__buf_get(offsize);
			end = (int)b.stbtt__buf_get(offsize);
			return b.stbtt__buf_range(2 + (count + 1) * offsize + start, end - start);
		}

		public static Buffer stbtt__get_subr(Buffer idx, int n)
		{
			var count = idx.stbtt__cff_index_count();
			var bias = 107;
			if (count >= 33900)
				bias = 32768;
			else if (count >= 1240)
				bias = 1131;
			n += bias;
			if (n < 0 || n >= count)
				return Null;
			return stbtt__cff_index_get(idx, n);
		}

		public static Buffer stbtt__get_subrs(Buffer cff, Buffer fontdict)
		{
			var private_loc = new uint[] { 0, 0 };
			fontdict.stbtt__dict_get_ints(18, 2, private_loc);
			if (private_loc[1] == 0 || private_loc[0] == 0)
				return Null;
			var pdict = cff.stbtt__buf_range((int)private_loc[1], (int)private_loc[0]);
			uint subrsoff = 0;
			if (!pdict.stbtt__dict_get_ints(19, ref subrsoff))
			{
				return Null;
			}

			cff.stbtt__buf_seek((int)(private_loc[1] + subrsoff));
			return cff.stbtt__cff_get_index();
		}
	}
}
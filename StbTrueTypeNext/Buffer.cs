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

		public byte ReadByte()
		{
			if (Cursor >= Size)
				return 0;
			return Data[Cursor++];
		}

		public uint ReadUint(int n)
		{
			uint v = 0;
			var i = 0;
			for (i = 0; i < n; i++) v = (v << 8) | ReadByte();

			return v;
		}

		public byte PeekByte()
		{
			if (Cursor >= Size)
				return 0;
			return Data[Cursor];
		}

		public Buffer CreateRange(int o, int s)
		{
			var r = Null;
			if (o < 0 || s < 0 || o > Size || s > Size - o)
				return r;
			r.Data = Data + o;
			r.Size = s;
			return r;
		}

		public void Seek(int o)
		{
			Cursor = o > Size || o < 0 ? Size : o;
		}

		public void Skip(int o)
		{
			Seek(Cursor + o);
		}

		public Buffer ReadCffIndex()
		{
			var count = 0;
			var start = 0;
			var offsize = 0;
			start = Cursor;
			count = (int)ReadUint(2);
			if (count != 0)
			{
				offsize = ReadByte();
				Skip(offsize * count);
				Skip((int)(ReadUint(offsize) - 1));
			}

			return CreateRange(start, Cursor - start);
		}

		public int ReadCffIndexCount()
		{
			Seek(0);
			return (int)ReadUint(2);
		}

		public uint ReadCffUint()
		{
			int b0 = ReadByte();
			if (b0 >= 32 && b0 <= 246)
				return (uint)(b0 - 139);
			if (b0 >= 247 && b0 <= 250)
				return (uint)((b0 - 247) * 256 + ReadByte() + 108);
			if (b0 >= 251 && b0 <= 254)
				return (uint)(-(b0 - 251) * 256 - ReadByte() - 108);
			if (b0 == 28)
				return ReadUint(2);
			if (b0 == 29)
				return ReadUint(4);
			return 0;
		}

		public void SkipCffOperand()
		{
			var v = 0;
			int b0 = PeekByte();
			if (b0 == 30)
			{
				Skip(1);
				while (Cursor < Size)
				{
					v = ReadByte();
					if ((v & 0xF) == 0xF || v >> 4 == 0xF)
						break;
				}
			}
			else
			{
				ReadCffUint();
			}
		}

		public Buffer ReadCffDict(int key)
		{
			Seek(0);
			while (Cursor < Size)
			{
				var start = Cursor;
				var end = 0;
				var op = 0;
				while (PeekByte() >= 28) SkipCffOperand();

				end = Cursor;
				op = ReadByte();
				if (op == 12)
					op = ReadByte() | 0x100;
				if (op == key)
					return CreateRange(start, end - start);
			}

			return CreateRange(0, 0);
		}

		public void ReadCffDictUints(int key, int outcount, uint[] _out_)
		{
			var i = 0;
			var operands = ReadCffDict(key);
			for (i = 0; i < outcount && operands.Cursor < operands.Size; i++)
			{
				_out_[i] = operands.ReadCffUint();
			}
		}

		public bool ReadCffDictUint(int key, ref uint _out_)
		{
			var operands = ReadCffDict(key);
			if (operands.Cursor < operands.Size)
			{
				_out_ = operands.ReadCffUint();
				return true;
			}

			return false;
		}

		public static Buffer ReadCffIndexBy(Buffer b, int i)
		{
			var count = 0;
			var offsize = 0;
			var start = 0;
			var end = 0;
			b.Seek(0);
			count = (int)b.ReadUint(2);
			offsize = b.ReadByte();
			b.Skip(i * offsize);
			start = (int)b.ReadUint(offsize);
			end = (int)b.ReadUint(offsize);
			return b.CreateRange(2 + (count + 1) * offsize + start, end - start);
		}

		public static Buffer ReadCffSubrBy(Buffer idx, int n)
		{
			var count = idx.ReadCffIndexCount();
			var bias = 107;
			if (count >= 33900)
				bias = 32768;
			else if (count >= 1240)
				bias = 1131;
			n += bias;
			if (n < 0 || n >= count)
				return Null;
			return ReadCffIndexBy(idx, n);
		}

		public static Buffer ReadCffSubrs(Buffer cff, Buffer fontdict)
		{
			var private_loc = new uint[] { 0, 0 };
			fontdict.ReadCffDictUints(18, 2, private_loc);
			if (private_loc[1] == 0 || private_loc[0] == 0)
				return Null;
			var pdict = cff.CreateRange((int)private_loc[1], (int)private_loc[0]);
			uint subrsoff = 0;
			if (!pdict.ReadCffDictUint(19, ref subrsoff))
			{
				return Null;
			}

			cff.Seek((int)(private_loc[1] + subrsoff));
			return cff.ReadCffIndex();
		}
	}
}
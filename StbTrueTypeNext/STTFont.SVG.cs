namespace StbTrueTypeNext
{
	partial class STTFont
	{
		public int stbtt_GetCodepointSVG(int unicode_codepoint, ref FakePtr<byte> svg)
		{
			return stbtt_GetGlyphSVG(stbtt_FindGlyphIndex(unicode_codepoint), ref svg);
		}

		public int stbtt_GetGlyphSVG(int gl, ref FakePtr<byte> svg)
		{
			var data = new FakePtr<byte>(_data);
			if (_svg == 0)
				return 0;
			var svg_doc = stbtt_FindSVGDoc(gl);
			if (!svg_doc.IsNull)
			{
				svg = data + _svg + Utility.ttULONG(svg_doc + 4);
				return (int)Utility.ttULONG(svg_doc + 8);
			}

			return 0;
		}

		private FakePtr<byte> stbtt_FindSVGDoc(int gl)
		{
			var i = 0;
			var data = new FakePtr<byte>(_data);
			var svg_doc_list = data + stbtt__get_svg();
			int numEntries = Utility.ttUSHORT(svg_doc_list);
			var svg_docs = svg_doc_list + 2;
			for (i = 0; i < numEntries; i++)
			{
				var svg_doc = svg_docs + 12 * i;
				if (gl >= Utility.ttUSHORT(svg_doc) && gl <= Utility.ttUSHORT(svg_doc + 2))
					return svg_doc;
			}

			return FakePtr<byte>.Null;
		}

		private int stbtt__get_svg()
		{
			uint t = 0;

			var dataPtr = new FakePtr<byte>(_data);
			if (_svg < 0)
			{
				t = Utility.stbtt__find_table(dataPtr, (uint)_fontstart, "SVG ");
				if (t != 0)
				{
					var offset = Utility.ttULONG(dataPtr + t + 2);
					_svg = (int)(t + offset);
				}
				else
				{
					_svg = 0;
				}
			}

			return _svg;
		}
	}
}

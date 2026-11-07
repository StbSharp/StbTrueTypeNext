namespace StbTrueTypeNext
{
	partial class STBFont
	{
		public int GetCodepointKernAdvance(int ch1, int ch2)
		{
			if (_kern == 0 && _gpos == 0)
				return 0;
			return GetGlyphKernAdvance(FindGlyphIndex(ch1), FindGlyphIndex(ch2));
		}

		public int GetGlyphKernAdvance(int g1, int g2)
		{
			var xAdvance = 0;
			if (_gpos != 0)
			{
				xAdvance += GetGlyphGPOSInfoAdvance(g1, g2);
			}
			else if (_kern != 0)
			{
				xAdvance += GetGlyphKernInfoAdvance(g1, g2);
			}
			return xAdvance;
		}

		public STBKerningEntry[] GetKerningTable()
		{
			var data = new FakePtr<byte>(_data, _kern);
			var k = 0;
			var length = 0;
			if (_kern == 0)
				return null;
			if (Utility.ttUSHORT(data + 2) < 1)
				return null;
			if (Utility.ttUSHORT(data + 8) != 1)
				return null;
			length = Utility.ttUSHORT(data + 10);

			var table = new STBKerningEntry[length];
			for (k = 0; k < length; k++)
			{
				table[k].Glyph1 = Utility.ttUSHORT(data + 18 + k * 6);
				table[k].Glyph2 = Utility.ttUSHORT(data + 20 + k * 6);
				table[k].Advance = Utility.ttSHORT(data + 22 + k * 6);
			}

			return table;
		}

		private int GetGlyphGPOSInfoAdvance(int glyph1, int glyph2)
		{
			ushort lookupListOffset = 0;
			ushort lookupCount = 0;
			var i = 0;
			var sti = 0;
			if (_gpos == 0)
				return 0;

			var data = new FakePtr<byte>(_data, _gpos);
			if (Utility.ttUSHORT(data + 0) != 1)
				return 0;
			if (Utility.ttUSHORT(data + 2) != 0)
				return 0;
			lookupListOffset = Utility.ttUSHORT(data + 8);
			var lookupList = data + lookupListOffset;
			lookupCount = Utility.ttUSHORT(lookupList);
			for (i = 0; i < lookupCount; ++i)
			{
				var lookupOffset = Utility.ttUSHORT(lookupList + 2 + 2 * i);
				var lookupTable = lookupList + lookupOffset;
				var lookupType = Utility.ttUSHORT(lookupTable);
				var subTableCount = Utility.ttUSHORT(lookupTable + 4);
				var subTableOffsets = lookupTable + 6;
				if (lookupType != 2)
					continue;
				for (sti = 0; sti < subTableCount; sti++)
				{
					var subtableOffset = Utility.ttUSHORT(subTableOffsets + 2 * sti);
					var table = lookupTable + subtableOffset;
					var posFormat = Utility.ttUSHORT(table);
					var coverageOffset = Utility.ttUSHORT(table + 2);
					var coverageIndex = GetCoverageIndex(table + coverageOffset, glyph1);
					if (coverageIndex == -1)
						continue;
					switch (posFormat)
					{
						case 1:
							{
								var l = 0;
								var r = 0;
								var m = 0;
								var straw = 0;
								var needle = 0;
								var valueFormat1 = Utility.ttUSHORT(table + 4);
								var valueFormat2 = Utility.ttUSHORT(table + 6);
								if (valueFormat1 == 4 && valueFormat2 == 0)
								{
									var valueRecordPairSizeInBytes = 2;
									var pairSetCount = Utility.ttUSHORT(table + 8);
									var pairPosOffset = Utility.ttUSHORT(table + 10 + 2 * coverageIndex);
									var pairValueTable = table + pairPosOffset;
									var pairValueCount = Utility.ttUSHORT(pairValueTable);
									var pairValueArray = pairValueTable + 2;
									if (coverageIndex >= pairSetCount)
										return 0;
									needle = glyph2;
									r = pairValueCount - 1;
									l = 0;
									while (l <= r)
									{
										ushort secondGlyph = 0;
										m = (l + r) >> 1;
										var pairValue = pairValueArray + (2 + valueRecordPairSizeInBytes) * m;
										secondGlyph = Utility.ttUSHORT(pairValue);
										straw = secondGlyph;
										if (needle < straw)
										{
											r = m - 1;
										}
										else if (needle > straw)
										{
											l = m + 1;
										}
										else
										{
											var xAdvance = Utility.ttSHORT(pairValue + 2);
											return xAdvance;
										}
									}
								}
								else
								{
									return 0;
								}

								break;
							}

						case 2:
							{
								var valueFormat1 = Utility.ttUSHORT(table + 4);
								var valueFormat2 = Utility.ttUSHORT(table + 6);
								if (valueFormat1 == 4 && valueFormat2 == 0)
								{
									var classDef1Offset = Utility.ttUSHORT(table + 8);
									var classDef2Offset = Utility.ttUSHORT(table + 10);
									var glyph1class = GetGlyphClass(table + classDef1Offset, glyph1);
									var glyph2class = GetGlyphClass(table + classDef2Offset, glyph2);
									var class1Count = Utility.ttUSHORT(table + 12);
									var class2Count = Utility.ttUSHORT(table + 14);
									short xAdvance = 0;
									if (glyph1class < 0 || glyph1class >= class1Count)
										return 0;
									if (glyph2class < 0 || glyph2class >= class2Count)
										return 0;
									var class1Records = table + 16;
									var class2Records = class1Records + 2 * glyph1class * class2Count;
									xAdvance = Utility.ttSHORT(class2Records + 2 * glyph2class);
									return xAdvance;
								}

								return 0;
							}

						default:
							return 0;
					}
				}
			}

			return 0;
		}

		private int GetGlyphKernInfoAdvance(int glyph1, int glyph2)
		{
			var data = new FakePtr<byte>(_data, _kern);
			uint needle = 0;
			uint straw = 0;
			var l = 0;
			var r = 0;
			var m = 0;
			if (_kern == 0)
				return 0;
			if (Utility.ttUSHORT(data + 2) < 1)
				return 0;
			if (Utility.ttUSHORT(data + 8) != 1)
				return 0;
			l = 0;
			r = Utility.ttUSHORT(data + 10) - 1;
			needle = (uint)((glyph1 << 16) | glyph2);
			while (l <= r)
			{
				m = (l + r) >> 1;
				straw = Utility.ttULONG(data + 18 + m * 6);
				if (needle < straw)
					r = m - 1;
				else if (needle > straw)
					l = m + 1;
				else
					return Utility.ttSHORT(data + 22 + m * 6);
			}

			return 0;
		}

		private static int GetCoverageIndex(FakePtr<byte> coverageTable, int glyph)
		{
			var coverageFormat = Utility.ttUSHORT(coverageTable);
			switch (coverageFormat)
			{
				case 1:
					{
						var glyphCount = Utility.ttUSHORT(coverageTable + 2);
						var l = 0;
						var r = glyphCount - 1;
						var m = 0;
						var straw = 0;
						var needle = glyph;
						while (l <= r)
						{
							var glyphArray = coverageTable + 4;
							ushort glyphID = 0;
							m = (l + r) >> 1;
							glyphID = Utility.ttUSHORT(glyphArray + 2 * m);
							straw = glyphID;
							if (needle < straw)
								r = m - 1;
							else if (needle > straw)
								l = m + 1;
							else
								return m;
						}

						break;
					}

				case 2:
					{
						var rangeCount = Utility.ttUSHORT(coverageTable + 2);
						var rangeArray = coverageTable + 4;
						var l = 0;
						var r = rangeCount - 1;
						var m = 0;
						var strawStart = 0;
						var strawEnd = 0;
						var needle = glyph;
						while (l <= r)
						{
							m = (l + r) >> 1;
							var rangeRecord = rangeArray + 6 * m;
							strawStart = Utility.ttUSHORT(rangeRecord);
							strawEnd = Utility.ttUSHORT(rangeRecord + 2);
							if (needle < strawStart)
							{
								r = m - 1;
							}
							else if (needle > strawEnd)
							{
								l = m + 1;
							}
							else
							{
								var startCoverageIndex = Utility.ttUSHORT(rangeRecord + 4);
								return startCoverageIndex + glyph - strawStart;
							}
						}

						break;
					}

				default:
					return -1;
			}

			return -1;
		}

		private static int GetGlyphClass(FakePtr<byte> classDefTable, int glyph)
		{
			var classDefFormat = Utility.ttUSHORT(classDefTable);
			switch (classDefFormat)
			{
				case 1:
					{
						var startGlyphID = Utility.ttUSHORT(classDefTable + 2);
						var glyphCount = Utility.ttUSHORT(classDefTable + 4);
						var classDef1ValueArray = classDefTable + 6;
						if (glyph >= startGlyphID && glyph < startGlyphID + glyphCount)
							return Utility.ttUSHORT(classDef1ValueArray + 2 * (glyph - startGlyphID));
						break;
					}

				case 2:
					{
						var classRangeCount = Utility.ttUSHORT(classDefTable + 2);
						var classRangeRecords = classDefTable + 4;
						var l = 0;
						var r = classRangeCount - 1;
						var m = 0;
						var strawStart = 0;
						var strawEnd = 0;
						var needle = glyph;
						while (l <= r)
						{
							m = (l + r) >> 1;
							var classRangeRecord = classRangeRecords + 6 * m;
							strawStart = Utility.ttUSHORT(classRangeRecord);
							strawEnd = Utility.ttUSHORT(classRangeRecord + 2);
							if (needle < strawStart)
								r = m - 1;
							else if (needle > strawEnd)
								l = m + 1;
							else
								return Utility.ttUSHORT(classRangeRecord + 4);
						}

						break;
					}

				default:
					return -1;
			}

			return 0;
		}
	}
}

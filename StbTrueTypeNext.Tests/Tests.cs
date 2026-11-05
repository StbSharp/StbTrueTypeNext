using System;
using System.IO;
using System.Linq;
using System.Reflection;
using StbTrueTypeNext.Tests.Utility;
using static StbTrueTypeNext.stbtt_fontinfo;
using Xunit;
using Xunit.Sdk;
using FontsList = System.Collections.Generic.List<StbTrueTypeNext.stbtt_fontinfo>;

namespace StbTrueTypeNext.Tests
{
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		/// <summary>
		///     Makes sure Exception is thrown if font file lacks index map
		/// </summary>
		[Fact]
		public void TestNoIndexMap()
		{
			var ttfData = File.ReadAllBytes(@"C:\Windows\Fonts\webdings.ttf");
			Assert.Throws<Exception>(() =>
			{
				var fontInfo = stbtt_fontinfo.FromMemory(ttfData);
			});
		}

		[Fact]
		public void TestCreationAndDispose()
		{
			var ttfData = _assembly.ReadResourceAsBytes("DroidSans.ttf");
			var fontInfo = stbtt_fontinfo.FromMemory(ttfData);
			Assert.NotNull(fontInfo);
		}

		[Fact]
		public void TestLoadFontCollection()
		{
			string fontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
			string someTtcPath = Directory.EnumerateFiles(fontsPath, "*.ttc", new EnumerationOptions()
			{
				AttributesToSkip = FileAttributes.Directory,
				MatchCasing = MatchCasing.CaseInsensitive,
				MatchType = MatchType.Simple,
				RecurseSubdirectories = false,
				IgnoreInaccessible = true,
				ReturnSpecialDirectories = false
			}).FirstOrDefault();

			if (someTtcPath == null)
				throw new SkipException("You don't have a ttc font installed on your computer, but this test requires it.");

			byte[] ttcContent = File.ReadAllBytes(someTtcPath);
			// Assert.NotNull(someTtc);
			FontsList fonts;
			int numberOfFonts;

			numberOfFonts = stbtt_GetNumberOfFonts(ttcContent);
			fonts = new FontsList(numberOfFonts);
			for (int i = 0; i < numberOfFonts; i++)
			{
				int offset = stbtt_GetFontOffsetForIndex(ttcContent, i);
				fonts.Add(stbtt_fontinfo.FromMemory(ttcContent, offset));
			}

			Assert.Equal(numberOfFonts, fonts.Count);
		}

		private void TestRasterize(stbtt_fontinfo fontInfo, string text, float size)
		{
			int iascent, idescent, ilineGap;
			fontInfo.stbtt_GetFontVMetrics(out iascent, out idescent, out ilineGap);

			var scale = stbtt_ScaleForPixelHeight(fontInfo, 32.0f);
			var ascent = iascent * scale;
			var descent = idescent * scale;
			var lineGap = ilineGap * scale;

			var lineHeight = ascent - descent + lineGap;

			Assert.True(lineHeight.EpsilonEquals(32.0f));

			for (var i = 0; i < text.Length; ++i)
			{
				var c = text[i];

				if (char.IsWhiteSpace(c))
				{
					continue;
				}

				var glyphId = fontInfo.stbtt_FindGlyphIndex(c);
				Assert.NotEqual(0, glyphId);

				int advanceWidth, leftSideBearing;
				stbtt_GetGlyphHMetrics(fontInfo, glyphId, out advanceWidth, out leftSideBearing);

				int x0, y0, x1, y1;
				fontInfo.stbtt_GetGlyphBitmapBox(glyphId, scale, scale, out x0, out y0, out x1, out y1);

				var width = x1 - x0;
				var height = y1 - y0;

				Assert.NotEqual(0, width);
				Assert.NotEqual(0, height);
				var data = new byte[width * height];

				stbtt_MakeGlyphBitmap(fontInfo, data, width, height, width, scale, scale, glyphId);
			}
		}

		[Fact]
		public void TestNewRasterizer()
		{
			var ttfData = _assembly.ReadResourceAsBytes("DroidSans.ttf");
			var fontInfo = stbtt_fontinfo.FromMemory(ttfData);
			Assert.NotNull(fontInfo);

			TestRasterize(fontInfo, "Hello, World!", 32.0f);
		}

		[Fact]
		public void TestOldRasterizer()
		{
			var ttfData = _assembly.ReadResourceAsBytes("DroidSans.ttf");
			var fontInfo = stbtt_fontinfo.FromMemory(ttfData);
			Assert.NotNull(fontInfo);

			fontInfo.RasterizerVersion = RasterizerVersion.V1;

			TestRasterize(fontInfo, "Hello, World!", 32.0f);
		}
	}
}
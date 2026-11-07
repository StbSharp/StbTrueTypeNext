using System;
using System.IO;
using System.Linq;
using System.Reflection;
using StbTrueTypeNext.Tests.Utility;
using Xunit;
using Xunit.Sdk;
using FontsList = System.Collections.Generic.List<StbTrueTypeNext.STBFont>;

namespace StbTrueTypeNext.Tests
{
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		[Fact]
		public void TestNoIndexMap()
		{
			var ttfData = File.ReadAllBytes(@"C:\Windows\Fonts\webdings.ttf");
			Assert.Throws<Exception>(() =>
			{
				var fontInfo = new STBFont(ttfData);
			});
		}

		[Fact]
		public void TestTtf()
		{
			var ttfData = _assembly.ReadResourceAsBytes("DroidSans.ttf");
			var fontInfo = new STBFont(ttfData);
		}

		[Fact]
		public void TestOtf()
		{
			var ttfData = _assembly.ReadResourceAsBytes("SourceSansPro-Regular.otf");
			var fontInfo = new STBFont(ttfData);
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
			FontsList fonts;
			int numberOfFonts;

			numberOfFonts = STBFontCollection.GetNumberOfFonts(ttcContent);
			fonts = new FontsList(numberOfFonts);
			for (int i = 0; i < numberOfFonts; i++)
			{
				int offset = STBFontCollection.GetFontOffsetForIndex(ttcContent, i);
				fonts.Add(new STBFont(ttcContent, offset));
			}

			Assert.Equal(numberOfFonts, fonts.Count);
		}

		private void TestRasterize(STBFont fontInfo, string text, float size)
		{
			int iascent, idescent, ilineGap;
			fontInfo.GetFontVMetrics(out iascent, out idescent, out ilineGap);

			var scale = fontInfo.ScaleForPixelHeight(32.0f);
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

				var glyphId = fontInfo.FindGlyphIndex(c);
				Assert.NotEqual(0, glyphId);

				int advanceWidth, leftSideBearing;
				fontInfo.GetGlyphHMetrics(glyphId, out advanceWidth, out leftSideBearing);

				var box = fontInfo.GetGlyphBitmapBoxSubpixel(glyphId, scale, scale) ?? STBGlyphBitmapBox.Zero;

				Assert.NotEqual(0, box.Width);
				Assert.NotEqual(0, box.Height);
				var data = new byte[box.Width * box.Height];

				fontInfo.MakeGlyphBitmap(data, 0, box.Width, box.Height, box.Width, scale, scale, glyphId);
			}
		}

		[Fact]
		public void TestNewRasterizer()
		{
			var ttfData = _assembly.ReadResourceAsBytes("DroidSans.ttf");
			var fontInfo = new STBFont(ttfData);
			Assert.NotNull(fontInfo);

			TestRasterize(fontInfo, "Hello, World!", 32.0f);
		}

		[Fact]
		public void TestOldRasterizer()
		{
			var ttfData = _assembly.ReadResourceAsBytes("DroidSans.ttf");
			var fontInfo = new STBFont(ttfData);
			Assert.NotNull(fontInfo);

			fontInfo.RasterizerVersion = STBRasterizerVersion.V1;

			TestRasterize(fontInfo, "Hello, World!", 32.0f);
		}
	}
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StbTrueTypeNext.Samples.MonoGameTest;

internal static class Utility
{
	public static SpriteFont CreateFont(GraphicsDevice device, FontBakerResult charData)
	{
		// Offset by minimal offset
		var minimumOffsetY = 10000;
		foreach (var pair in charData.Glyphs)
			if (pair.Value.YOffset < minimumOffsetY)
				minimumOffsetY = pair.Value.YOffset;

		var keys = charData.Glyphs.Keys.ToArray();
		foreach (var key in keys)
		{
			var pc = charData.Glyphs[key];
			pc.YOffset -= minimumOffsetY;
			charData.Glyphs[key] = pc;
		}

		var rgb = new Color[charData.Width * charData.Height];
		for (var i = 0; i < charData.Bitmap.Length; ++i)
		{
			var b = charData.Bitmap[i];
			rgb[i].R = b;
			rgb[i].G = b;
			rgb[i].B = b;

			rgb[i].A = b;
		}

		var fontTexture = new Texture2D(device, charData.Width, charData.Height);
		fontTexture.SetData(rgb);

		var glyphBounds = new List<Rectangle>();
		var cropping = new List<Rectangle>();
		var chars = new List<char>();
		var kerning = new List<Vector3>();

		var orderedKeys = charData.Glyphs.Keys.OrderBy(a => a);
		foreach (var key in orderedKeys)
		{
			var character = charData.Glyphs[key];

			var bounds = new Rectangle(character.X, character.Y,
				character.Width,
				character.Height);

			glyphBounds.Add(bounds);
			cropping.Add(new Rectangle(character.XOffset, character.YOffset, bounds.Width, bounds.Height));

			chars.Add((char)key);

			kerning.Add(new Vector3(0, bounds.Width, character.XAdvance - bounds.Width));
		}

		var constructorInfo = typeof(SpriteFont).GetTypeInfo().DeclaredConstructors.First();
		return (SpriteFont)constructorInfo.Invoke(new object[]
		{
			fontTexture, glyphBounds, cropping,
			chars, 20, 0, kerning, ' '
		});
	}
}

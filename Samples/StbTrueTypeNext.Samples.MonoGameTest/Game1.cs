using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StbTrueTypeNext.Samples.MonoGameTest;

namespace StbTrueTypeNext.Samples;

/// <summary>
/// This is the main type for your game.
/// </summary>
public class Game1 : Game
{
	private const int FontBitmapWidth = 2048;
	private const int FontBitmapHeight = 2048;

	private SpriteFont _ttfFont;
	private SpriteFont _otfFont;

	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;

	public Game1()
	{
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1920,
			PreferredBackBufferHeight = 1080,
			SynchronizeWithVerticalRetrace = false
		};
		IsFixedTimeStep = false;

		Content.RootDirectory = "Content";
		IsMouseVisible = true;
		Window.AllowUserResizing = true;
	}

	private void LoadTtfFont()
	{
		var fontBaker = new FontBaker();

		fontBaker.Begin(FontBitmapWidth, FontBitmapHeight);
		fontBaker.Add(File.ReadAllBytes("Fonts/DroidSans.ttf"), 32, new[]
		{
			CharacterRange.BasicLatin,
			CharacterRange.Latin1Supplement,
			CharacterRange.LatinExtendedA,
			CharacterRange.Cyrillic,
			CharacterRange.Greek
		});

		fontBaker.Add(File.ReadAllBytes("Fonts/DroidSansJapanese.ttf"), 32, new[]
		{
			CharacterRange.Hiragana,
			CharacterRange.Katakana
		});

		fontBaker.Add(File.ReadAllBytes("Fonts/ZCOOLXiaoWei-Regular.ttf"), 32, new[]
		{
			CharacterRange.CjkSymbolsAndPunctuation,
			CharacterRange.CjkUnifiedIdeographs
		});

		fontBaker.Add(File.ReadAllBytes("Fonts/KoPubBatang-Regular.ttf"), 32, new[]
		{
			CharacterRange.HangulCompatibilityJamo,
			CharacterRange.HangulSyllables
		});

		var charData = fontBaker.End();
		_ttfFont = Utility.CreateFont(GraphicsDevice, charData);
	}

	private void LoadOtfFont()
	{
		var fontBaker = new FontBaker();

		fontBaker.Begin(FontBitmapWidth, FontBitmapHeight);
		fontBaker.Add(File.ReadAllBytes("Fonts/SourceSansPro-Regular.otf"), 32, new[]
		{
			CharacterRange.BasicLatin,
			CharacterRange.Latin1Supplement,
			CharacterRange.LatinExtendedA,
			CharacterRange.Cyrillic,
			CharacterRange.Greek
		});

		var charData = fontBaker.End();
		_otfFont = Utility.CreateFont(GraphicsDevice, charData);
	}

	/// <summary>
	///     LoadContent will be called once per game and is the place to load
	///     all of your content.
	/// </summary>
	protected override void LoadContent()
	{
		// Create a new SpriteBatch, which can be used to draw textures.
		_spriteBatch = new SpriteBatch(GraphicsDevice);

		// TODO: use this.Content to load your game content here
		// Create white texture

		LoadTtfFont();
		LoadOtfFont();

		GC.Collect();
	}

	/// <summary>
	///     UnloadContent will be called once per game and is the place to unload
	///     game-specific content.
	/// </summary>
	protected override void UnloadContent()
	{
		// TODO: Unload any non ContentManager content here
	}

	/// <summary>
	///     Allows the game to run logic such as updating the world,
	///     checking for collisions, gathering input, and playing audio.
	/// </summary>
	/// <param name="gameTime">Provides a snapshot of timing values.</param>
	protected override void Update(GameTime gameTime)
	{
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
			Keyboard.GetState().IsKeyDown(Keys.Escape))
			Exit();

		base.Update(gameTime);
	}

	/// <summary>
	///     This is called when the game should draw itself.
	/// </summary>
	/// <param name="gameTime">Provides a snapshot of timing values.</param>
	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.CornflowerBlue);

		_spriteBatch.Begin();

		// Draw alphabet for all common languages.
		_spriteBatch.DrawString(_ttfFont,
			"Eng: A a B b C c D d E e F f G g H h I i J j K k L l M m N n O o P p Q q R r S s T t U u V v W w X x Y y Z z",
			new Vector2(0, 0), Color.White);
		_spriteBatch.DrawString(_ttfFont,
			"Rus: А а, Б б, В в, Г г, Д д, Е е, Ё ё, Ж ж, З з, И и, Й й, К к, Л л, М м, Н н, О о, П п, Р р, С с, Т т, У у, Ф ф, Х х, Ц ц, Ч ч, Ш ш, Щ щ, Ъ ъ, Ы ы, Ь ь, Э э, Ю ю, Я я, І і, Ѳ ѳ, Ѣ ѣ, Ѵ ѵ",
			new Vector2(0, 30), Color.Maroon);
		_spriteBatch.DrawString(_ttfFont,
			"Scandinavian: Å å, Ø ø, Æ æ, œ, þ Fra: â ç è é ê î ô û ë ï ù á í ì ó ò ú, Romana: ă â î ș ț",
			new Vector2(0, 60), Color.Green);
		_spriteBatch.DrawString(_ttfFont, "Fra: â ç è é ê î ô û ë ï ù á í ì ó ò ú // Romana: ă â î ș ț",
			new Vector2(0, 90), Color.Navy);
		_spriteBatch.DrawString(_ttfFont,
			"Pol: Ą Ć Ę Ł Ń Ó Ś Ź Ż ą ć ę ł ń ó ś ź ż Zażółć gęślą jaźń, Prtgs: ã, õ, â, ê, ô, á, é, í, ó, ú, à, ç",
			new Vector2(0, 120), Color.Yellow);
		_spriteBatch.DrawString(_ttfFont,
			"Cze: ž š ů ě ř Příliš žluťoučký kůň úpěl ďábelské kódy, Lat/Lit: ā, č, ē, ģ, ī, ķ, ļ, ņ, ō, ŗ, š, ū, ž, ą, č, ę, ė, į, š, ų, ū, ž",
			new Vector2(0, 150), Color.Black);
		_spriteBatch.DrawString(_ttfFont,
			"Greek: Α α, Β β, Γ γ, Δ δ, Ε ε, Ζ ζ, Η η, Θ θ, Ι ι, Κ κ, Λ λ, Μ μ, Ν ν, Ξ ξ, Ο ο, Π π, Ρ ρ, Σ σ/ς, Τ τ, Υ υ, Φ φ, Χ χ, Ψ ψ, Ω ω ά έ ή ί ό ύ ώ",
			new Vector2(0, 180), Color.Aqua);
		_spriteBatch.DrawString(_ttfFont, "Jap: いろはにほ 。へどひらがなカタカナ, Kor: 한국어조선말, Cn: 国会这来对开关门时个书长万边东车爱儿。吾艾、贼德艾尺",
			new Vector2(0, 210), Color.Cyan);
		_spriteBatch.DrawString(_ttfFont,
			"Other symbols: Ñ ñ ¿ ¡ Ç ç á ê Ä ä à â Ö ö ô Ü ü ë ß ẞ Ÿ ÿ Œ Æ æ ï Ğ ğ Ş ş Ő ő Ű ű ù",
			new Vector2(0, 240), Color.Moccasin);

		_spriteBatch.DrawString(_otfFont,
			"Otf Eng: A a B b C c D d E e F f G g H h I i J j K k L l M m N n O o P p Q q R r S s T t U u V v W w X x Y y Z z",
			new Vector2(0, 270), Color.White);
		_spriteBatch.DrawString(_otfFont,
			"Otf Rus: А а, Б б, В в, Г г, Д д, Е е, Ё ё, Ж ж, З з, И и, Й й, К к, Л л, М м, Н н, О о, П п, Р р, С с, Т т, У у, Ф ф, Х х, Ц ц, Ч ч, Ш ш, Щ щ, Ъ ъ, Ы ы, Ь ь, Э э, Ю ю, Я я, І і, Ѳ ѳ, Ѣ ѣ, Ѵ ѵ",
			new Vector2(0, 300), Color.Maroon);
		_spriteBatch.DrawString(_otfFont,
			"Otf Scandinavian: Å å, Ø ø, Æ æ, œ, þ Fra: â ç è é ê î ô û ë ï ù á í ì ó ò ú, Romana: ă â î ș ț",
			new Vector2(0, 330), Color.Green);


		// _spriteBatch.Draw(_font.Texture, new Vector2(0, 300));

		_spriteBatch.End();

		base.Draw(gameTime);
	}
}
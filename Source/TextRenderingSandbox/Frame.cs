using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using StbSharp;
using static StbSharp.StbTrueType;

// TODO: sub/super-script text for most fonts
// https://forum.processing.org/two/discussion/6367/primitive-superscript-subscript-text-rendering

namespace TextRenderingSandbox
{
    class FontGlyphCache
    {
        private FontGlyphCacheRegion[] _regions;

        public int UnitWidth { get; }
        public int UnitHeight { get; }

        public int HorizontalUnitCount { get; }
        public int VerticalUnitCount { get; }
        public int UnitCount => HorizontalUnitCount * VerticalUnitCount;

        public int Width => UnitWidth * HorizontalUnitCount;
        public int Height => UnitHeight * VerticalUnitCount;

        public FontGlyphCache()
        {
            _regions = new FontGlyphCacheRegion[UnitCount];
        }

        
    }

    public class Frame : Game
    {
        private const int FontBitmapWidth = 4096;
        private const int FontBitmapHeight = 4096;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Effect _transparentAlpha8Effect;

        private SpriteFont _bitmapFont;

        private Texture2D _bitmapTex;
        
        private Stopwatch _watch = new Stopwatch();

        private FontGlyphCacheRegion _glyphCacheRegion = new FontGlyphCacheRegion(1024, 1024);
        private Texture2D _glyphCacheTexture;

        // FontGlyphCacheRegion - keeps track of packed glyphs
        // FontGlyphCache - contains multiple FontGlyphPackers in a square; can remove a font's glyphs and repack
        // FontGlyphCacheManager - manages multiple FontGlyphCaches; may repack caches to consolidate space

        public Frame()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1366 * 2;
            _graphics.PreferredBackBufferHeight = 768 * 2;
            Content.RootDirectory = "Content";

            IsFixedTimeStep = false;
        }

        protected override void Initialize()
        {
            base.Initialize();

            Window.AllowUserResizing = true;
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _droidSansBytes = File.ReadAllBytes("Fonts/DroidSans.ttf");
            _fontBaker = new FontBaker();

            _watch.Restart();
            LoadBitmapFont();
            _watch.Stop();
            Console.WriteLine("bitmap font load: " + Math.Round(_watch.Elapsed.TotalMilliseconds) + "ms");

            _watch.Restart();
            FontLoadTest1();
            _watch.Stop();
            Console.WriteLine(nameof(FontLoadTest1) + ": " + Math.Round(_watch.Elapsed.TotalMilliseconds) + "ms");

            _transparentAlpha8Effect = Content.Load<Effect>("TransparentAlpha8Effect");

            var systemFonts = SystemFonts.GetSystemFonts();
            Console.WriteLine("System font count: " + systemFonts.Count);

            //_ranges.Enqueue(CharacterRange.BasicLatin);
            //_ranges.Enqueue(CharacterRange.Latin1Supplement);
            //_ranges.Enqueue(CharacterRange.LatinExtendedA);
            //_ranges.Enqueue(CharacterRange.LatinExtendedB);
            //_ranges.Enqueue(CharacterRange.Cyrillic);
            //_ranges.Enqueue(CharacterRange.CyrillicSupplement);
            //_ranges.Enqueue(CharacterRange.Greek);

            //_ranges.Enqueue(CharacterRange.Hiragana);
            //_ranges.Enqueue(CharacterRange.Katakana);
            
            _ranges.Enqueue(CharacterRange.CjkSymbolsAndPunctuation);
            _ranges.Enqueue(CharacterRange.CjkUnifiedIdeographs);

            _charCodepoint = _ranges.Peek().Start;
        }

        private FontBaker _fontBaker;
        private byte[] _droidSansBytes;
        private TTFontInfo _droidSansFontInfo;
        private TTFontInfo _droidSansJapFontInfo;
        private TTFontInfo _ZCOOLXiaoWeiFontInfo;

        private void LoadBitmapFont()
        {
            _droidSansFontInfo = new TTFontInfo();
            StbTrueType.InitFont(_droidSansFontInfo, _droidSansBytes, 0);

            _droidSansJapFontInfo = new TTFontInfo();
            StbTrueType.InitFont(_droidSansJapFontInfo, File.ReadAllBytes("Fonts/DroidSansJapanese.ttf"), 0);

            _ZCOOLXiaoWeiFontInfo = new TTFontInfo();
            StbTrueType.InitFont(_ZCOOLXiaoWeiFontInfo, File.ReadAllBytes("Fonts/ZCOOLXiaoWei-Regular.ttf"), 0);

            _fontBaker.Begin(FontBitmapWidth, FontBitmapHeight);
            _fontBaker.Add(_droidSansBytes, 32, new[]
            {
                CharacterRange.BasicLatin,
                CharacterRange.Latin1Supplement,
                CharacterRange.LatinExtendedA,
                CharacterRange.Cyrillic,
                CharacterRange.Greek,
            });

            //_fontBaker.Add(File.ReadAllBytes("Fonts/DroidSansJapanese.ttf"), 32, new[]
            //{
            //    CharacterRange.Hiragana,
            //    CharacterRange.Katakana,
            //});
            //
            //_fontBaker.Add(File.ReadAllBytes("Fonts/ZCOOLXiaoWei-Regular.ttf"), 32, new[]
            //{
            //    CharacterRange.CjkSymbolsAndPunctuation,
            //    CharacterRange.CjkUnifiedIdeographs
            //});
            //
            //_fontBaker.Add(File.ReadAllBytes("Fonts/KoPubBatang-Regular.ttf"), 32, new[]
            //{
            //    CharacterRange.HangulCompatibilityJamo,
            //    CharacterRange.HangulSyllables
            //});

            var result = _fontBaker.End();

            Console.WriteLine("glyph count: " + result.Glyphs.Count);

            //SaveFontBitmap("bruh.png", result);

            var fontTexture = new Texture2D(
                GraphicsDevice, FontBitmapWidth, FontBitmapHeight, false, SurfaceFormat.Alpha8);
            var rgb = AlphaToRgb(result);
            fontTexture.SetData(result.Bitmap);

            _bitmapFont = CreateFont(fontTexture, result);
        }

        private unsafe void FontLoadTest1()
        {
            var fontInfo = new StbTrueType.TTFontInfo();
            if (!StbTrueType.InitFont(fontInfo, File.ReadAllBytes("Fonts/ZCOOLXiaoWei-Regular.ttf"), 0))
                throw new Exception("Failed to init font.");

            float fontPixelHeight = 32;
            var scale = StbTrueType.ScaleForPixelHeight(fontInfo, fontPixelHeight);

            int codepoint = '贼';

            //byte* bitmapPixels = StbTrueType.GetCodepointBitmap(
            //    fontInfo, scale, codepoint, out int bmpWidth, out int bmpHeight, out var bmpOffset);
            //
            //{
            //    _bitmapTex = new Texture2D(GraphicsDevice, bmpWidth, bmpHeight, false, SurfaceFormat.Alpha8);
            //    _bitmapTex.SetData(new Span<byte>(bitmapPixels, bmpWidth * bmpHeight).ToArray());
            //}

            var w = new Stopwatch();
            w.Restart();
            int execs = 1000 * 1;
            for (int zz = 0; zz < execs; zz++)
            {
                byte* pixelse = StbTrueType.GetCodepointBitmap(
                    fontInfo, scale, codepoint, out int widthe, out int heighte, out var offsete);

                StbTrueType.FreeBitmap(pixelse);
            }
            w.Stop();
            Console.WriteLine("Bitmap: Render time per char: " + Math.Round(w.Elapsed.TotalMilliseconds / execs, 2) + "ms");

            //StbTrueType.FreeBitmap(bitmapPixels);
        }

        private SpriteFont CreateFont(Texture2D texture, FontBakerResult result)
        {
            // Offset by minimal offset
            int minimumOffsetY = 10000;
            foreach (var pair in result.Glyphs)
                if (pair.Value.YOffset < minimumOffsetY)
                    minimumOffsetY = pair.Value.YOffset;

            var keys = result.Glyphs.Keys.ToArray();
            foreach (var key in keys)
            {
                var pc = result.Glyphs[key];
                pc.YOffset -= minimumOffsetY;
                result.Glyphs[key] = pc;
            }

            var glyphBounds = new List<Rectangle>();
            var cropping = new List<Rectangle>();
            var chars = new List<char>();
            var kerning = new List<Vector3>();

            var orderedKeys = result.Glyphs.Keys.OrderBy(a => a);
            foreach (int key in orderedKeys)
            //foreach (var pair in _charData.Glyphs)
            {
                var character = result.Glyphs[key];
                //var character = pair.Value;

                var bounds = new Rectangle(
                    character.X, character.Y, character.Width, character.Height);

                glyphBounds.Add(bounds);
                cropping.Add(new Rectangle(character.XOffset, character.YOffset, bounds.Width, bounds.Height));

                //chars.Add((char)pair.Key);
                chars.Add((char)key);

                kerning.Add(new Vector3(0, bounds.Width, character.XAdvance - bounds.Width));
            }

            return new SpriteFont(texture, glyphBounds, cropping, chars, 32, 0, kerning, ' ');
        }

        private Color[] AlphaToRgb(FontBakerResult result)
        {
            var rgba = new Color[result.Width * result.Height];
            for (var i = 0; i < rgba.Length; ++i)
            {
                byte b = result.Bitmap[i];
                rgba[i].R = 255;
                rgba[i].G = 255;
                rgba[i].B = 255;

                rgba[i].A = b;
            }
            return rgba;
        }

        private void SaveFontBitmap(string fileName, FontBakerResult result)
        {
            using (var bitmap = new System.Drawing.Bitmap(
                   FontBitmapWidth, FontBitmapHeight, System.Drawing.Imaging.PixelFormat.Format8bppIndexed))
            {
                var palette = bitmap.Palette;
                for (int i = 0; i < palette.Entries.Length; i++)
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, 255, 255, 255);
                bitmap.Palette = palette;

                var pixels = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                Marshal.Copy(result.Bitmap, 0, pixels.Scan0, result.Bitmap.Length);

                bitmap.UnlockBits(pixels);
                bitmap.Save(fileName);
            }
        }

        protected override void UnloadContent()
        {
        }

        private float _tick;
        private int _processed;
        private int _charCodepoint;

        Queue<CharacterRange> _ranges = new Queue<CharacterRange>();

        Random random = new Random();

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            var pp = _glyphCacheRegion._packer;

            int built = 0;
            for (int i = 0; i < 100; i++)
            {
                if (pp.FreeRectangles.Count > 100)
                    CompressPacker(pp);

                _tick += gameTime.GetElapsedSeconds();
                if (_tick > 0f && _ranges.Count > 0)
                {
                    _tick = 0;

                    TICK:
                    if (_charCodepoint < _ranges.Peek().End)
                    {
                        int glyph = StbTrueType.FindGlyphIndex(_ZCOOLXiaoWeiFontInfo, _charCodepoint);

                        bool succ = _glyphCacheRegion.GetGlyphRect(
                            _ZCOOLXiaoWeiFontInfo, glyph, 0, new TTIntPoint(1), 12, //random.Next(24, 128) / 2f,
                            out TTPoint scale, out Rect packedRect, out Rect charRect);

                        //if (packedRect.Width != 0 && packedRect.Height != 0 &&
                        //    (packedRect.Width < charRect.Width ||
                        //    packedRect.Height < charRect.Height))
                        //    throw new Exception();

                        //if (succ && packedRect.Width != 0 && packedRect.Height != 0)
                        //{
                        //    _glyphCacheRegion.DrawGlyph(_ZCOOLXiaoWeiFontInfo, glyph, scale, charRect);
                        //    built++;

                        // TODO: fix this, it needs to be redrawn after compress
                        //}

                        _processed++;
                        _charCodepoint++;

                        if (!succ)
                            goto TICK;
                    }
                    else
                    {
                        _ranges.Dequeue();
                        if (_ranges.Count > 0)
                            _charCodepoint = _ranges.Peek().Start;
                        else
                        {
                            //_graphics.IsFullScreen = true;
                            //_graphics.ApplyChanges();
                            //
                            //Draw(gameTime);
                            //
                            //using (var tex = new Texture2D(
                            //    GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height))
                            //{
                            //    Color[] colors = new Color[tex.Width * tex.Height];
                            //    GraphicsDevice.GetBackBufferData(colors);
                            //    tex.SetData(colors);
                            //
                            //    using (var fs = new FileStream("result.png", FileMode.Create))
                            //        tex.SaveAsPng(fs, tex.Width, tex.Height);
                            //}
                            //
                            //_graphics.IsFullScreen = false;
                            //_graphics.PreferredBackBufferWidth = 1200;
                            //_graphics.PreferredBackBufferHeight = 650;
                            //_graphics.ApplyChanges();

                            Console.WriteLine("Done");

                            CompressPacker(pp);
                        }
                    }
                }
            }

            if (built > 0)
            {
                if (_glyphCacheTexture == null)
                    _glyphCacheTexture = new Texture2D(
                        GraphicsDevice, pp.BinWidth, pp.BinHeight, false, SurfaceFormat.Alpha8);

                _glyphCacheTexture.SetData(_glyphCacheRegion._bitmap);
            }
        }

        private ListArray<Rect> tmpList = new ListArray<Rect>();

        private void CompressPacker(MaxRectsBinPack packer)
        {
            tmpList.AddRange(packer.UsedRectangles);
            tmpList.Sort((x, y) =>
            {
                int xA = x.Area;
                int yA = y.Area;
                if (xA < yA)
                    return -1;
                if (xA > yA)
                    return 1;
                return 0;
            });
            packer.Init(packer.BinWidth, packer.BinHeight, rotations: true);
         
            for (int i = 0; i < tmpList.Count; i++)
            {
                Rect rrr = tmpList[i];
                Rect repacked = packer.Insert(
                    rrr.Width, rrr.Height, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBottomLeftRule);

                if (repacked.Width == 0 || repacked.Height == 0)
                    throw new Exception("Compressing packer failed.");
            }
            tmpList.Clear();
        }

        //private Texture2D _tmpTexture;
        //private FontBaker _tmpBaker = new FontBaker();

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.SlateGray);

            if (true)
            {
                float scale = 1f; // (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 0.2f) + 1) / 2f * 5 + 0.1f;

                /*
                _spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: _transparentAlpha8Effect);

                var font = _bitmapFont;
                _spriteBatch.DrawString(font, "Now this is some epic quality :^D\n dont u dare disagree 123456789 {[]}",
                    new Vector2(0, 0), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

                _spriteBatch.End();
                */

                //_spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: _transparentAlpha8Effect);
                //_spriteBatch.Draw(
                //    _bitmapTex, new Vector2(200, 100), null, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                //_spriteBatch.End();
            }

            var mat = Matrix.CreateScale(0.66f);

            _spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: _transparentAlpha8Effect, transformMatrix: mat);

            var pack = _glyphCacheRegion._packer;

            //foreach (Rect free in pack.FreeRectangles)
            //    _spriteBatch.DrawRectangle(new RectangleF(free.X, free.Y, free.Width, free.Height), Color.Green, 1, 0);

            int u = 0; // Math.Max(0, pack.UsedRectangles.Count - 250);
            for (; u < pack.UsedRectangles.Count; u++)
            {
                var used = pack.UsedRectangles[u];
                _spriteBatch.FillRectangle(new RectangleF(used.X, used.Y, used.Width, used.Height), Color.Yellow, 0);
            }

            Console.WriteLine("P: " + _processed + " | Used: " + pack.UsedRectangles.Count + 
                " | Free: " + pack.FreeRectangles.Count + " | " +  Math.Round(pack.Occupancy() * 100) + "%");

            //_spriteBatch.DrawRectangle(new RectangleF(0, 0, pack.BinWidth, pack.BinHeight), Color.Red, 1, 0);

            //_spriteBatch.Draw(_glyphCacheTexture, new Vector2(0, 0), Color.White);

            if (false)
            {
                //float size = 24;
                //float size = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 0.25f) + 1) / 2f * 128;
                //
                //FontBakerResult result = default;
                //const int count = 1;
                //_watch.Reset();
                //for (int i = 0; i < count; i++)
                //{
                //    _tmpBaker.Begin(1024, 512);
                //
                //    Span<CharacterRange> ranges = stackalloc CharacterRange[1];
                //    ranges[0] = CharacterRange.BasicLatin;
                //
                //    _watch.Start();
                //    _tmpBaker.Add(_droidSansBytes, size, ranges);
                //
                //    result = _tmpBaker.End();
                //    _watch.Stop();
                //}
                // Console.WriteLine(Math.Round(_watch.Elapsed.TotalMilliseconds / count, 3) + "ms");

                //if (_tmpTexture == null ||
                //    _tmpTexture.Width != result.Width ||
                //    _tmpTexture.Height != result.Height)
                //    _tmpTexture = new Texture2D(GraphicsDevice, result.Width, result.Height);
                //
                //var rgba = AlphaToRgb(result);
                //_tmpTexture.SetData(rgba);

                //SaveFontBitmap("wtf.png", result);

                //_spriteBatch.Draw(_tmpTexture, new Vector2(10, 10), Color.Black);
            }

            if (false)
            {
                float offset = 120;

                // Draw alphabet for all common languages.

                float mul = 40;
                _spriteBatch.DrawString(_bitmapFont, "Eng: A a B b C c D d E e F f G g H h I i J j K k L l M m N n O o P p Q q R r S s T t U u V v W w X x Y y Z z",
                    new Vector2(0, offset += 0), Color.White);
                _spriteBatch.DrawString(_bitmapFont, "Rus: А а, Б б, В в, Г г, Д д, Е е, Ё ё, Ж ж, З з, И и, Й й, К к, Л л, М м, Н н, О о, П п, Р р, С с, Т т, У у, \n Ф ф, Х х, Ц ц, Ч ч, Ш ш, Щ щ, Ъ ъ, Ы ы, Ь ь, Э э, Ю ю, Я я, І і, Ѳ ѳ, Ѣ ѣ, Ѵ ѵ",
                    new Vector2(0, offset += mul), Color.Maroon);
                _spriteBatch.DrawString(_bitmapFont, "Scandinavian: Å å, Ø ø, Æ æ, œ, þ Fra: â ç è é ê î ô û ë ï ù á í ì ó ò ú, Romana: ă â î ș ț",
                    new Vector2(0, offset += mul * 2), Color.LimeGreen);
                _spriteBatch.DrawString(_bitmapFont, "Fra: â ç è é ê î ô û ë ï ù á í ì ó ò ú // Romana: ă â î ș ț",
                    new Vector2(0, offset += mul), Color.Navy);
                _spriteBatch.DrawString(_bitmapFont, "Pol: Ą Ć Ę Ł Ń Ó Ś Ź Ż ą ć ę ł ń ó ś ź ż Zażółć gęślą jaźń, Prtgs: ã, õ, â, ê, ô, á, é, í, ó, ú, à, ç",
                    new Vector2(0, offset += mul), Color.Yellow);
                _spriteBatch.DrawString(_bitmapFont, "Cze: ž š ů ě ř Příliš žluťoučký kůň úpěl ďábelské kódy, \n Lat/Lit: ā, č, ē, ģ, ī, ķ, ļ, ņ, ō, ŗ, š, ū, ž, ą, č, ę, ė, į, š, ų, ū, ž",
                    new Vector2(0, offset += mul), Color.Black);
                _spriteBatch.DrawString(_bitmapFont, "Greek: Α α, Β β, Γ γ, Δ δ, Ε ε, Ζ ζ, Η η, Θ θ, Ι ι, Κ κ, Λ λ, Μ μ, Ν ν, Ξ ξ, Ο ο, Π π, Ρ ρ, Σ σ/ς, Τ τ, Υ υ, \n Φ φ, Χ χ, Ψ ψ, Ω ω ά έ ή ί ό ύ ώ",
                    new Vector2(0, offset += mul * 2), Color.Aquamarine);
                _spriteBatch.DrawString(_bitmapFont, "Jap: いろはにほ 。へどひらがなカタカナ, Kor: 한국어조선말, \n Cn: 国会这来对开关门时个书长万边东车爱儿。吾艾、贼德艾尺",
                    new Vector2(0, offset += mul * 2), Color.Cyan);
                _spriteBatch.DrawString(_bitmapFont, "Other symbols: Ñ ñ ¿ ¡ Ç ç á ê Ä ä à â Ö ö ô Ü ü ë ß ẞ Ÿ ÿ Œ Æ æ ï Ğ ğ Ş ş Ő ő Ű ű ù",
                    new Vector2(0, offset += mul * 2), Color.Moccasin);

                // _spriteBatch.Draw(_font.Texture, new Vector2(0, 300));
            }

            _spriteBatch.End();
        }
    }
}
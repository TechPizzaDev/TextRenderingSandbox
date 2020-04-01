using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace TextRenderingSandbox
{
    /// <summary>
    /// Provides simple information about a font.
    /// </summary>
    public readonly struct FontDescription
    {
        /// <summary>
        /// Gets the font style.
        /// </summary>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the font family.
        /// </summary>
        public string Family { get; }

        /// <summary>
        /// Gets the font sub family.
        /// </summary>
        public string SubFamily { get; }

        public FontDescription(string name, string family, string subFamily, FontStyle style)
        {
            Name = name;
            Family = family;
            SubFamily = subFamily;
            Style = style;
        }
    }

    public interface IFont
    {
        FontDescription Description { get; }

        short Ascender { get; }
        short Descender { get; }
        short LineGap { get; }
        ushort EmSize { get; }
        int LineHeight { get; }

        FontGlyph GetGlyph(int codePoint);
        Vector2 GetOffset(FontGlyph glyph, FontGlyph previousGlyph);
    }

    public class Font // : IFont
    {
        private Dictionary<int, FontGlyph> _glyphs;
        
        public static Font Load(Stream stream)
        {
            return null;
        }

        public static Font Load(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
                return Load(fs);
        }
    }
}

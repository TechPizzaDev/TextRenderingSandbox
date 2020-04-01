using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TextRenderingSandbox
{
    /// <summary>
    /// Defines a group of fonts with varied styles.
    /// </summary>
    public class FontFamily : IReadOnlyDictionary<FontStyle, IFont>
    {
        private Dictionary<FontStyle, IFont> _fonts;

        #region Properties

        public IFont this[FontStyle style] => _fonts[style];

        /// <summary>
        /// Gets the name of the font family.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the font styles in this family.
        /// </summary>
        public Dictionary<FontStyle, IFont>.KeyCollection Styles => _fonts.Keys;

        /// <summary>
        /// Gets the fonts in this family.
        /// </summary>
        public Dictionary<FontStyle, IFont>.ValueCollection Fonts => _fonts.Values;

        public FontStyle DefaultStyle => ContainsKey(FontStyle.Regular) ? FontStyle.Regular : Styles.First();

        IEnumerable<FontStyle> IReadOnlyDictionary<FontStyle, IFont>.Keys => _fonts.Keys;
        IEnumerable<IFont> IReadOnlyDictionary<FontStyle, IFont>.Values => _fonts.Values;

        /// <summary>
        /// Gets the number of fonts in this family.
        /// </summary>
        public int Count => _fonts.Count;

        #endregion

        public FontFamily(string name, IEnumerable<IFont> fonts)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (fonts == null) throw new ArgumentNullException(nameof(fonts));

            _fonts = new Dictionary<FontStyle, IFont>();
            foreach (var font in fonts)
                _fonts.Add(font.Description.Style, font);
        }

        /// <summary>
        /// Determines whether the <see cref="FontFamily"/> contains the specified <see cref="FontStyle"/>.
        /// </summary>
        public bool ContainsKey(FontStyle style) => _fonts.ContainsKey(style);

        /// <summary>
        /// Gets the <see cref="IFont"/> associated with the specified <see cref="FontStyle"/>.
        /// </summary>
        public bool TryGetValue(FontStyle style, out IFont font) => _fonts.TryGetValue(style, out font);

        /// <summary>
        /// Gets a string representation of this <see cref="FontFamily"/>.
        /// </summary>
        public override string ToString() => nameof(FontFamily) + ": \"" + Name + "\"";

        public Dictionary<FontStyle, IFont>.Enumerator GetEnumerator() => _fonts.GetEnumerator();
        IEnumerator<KeyValuePair<FontStyle, IFont>> IEnumerable<KeyValuePair<FontStyle, IFont>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

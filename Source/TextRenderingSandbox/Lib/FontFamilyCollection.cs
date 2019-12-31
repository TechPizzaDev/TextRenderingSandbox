using System.Collections;
using System.Collections.Generic;

namespace TextRenderingSandbox
{
    public class FontFamilyCollection : IReadOnlyDictionary<string, FontFamily>
    {
        private Dictionary<string, FontFamily> _families;

        public FontFamily this[string key] => _families[key];

        public int Count => _families.Count;

        public Dictionary<string, FontFamily>.KeyCollection Keys => _families.Keys;
        public Dictionary<string, FontFamily>.ValueCollection Values => _families.Values;

        IEnumerable<string> IReadOnlyDictionary<string, FontFamily>.Keys => _families.Keys;
        IEnumerable<FontFamily> IReadOnlyDictionary<string, FontFamily>.Values => _families.Values;

        public FontFamilyCollection()
        {
            _families = new Dictionary<string, FontFamily>();
        }

        public bool ContainsKey(string familyName) => _families.ContainsKey(familyName);
        public bool TryGetValue(string familyName, out FontFamily family) => _families.TryGetValue(familyName, out family);

        public Dictionary<string, FontFamily>.Enumerator GetEnumerator() => _families.GetEnumerator();
        IEnumerator<KeyValuePair<string, FontFamily>> IEnumerable<KeyValuePair<string, FontFamily>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}

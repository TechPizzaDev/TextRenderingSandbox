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

        public bool ContainsKey(string familyName)
        {
            return _families.ContainsKey(familyName);
        }

        public bool TryGetValue(string familyName, out FontFamily family)
        {
            return _families.TryGetValue(familyName, out family);
        }

        #region IEnumerator

        public Dictionary<string, FontFamily>.Enumerator GetEnumerator()
        {
            return _families.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, FontFamily>>
            IEnumerable<KeyValuePair<string, FontFamily>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}

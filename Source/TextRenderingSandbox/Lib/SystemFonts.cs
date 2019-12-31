using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace TextRenderingSandbox
{
    public class FontGlyph
    {

    }

    public static class SystemFonts
    {
        private static readonly string[] _paths = new[]
        {
            // Windows
            "%SYSTEMROOT%\\Fonts",

            // Linux
            "~/.fonts/",
            "/usr/local/share/fonts/",
            "/usr/share/fonts/",

            // Mac
            "~/Library/Fonts/",
            "/Library/Fonts/",
            "/Network/Library/Fonts/",
            "/System/Library/Fonts/",
            "/System Folder/Fonts/",
        };

        /// <summary>
        /// Gets directory paths that are searched for fonts.
        /// </summary>
        public static ReadOnlyMemory<string> Paths => _paths;

        public static FontFamilyCollection GetSystemFonts()
        {
            var expanded = _paths.Select(x => Environment.ExpandEnvironmentVariables(x));
            var found = expanded.Where(x => Directory.Exists(x));

            IEnumerable<string> files = found
                .SelectMany(x => Directory.EnumerateFiles(x, "*.*", SearchOption.AllDirectories))
                .Where(x =>
                {
                    string extension = Path.GetExtension(x);
                    return extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
                });

            var collection = new FontFamilyCollection();
            foreach (string file in files)
            {
                try
                {
                    var font = Font.LoadFrom(file);
                }
                catch
                {
                    // ignore exceptions, we don't know about file permissions etc.
                }
            }
            return collection;
        }
    }
}

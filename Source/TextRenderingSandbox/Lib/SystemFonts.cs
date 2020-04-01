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
        private static readonly string[] _staticPaths = new[]
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
        /// Gets static directory paths that are searched for fonts.
        /// </summary>
        public static ReadOnlyMemory<string> StaticPaths => _staticPaths;

        public static FontFamilyCollection GetSystemFonts()
        {
            IEnumerable<string> directories = _staticPaths
                .Select(x => Environment.ExpandEnvironmentVariables(x))
                .Append(Environment.GetFolderPath(Environment.SpecialFolder.Fonts))
                .Distinct()
                .Where(x => Directory.Exists(x));

            IEnumerable<string> files = directories
                .SelectMany(x =>
                {
                    try
                    {
                        return Directory.EnumerateFiles(x, "*.*", SearchOption.TopDirectoryOnly);
                    }
                    catch
                    {
                        // TODO: fix this (net core has some options for EnumerateFiles)
                        return null;
                    }
                })
                .Where(x =>
                {
                    if (x == null)
                        return false;

                    string extension = Path.GetExtension(x);
                    return extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
                });

            var collection = new FontFamilyCollection();
            foreach (string file in files)
            {
                try
                {
                    var font = Font.Load(file);
                }
                catch
                {
                    // ignore load exceptions, we don't know about file permissions etc.
                }
            }
            return collection;
        }
    }
}

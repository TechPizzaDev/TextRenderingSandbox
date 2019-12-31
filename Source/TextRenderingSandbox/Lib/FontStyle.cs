using System;

namespace TextRenderingSandbox
{
    [Flags]
    public enum FontStyle
    {
        Regular = 0,

        Bold = 1,

        Italic = 2,

        BoldItalic = Bold | Italic,

        // not yet supported
        // Underline = 4,
        // Strikethrough = 8
    }
}

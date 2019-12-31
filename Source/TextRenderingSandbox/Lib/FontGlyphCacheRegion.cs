using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StbSharp.StbTrueType;

namespace TextRenderingSandbox
{
    class FontGlyphCacheRegion
    {
        public MaxRectsBinPack _packer;// TODO: optimize packer (e.g sorting every 100 rects)
        public byte[] _bitmap; // TODO: change to MonoGame.Imaging.Image<Alpha8>

        public MaxRectsBinPack.FreeRectChoiceHeuristic PackMethod { get; set; } =
            MaxRectsBinPack.FreeRectChoiceHeuristic.RectBottomLeftRule;
        
        public FontGlyphCacheRegion(int width, int height)
        {
            _packer = new MaxRectsBinPack(width, height, rotations: true);
            _bitmap = new byte[width * height];
        }

        public void DrawGlyph(TTFontInfo fontInfo, int glyph, TTPoint scale, Rect charRect)
        {
            var pixelOffset = new TTIntPoint(charRect.X, charRect.Y);
            
            MakeGlyphBitmapSubpixel(
                fontInfo, _bitmap,
                charRect.Width, charRect.Height, _packer.BinWidth,
                scale, TTPoint.Zero, pixelOffset, glyph);
        }

        public bool GetGlyphRect(
            TTFontInfo fontInfo, int glyph, int padding,
            TTIntPoint oversample, float fontSize, 
            out TTPoint scale, out Rect packedRect, out Rect charRect)
        {
            scale = fontSize > 0
                ? ScaleForPixelHeight(fontInfo, fontSize)
                : ScaleForMappingEmToPixels(fontInfo, -fontSize);

            if (glyph == 0)
            {
                packedRect = default;
                charRect = default;
                return false;
            }
            else
            {
                GetGlyphBitmapBoxSubpixel(
                    fontInfo, glyph, scale * oversample, TTPoint.Zero, out var glyphBox);

                int w = glyphBox.w + padding + oversample.x - 1;
                int h = glyphBox.h + padding + oversample.y - 1;

                int rw = ToNextNearestMultiple(w, 2);
                int rh = ToNextNearestMultiple(h, 2);
                packedRect = _packer.Insert(rw, rh, PackMethod);
                charRect = new Rect(packedRect.X, packedRect.Y, w, h);

                return packedRect.Width != 0 && packedRect.Height != 0;
            }
        }

        int ToNextNearestMultiple(int value, int multiple)
        {
            return value / multiple * multiple + multiple;
        }

        int ToNextNearestPowOf2(int x)
        {
            if (x < 0)
                return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
    }
}

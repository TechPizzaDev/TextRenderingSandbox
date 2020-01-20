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

        private readonly int _width;
        private readonly int _height;
        public byte[] _bitmap; // TODO: change to MonoGame.Imaging.Image<Alpha8>

        private readonly int _bufferWidth;
        private readonly int _bufferHeight;
        private byte[] _bufferBitmap;

        public MaxRectsBinPack.FreeRectChoiceHeuristic PackMethod { get; set; } =
            MaxRectsBinPack.FreeRectChoiceHeuristic.BottomLeftRule;

        public FontGlyphCacheRegion(int width, int height)
        {
            _width = width;
            _height = height;
            _bufferWidth = 256;
            _bufferHeight = 256;

            _packer = new MaxRectsBinPack(_width, _height, rotations: true);
            _bitmap = new byte[_width * _height];
            _bufferBitmap = new byte[_bufferWidth * _bufferHeight];
        }

        public void DrawGlyph(TTFontInfo fontInfo, int glyph, TTPoint scale, Rect charRect, bool rotated)
        {
            int w = charRect.Width;
            int h = charRect.Height;

            MakeGlyphBitmapSubpixel(
                fontInfo,
                _bufferBitmap,
                w,
                h,
                out_stride: _bufferWidth,
                scale,
                shift: TTPoint.Zero,
                pixelOffset: TTIntPoint.Zero,
                glyph);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dst;
                    if (rotated)
                        dst = y + charRect.X + (x + charRect.Y) * _width;
                    else
                        dst = x + charRect.X + (y + charRect.Y) * _width;

                    int src = x + y * _bufferWidth;
                    _bitmap[dst] = _bufferBitmap[src];
                }
            }
        }

        #region GetGlyphRect

        public bool GetGlyphRect(
            TTFontInfo fontInfo, int glyph, int padding, TTIntPoint oversample, float fontSize,
            out TTPoint scale, out PackedRect packedRect, out Rect charRect)
        {
            scale = fontSize > 0
                ? ScaleForPixelHeight(fontInfo, fontSize)
                : ScaleForMappingEmToPixels(fontInfo, -fontSize);

            if (glyph != 0)
            {
                GetGlyphBitmapBoxSubpixel(
                    fontInfo, glyph, scale * oversample, TTPoint.Zero, out var glyphBox);

                if (glyphBox.w != 0 && glyphBox.h != 0)
                {
                    int w = glyphBox.w + padding + oversample.x - 1;
                    int h = glyphBox.h + padding + oversample.y - 1;

                    int rw = ToNextNearestMultiple(w, 2);
                    int rh = ToNextNearestMultiple(h, 2);

                    packedRect = _packer.Insert(rw, rh, PackMethod);
                    if (packedRect.Rect.Width != 0 && packedRect.Rect.Height != 0)
                    {
                        charRect = new Rect(packedRect.Rect.X, packedRect.Rect.Y, w, h);
                        return true;
                    }
                }
            }
            packedRect = default;
            charRect = default;
            return false;
        }

        public bool GetGlyphRect(
            TTFontInfo fontInfo, int glyph, int padding, float fontSize,
            out TTPoint scale, out PackedRect packedRect, out Rect charRect)
        {
            return GetGlyphRect(
                fontInfo, glyph, padding, new TTIntPoint(1), fontSize,
                out scale, out packedRect, out charRect);
        }

        #endregion

        int ToNextNearestMultiple(int value, int multiple)
        {
            int nearestMultiple = value / multiple * multiple;
            if (nearestMultiple < value)
                nearestMultiple += multiple;
            return nearestMultiple;
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

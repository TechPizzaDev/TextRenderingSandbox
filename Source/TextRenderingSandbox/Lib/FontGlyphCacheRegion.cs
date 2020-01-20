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
        public class RegionData
        {
            public int Width { get; }
            public int Height { get; }
            public byte[] Bitmap { get; } // TODO: change to MonoGame.Imaging.Image<Alpha8>

            public int Stride => Width;

            public RegionData(int width, int height)
            {
                Width = width;
                Height = height;
                Bitmap = new byte[Width * Height];
            }
        }

        public MaxRectsBinPack _packer;// TODO: optimize packer (e.g sorting every 100 rects)

        private RegionData _regionData;

        public MaxRectsBinPack.FreeRectChoiceHeuristic PackMethod { get; set; } =
            MaxRectsBinPack.FreeRectChoiceHeuristic.BottomLeftRule;

        public Memory<byte> RegionBitmap => _regionData.Bitmap;

        public FontGlyphCacheRegion(int width, int height)
        {
            _regionData = new RegionData(width, height);
            _packer = new MaxRectsBinPack(_regionData.Width, _regionData.Height, rotations: false);
        }

        public void DrawGlyph(TTFontInfo fontInfo, int glyph, TTPoint scale, Rect charRect)
        {
            int width = charRect.Width;
            int height = charRect.Height;
            var pixelOffset = new TTIntPoint(charRect.X, charRect.Y);

            MakeGlyphBitmapSubpixel(
                fontInfo,
                _regionData.Bitmap,
                width,
                height,
                out_stride: _regionData.Stride,
                scale,
                shift: TTPoint.Zero,
                pixelOffset,
                glyph);
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

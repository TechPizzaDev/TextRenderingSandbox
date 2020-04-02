using System;
using StbSharp;

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

        public RegionData _regionData; // TODO: make private

        public MaxRectsBinPack.FreeRectChoiceHeuristic PackMethod { get; set; } =
            MaxRectsBinPack.FreeRectChoiceHeuristic.BottomLeftRule;

        public Memory<byte> RegionBitmap => _regionData.Bitmap;

        public FontGlyphCacheRegion(int width, int height)
        {
            _regionData = new RegionData(width, height);
            _packer = new MaxRectsBinPack(_regionData.Width, _regionData.Height, rotations: false);
        }

        public void DrawGlyph(TrueType.FontInfo fontInfo, int glyph, TrueType.Point scale, Rect charRect)
        {
            int width = charRect.Width;
            int height = charRect.Height;
            var pixelOffset = new TrueType.IntPoint(charRect.X, charRect.Y);

            TrueType.MakeGlyphBitmapSubpixel(
                fontInfo,
                _regionData.Bitmap,
                width,
                height,
                out_stride: _regionData.Stride,
                scale,
                shift: TrueType.Point.Zero,
                pixelOffset,
                glyph);
        }

        #region GetGlyphRect

        public bool GetGlyphRect(
            TrueType.FontInfo fontInfo, 
            int glyph,
            int padding,
            TrueType.IntPoint oversample,
            float fontSize,
            out TrueType.Point scale, 
            out PackedRect packedRect, 
            out Rect charRect)
        {
            scale = fontSize > 0
                ? TrueType.ScaleForPixelHeight(fontInfo, fontSize)
                : TrueType.ScaleForMappingEmToPixels(fontInfo, -fontSize);

            if (glyph != 0)
            {
                TrueType.GetGlyphBitmapBoxSubpixel(
                    fontInfo, glyph, scale * oversample, TrueType.Point.Zero, out var glyphBox);

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
            TrueType.FontInfo fontInfo, int glyph, int padding, float fontSize,
            out TrueType.Point scale, out PackedRect packedRect, out Rect charRect)
        {
            return GetGlyphRect(
                fontInfo, glyph, padding, new TrueType.IntPoint(1), fontSize,
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Color = Microsoft.Xna.Framework.Color;
using DColor = System.Drawing.Color;

namespace TextRenderingSandbox
{
    public static class Program
    {
        public static int Components;
        public static int Width;
        public static Memory<Color> Pixels;

        [STAThread]
        private static unsafe void Main()
        {
            /*
            Components = 4;
            Width = 3;
            Pixels = new Color[]
            {
                Color.Red, Color.Green, Color.Blue,
                Color.HotPink, Color.Lime, Color.Aqua,
                Color.LightPink, Color.LightGreen, Color.LightBlue
            };

            using (var bmp = new Bitmap(Width, Width))
            {
                for (int y = 0; y < Width; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var c = Pixels.Span[x + y * Width];
                        bmp.SetPixel(x, y, DColor.FromArgb(c.A, c.R, c.G, c.B));
                    }
                }
                bmp.Save("yommo1.png");

                for (int y = 0; y < Width; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Span<byte> buffer = stackalloc byte[4];
                        Fill(buffer, (x + y * Width) * 4);

                        Span<Color> c = MemoryMarshal.Cast<byte, Color>(buffer);
                        bmp.SetPixel(x, y, DColor.FromArgb(c[0].A, c[0].R, c[0].G, c[0].B));
                    }
                }
                bmp.Save("yommo2.png");
            }
            */

            using (var game = new Frame())
                game.Run();
        }

        static Span<Color> GetPixelRowSpan(int y)
        {
            return Pixels.Span.Slice(y * Width, Width);
        }

        public static unsafe void Fill(Span<byte> buffer, int dataOffset)
        {
            int startPixelOffset = dataOffset / Components;
            int requestedPixelCount = (int)Math.Ceiling(buffer.Length / (double)Components);

            int offsetX = startPixelOffset % Width;
            int offsetY = startPixelOffset / Width;

            byte* castTmp = stackalloc byte[4];
            var rgbaSpan = new Span<Color>(castTmp, 1);

            // each iteration is supposed to read pixels from a single row at the time
            int bufferOffset = 0;
            int pixelsLeft = requestedPixelCount;
            while (pixelsLeft > 0)
            {
                int lastByteOffset = bufferOffset;
                int toRead = Math.Min(pixelsLeft, Width - offsetX);

                var srcRow = GetPixelRowSpan(offsetY);

                // some for-loops in the following cases use "toRead - 1" so
                // we can copy leftover bytes if the request length is irregular
                switch (Components)
                {
                    case 4:
                        for (int i = 0; i < toRead - 1; i++, bufferOffset += 4)
                        {
                            rgbaSpan[0] = srcRow[i + offsetX];
                            for (int j = 0; j < 4; j++)
                                buffer[j + bufferOffset] = castTmp[j];
                        }
                        rgbaSpan[0] = srcRow[offsetX + toRead - 1];
                        break;
                }

                // copy over the remaining bytes,
                // as the Fill() caller may request less bytes than sizeof(TPixel)
                int bytesRead = bufferOffset - lastByteOffset;
                int leftoverBytes = Math.Min(
                    Components, toRead * sizeof(Color) - bytesRead);

                for (int j = 0; j < leftoverBytes; j++)
                    buffer[j + bufferOffset] = castTmp[j];
                bufferOffset += leftoverBytes;

                // a case for code that copies bytes directly, 
                // not needing to copy leftovers
                ReadEnd:
                pixelsLeft -= toRead;

                offsetX = 0; // read from row beginning on next loop
                offsetY++; // and jump to the next row
            }
        }
    }
}

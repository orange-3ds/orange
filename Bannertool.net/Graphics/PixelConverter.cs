using System;

namespace Bannertool.Net.Graphics
{
    public enum PixelFormat
    {
        Rgba8,   // 32-bit, 4 bytes
        Bgr565,  // 16-bit, 2 bytes
    }

    public static class PixelConverter
    {
        /// <summary>
        /// Rearranges linear pixel data into 8x8 tiles for 3DS hardware.
        /// </summary>
        /// <param name="src">The source linear pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the data.</param>
        /// <returns>A new byte array containing the tiled pixel data.</returns>
        public static byte[] Tile(byte[] src, int width, int height, PixelFormat format)
        {
            int bytesPerPixel = GetBytesPerPixel(format);
            byte[] tiled = new byte[src.Length];
            int tileWidthInPixels = width / 8;

            for (int tileY = 0; tileY < height / 8; tileY++)
            {
                for (int tileX = 0; tileX < width / 8; tileX++)
                {
                    // Iterate through each pixel of the 8x8 tile
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            // Calculate the source index (from the linear source image)
                            int srcPixelX = tileX * 8 + x;
                            int srcPixelY = tileY * 8 + y;
                            int srcIndex = (srcPixelY * width + srcPixelX) * bytesPerPixel;

                            // Calculate the destination index (in the tiled output buffer)
                            // The destination is a sequence of 8x8 tiles written linearly one after the other.
                            int tileIndex = tileY * tileWidthInPixels + tileX;
                            int pixelInTileIndex = y * 8 + x;
                            int dstIndex = (tileIndex * 64 + pixelInTileIndex) * bytesPerPixel;

                            // Copy the pixel data (e.g., 2 bytes for BGR565, 4 for RGBA8)
                            Array.Copy(src, srcIndex, tiled, dstIndex, bytesPerPixel);
                        }
                    }
                }
            }
            return tiled;
        }

        private static int GetBytesPerPixel(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Rgba8: return 4;
                case PixelFormat.Bgr565: return 2;
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }
}
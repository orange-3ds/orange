using System.IO;
using System.Threading.Tasks;
using Bannertool.Net.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bannertool.Net.Graphics
{
    public static class Cgfx
    {
        private const uint CgfxMagic = 0x58464743; // "CGFX"
        private const uint DataMagic = 0x41544144; // "DATA"
        private const uint DictMagic = 0x54434944; // "DICT"
        private const uint ImgMagic = 0x474D49;    // "IMG"

        // Simplified CGFX structure for a single, uncompressed texture
        public static async Task<byte[]> FromPngAsync(Stream pngStream, int width, int height, PixelFormat format)
        {
            byte[] pixelData;
            // Load the image into a specific pixel format (Rgba32)
            using (var image = await Image.LoadAsync<Rgba32>(pngStream))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Sampler = KnownResamplers.Lanczos3
                }).Flip(FlipMode.Vertical)); // CGFX textures are often flipped

                if (format == PixelFormat.Rgba8)
                {
                    pixelData = new byte[width * height * 4];
                    // **FIXED**: Use the modern ImageSharp API to copy pixel data.
                    image.Frames.RootFrame.CopyPixelDataTo(pixelData);
                }
                else
                {
                    throw new System.NotSupportedException("Only RGBA8 is supported for banner CGFX.");
                }
            }

            var tiledData = PixelConverter.Tile(pixelData, width, height, format);

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // This is a highly simplified CGFX header. A full implementation is very complex.
                // This structure is based on what bannertool produces.
                writer.Write(CgfxMagic);
                writer.Write((ushort)0xFEFF); // BOM
                writer.Write((ushort)0x18);   // Header size
                writer.Write(0x00060000);     // Version
                writer.Write(0);             // Placeholder for file size
                writer.Write(1);             // Number of blocks (DATA)

                // --- DATA Block ---
                long dataBlockStart = ms.Position;
                writer.Write(DataMagic);
                writer.Write(0);             // Placeholder for block size
                writer.Write(DictMagic);     // Magic 1
                writer.Write(1);             // Entry count
                writer.Write(ImgMagic);      // Magic 2
                writer.Write((ushort)width);
                writer.Write((ushort)height);
                writer.Write(0x2800);        // Image format magic (RGBA8)
                writer.Write(tiledData);

                // Update sizes
                long fileSize = ms.Position;
                writer.Seek(0xC, SeekOrigin.Begin);
                writer.Write((uint)fileSize);
                writer.Seek((int)dataBlockStart + 4, SeekOrigin.Begin);
                writer.Write((uint)(fileSize - dataBlockStart));

                return ms.ToArray();
            }
        }
    }
}
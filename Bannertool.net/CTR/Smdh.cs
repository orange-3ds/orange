using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Bannertool.Net.Graphics;
using Bannertool.Net.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bannertool.Net.CTR
{
    // C# representation of the SMDH structure
    public class Smdh
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        private struct AppTitle
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
            public string ShortDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
            public string LongDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
            public string Publisher;
        }

        private const uint SmdhMagic = 0x48444D53; // "SMDH"
        private const int AppTitleCount = 16;

        private uint magic;
        private ushort version;
        private ushort reserved;
        private AppTitle[] appTitles;
        private byte[] ratings;
        private uint regionLock;
        private byte[] matchMakerId;
        private byte[] flags;
        private ushort eulaVersion;
        private ushort reserved2;
        private uint optimalBannerFrame;
        private uint streetpassId;
        private byte[] smallIconData; // 24x24, BGR565, tiled
        private byte[] largeIconData; // 48x48, BGR565, tiled

        public enum Language
        {
            Japanese, English, French, German, Italian, Spanish,
            SimplifiedChinese, Korean, Dutch, Portuguese, Russian,
            TraditionalChinese
            // 12-15 are reserved
        }

        public Smdh()
        {
            magic = SmdhMagic;
            version = 0;
            reserved = 0;
            appTitles = new AppTitle[AppTitleCount];
            for (int i = 0; i < AppTitleCount; i++)
            {
                appTitles[i] = new AppTitle { ShortDescription = "", LongDescription = "", Publisher = "" };
            }
            ratings = new byte[0x10];
            regionLock = 0;
            matchMakerId = new byte[12];
            flags = new byte[4];
            eulaVersion = 0;
            reserved2 = 0;
            optimalBannerFrame = 0;
            streetpassId = 0;
            smallIconData = new byte[24 * 24 * 2];
            largeIconData = new byte[48 * 48 * 2];
        }

        public void SetTitle(Language lang, string shortTitle, string longTitle, string publisher)
        {
            appTitles[(int)lang].ShortDescription = shortTitle ?? "";
            appTitles[(int)lang].LongDescription = longTitle ?? "";
            appTitles[(int)lang].Publisher = publisher ?? "";
        }

        public void SetAllTitlesTo(string shortTitle, string longTitle, string publisher)
        {
            for (int i = 0; i < 12; i++) // Only first 12 languages are used
            {
                SetTitle((Language)i, shortTitle, longTitle, publisher);
            }
        }

        public async Task SetSmallIconFromPngAsync(Stream pngStream)
        {
            smallIconData = await LoadIconDataAsync(pngStream, 24, 24);
        }

        public async Task SetLargeIconFromPngAsync(Stream pngStream)
        {
            largeIconData = await LoadIconDataAsync(pngStream, 48, 48);
        }

        private static async Task<byte[]> LoadIconDataAsync(Stream pngStream, int width, int height)
        {
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(pngStream))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Sampler = KnownResamplers.Lanczos3
                }));

                var pixelData = new byte[width * height * 2]; // BGR565 is 2 bytes per pixel

                using (var imageBgr565 = image.CloneAs<Bgr565>())
                {
                    imageBgr565.Frames.RootFrame.CopyPixelDataTo(pixelData);
                }

                return PixelConverter.Tile(pixelData, width, height, PixelFormat.Bgr565);
            }
        }

        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(magic);
                writer.Write(version);
                writer.Write(reserved);

                // Write titles. The total size of this block is 0x2000 bytes.
                // Short (128 bytes) + Long (256 bytes) + Publisher (128 bytes) = 512 bytes per title.
                // 512 * 16 titles = 8192 bytes (0x2000).
                foreach (var title in appTitles)
                {
                    writer.WriteUnicodeString(title.ShortDescription, 128);
                    writer.WriteUnicodeString(title.LongDescription, 256);
                    writer.WriteUnicodeString(title.Publisher, 128);
                }

                writer.Write(ratings);
                writer.Write(regionLock);
                writer.Write(matchMakerId);
                writer.Write(flags);
                writer.Write(eulaVersion);
                writer.Write(reserved2);
                writer.Write(optimalBannerFrame);
                writer.Write(streetpassId);

                // **FIXED**: The reserved space here is 8 bytes, not 32.
                writer.Write(new byte[0x8]);

                writer.Write(smallIconData);
                writer.Write(largeIconData);

                // Final size must be 0x36C0
                if (ms.Length != 0x36C0)
                    throw new InvalidOperationException($"SMDH size is incorrect. Expected 0x36C0, but got {ms.Length:X}.");

                return ms.ToArray();
            }
        }
    }
}
using System;
using System.IO;
using System.Text;
using Bannertool.Net.Compression;
using Bannertool.Net.Util;

namespace Bannertool.Net.CTR
{
    // CBMD: Common Banner Data format. Can contain CGFX (graphics) and CWAV (audio).
    public static class Cbmd
    {
        private const uint CbmdMagic = 0x444D4243; // "CBMD"

        [Flags]
        public enum CbmdFlags : uint
        {
            CgfxIsPresent = 1 << 0,
            CwavIsPresent = 1 << 1,
        }

        /// <summary>
        /// Builds the main payload of a banner file.
        /// </summary>
        public static byte[] Build(byte[] cgfxData, byte[] cwavData)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(CbmdMagic);
                writer.Write(0); // Version, unused
                writer.Write((uint)0); // Placeholder for flags
                writer.Write(new byte[0x14]); // Reserved

                // Write CGFX data
                if (cgfxData != null && cgfxData.Length > 0)
                {
                    writer.Write(cgfxData);
                }

                // Write CWAV data
                if (cwavData != null && cwavData.Length > 0)
                {
                    writer.Write(cwavData);
                }

                // Go back and write the flags
                CbmdFlags flags = 0;
                if (cgfxData != null && cgfxData.Length > 0) flags |= CbmdFlags.CgfxIsPresent;
                if (cwavData != null && cwavData.Length > 0) flags |= CbmdFlags.CwavIsPresent;

                writer.Seek(8, SeekOrigin.Begin);
                writer.Write((uint)flags);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Wraps a CBMD payload into a final .bnr file.
        /// </summary>
        public static byte[] BuildBanner(byte[] cbmdPayload, bool compress)
        {
            if (compress)
            {
                // LZ11 compress the payload and prepend the "LZ11" magic and uncompressed size
                return Lz11.Compress(cbmdPayload);
            }
            else
            {
                // Uncompressed banners are just the raw CBMD payload
                return cbmdPayload;
            }
        }
    }
}
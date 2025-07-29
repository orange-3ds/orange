using System;
using System.IO;
using System.Threading.Tasks;
using Bannertool.Net.Audio; // Use our own Audio namespace
using Bannertool.Net.ImaAdpcm;
using Bannertool.Net.Util;

namespace Bannertool.Net.CTR
{
    public static class Cwav
    {
        public enum CwavEncoding : ushort
        {
            Pcm8 = 0,
            Pcm16 = 1,
            ImaAdpcm = 2,
        }

        public class AudioSettings
        {
            public bool Looping { get; set; }
            public uint LoopStartSample { get; set; }
            public uint LoopEndSample { get; set; }
            public CwavEncoding Encoding { get; set; } = CwavEncoding.Pcm16;
        }

        private const uint CwavMagic = 0x56415743; // "CWAV"
        private const uint InfoMagic = 0x4F464E49; // "INFO"
        private const uint DataMagic = 0x41544144; // "DATA"

        public static Task<byte[]> FromAudioAsync(Stream audioStream, AudioSettings settings)
        {
            // **UPDATED**: This section now uses our self-contained WavParser.
            var (format, pcmData) = WavParser.Parse(audioStream);

            // Ensure data is 16-bit mono for the IMA-ADPCM encoder.
            if (format.BitsPerSample != 16)
                throw new NotSupportedException("Only 16-bit source WAV files are supported.");

            if (format.Channels != 1)
            {
                pcmData = ConvertStereoToMono16(pcmData);
            }

            // Convert raw PCM to target format (e.g., IMA-ADPCM)
            byte[] encodedData;
            ImaAdpcmState adpcmState = null;

            if (settings.Encoding == CwavEncoding.ImaAdpcm)
            {
                var pcm16 = new short[pcmData.Length / 2];
                Buffer.BlockCopy(pcmData, 0, pcm16, 0, pcmData.Length);
                (encodedData, adpcmState) = ImaAdpcmEncoder.Encode(pcm16);
            }
            else
            {
                encodedData = pcmData;
            }

            // Now, build the CWAV file structure (this part remains the same)
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // --- Main Header ---
                writer.Write(CwavMagic);
                writer.Write((ushort)0xFEFF); // BOM
                writer.Write((ushort)0x0200); // Header size
                writer.Write(0); // Version
                writer.Write(0); // Placeholder for file size
                writer.Write((ushort)2);      // Number of blocks (INFO, DATA)
                writer.Write((ushort)0);      // Reserved

                // --- Block References ---
                long infoRefOffset = ms.Position;
                writer.Write(new byte[12]); // Placeholder for INFO reference
                long dataRefOffset = ms.Position;
                writer.Write(new byte[12]); // Placeholder for DATA reference

                // --- INFO Block ---
                long infoBlockStart = ms.Position;
                writer.Write(InfoMagic);
                writer.Write(0); // Placeholder for INFO block size

                writer.Write((ushort)settings.Encoding);
                writer.Write(settings.Looping);
                writer.Write((byte)0); // Reserved
                writer.Write(format.SampleRate);
                writer.Write(settings.Looping ? settings.LoopStartSample : 0);
                writer.Write(settings.Looping ? settings.LoopEndSample : (uint)(pcmData.Length / (format.BitsPerSample / 8)));

                if (settings.Encoding == CwavEncoding.ImaAdpcm)
                {
                    if (adpcmState == null) throw new InvalidOperationException("ADPCM state is null.");
                    writer.Write(adpcmState.CoeffsAsBytes());
                    writer.Write(adpcmState.PredScale);
                    writer.Write(adpcmState.Yn1);
                    writer.Write(adpcmState.Yn2);
                }

                // Pad to 0x20 boundary
                while (ms.Position % 0x20 != 0) writer.Write((byte)0);

                // Update INFO block size
                long infoBlockEnd = ms.Position;
                writer.Seek((int)infoBlockStart + 4, SeekOrigin.Begin);
                writer.Write((uint)(infoBlockEnd - infoBlockStart));
                writer.Seek((int)infoBlockEnd, SeekOrigin.Begin);

                // --- DATA Block ---
                long dataBlockStart = ms.Position;
                writer.Write(DataMagic);
                writer.Write((uint)(encodedData.Length + 0x20)); // Data block size
                writer.Write(new byte[0x18]); // Reserved padding
                writer.Write(encodedData);

                // Pad to 0x20 boundary
                while (ms.Position % 0x20 != 0) writer.Write((byte)0);

                // --- Update All Placeholders ---
                long fileSize = ms.Position;

                // File size
                writer.Seek(0xC, SeekOrigin.Begin);
                writer.Write((uint)fileSize);

                // Info block reference
                writer.Seek((int)infoRefOffset, SeekOrigin.Begin);
                writer.WriteReference(InfoMagic, (uint)infoBlockStart, (uint)(infoBlockEnd - infoBlockStart));

                // Data block reference
                writer.Seek((int)dataRefOffset, SeekOrigin.Begin);
                writer.WriteReference(DataMagic, (uint)dataBlockStart, (uint)(ms.Length - dataBlockStart));

                return Task.FromResult(ms.ToArray());
            }
        }

        /// <summary>
        /// Converts a 16-bit stereo PCM byte array to a 16-bit mono PCM byte array by averaging channels.
        /// </summary>
        private static byte[] ConvertStereoToMono16(byte[] input)
        {
            byte[] output = new byte[input.Length / 2];
            int outputIndex = 0;
            for (int i = 0; i < input.Length; i += 4)
            {
                short left = (short)(input[i] | (input[i + 1] << 8));
                short right = (short)(input[i + 2] | (input[i + 3] << 8));
                short mono = (short)((left + right) / 2);

                output[outputIndex++] = (byte)(mono & 0xFF);
                output[outputIndex++] = (byte)((mono >> 8) & 0xFF);
            }
            return output;
        }
    }
}
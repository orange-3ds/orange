using System;
using System.IO;

namespace Bannertool.Net.Audio
{
    /// <summary>
    /// Represents the format of a wave file.
    /// </summary>
    internal class WaveFormat
    {
        public int SampleRate { get; set; }
        public ushort BitsPerSample { get; set; }
        public ushort Channels { get; set; }
    }

    /// <summary>
    /// A lightweight, self-contained parser for reading PCM data from WAV files.
    /// </summary>
    internal static class WavParser
    {
        /// <summary>
        /// Parses a WAV stream and extracts its format and raw PCM data.
        /// </summary>
        /// <returns>A tuple containing the wave format and the byte array of PCM data.</returns>
        public static (WaveFormat format, byte[] data) Parse(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                // Read RIFF header
                if (new string(reader.ReadChars(4)) != "RIFF")
                    throw new FormatException("Invalid WAV file: 'RIFF' chunk not found.");

                reader.ReadUInt32(); // Skip file size

                if (new string(reader.ReadChars(4)) != "WAVE")
                    throw new FormatException("Invalid WAV file: 'WAVE' format identifier not found.");

                // Find the 'fmt ' and 'data' chunks
                WaveFormat format = null;
                byte[] data = null;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    string chunkId = new string(reader.ReadChars(4));
                    uint chunkSize = reader.ReadUInt32();

                    if (chunkId == "fmt ")
                    {
                        ushort formatTag = reader.ReadUInt16();
                        if (formatTag != 1) // 1 = PCM
                            throw new NotSupportedException("Only uncompressed PCM WAV files are supported.");

                        format = new WaveFormat
                        {
                            Channels = reader.ReadUInt16(),
                            SampleRate = (int)reader.ReadUInt32(),
                        };
                        reader.ReadUInt32(); // Skip avg bytes per sec
                        reader.ReadUInt16(); // Skip block align
                        format.BitsPerSample = reader.ReadUInt16();
                    }
                    else if (chunkId == "data")
                    {
                        data = reader.ReadBytes((int)chunkSize);
                    }
                    else
                    {
                        // Skip unknown chunks
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    }

                    if (format != null && data != null)
                        break; // We have what we need
                }

                if (format == null || data == null)
                    throw new FormatException("Invalid WAV file: Could not find 'fmt ' and 'data' chunks.");

                return (format, data);
            }
        }
    }
}
using System;
using System.Runtime.InteropServices;

namespace Bannertool.Net.ImaAdpcm
{
    // Corresponds to the CWAVDSPADPCMInfo struct
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ImaAdpcmState
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public short[] Coeffs; // Always standard IMA ADPCM coeffs

        public ushort PredScale;
        public ushort Yn1;
        public ushort Yn2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        private byte[] padding;

        public byte[] CoeffsAsBytes()
        {
            var bytes = new byte[Coeffs.Length * sizeof(short)];
            Buffer.BlockCopy(Coeffs, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public ImaAdpcmState()
        {
            Coeffs = new short[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // Not used by IMA
            PredScale = 0;
            Yn1 = 0;
            Yn2 = 0;
            padding = new byte[12];
        }
    }

    public static class ImaAdpcmEncoder
    {
        private static readonly int[] IndexTable = { -1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8 };
        private static readonly int[] StepTable = {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
            50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230,
            253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963,
            1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327,
            3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487,
            12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
        };

        public static (byte[] adpcm, ImaAdpcmState state) Encode(short[] pcm)
        {
            // **FIXED**: Allocate space for the 4-byte header + the data.
            var adpcm = new byte[4 + pcm.Length / 2];
            var state = new ImaAdpcmState();

            int sample = 0;
            int index = 0;
            int outPos = 0;

            // ADPCM header for each block
            adpcm[outPos++] = (byte)(sample & 0xFF);
            adpcm[outPos++] = (byte)((sample >> 8) & 0xFF);
            adpcm[outPos++] = (byte)index;
            adpcm[outPos++] = 0; // Padding

            for (int i = 0; i < pcm.Length; i++)
            {
                int step = StepTable[index];
                int diff = pcm[i] - sample;
                int encoded = 0;

                if (diff < 0)
                {
                    encoded = 8;
                    diff = -diff;
                }

                if (diff >= step) { encoded |= 4; diff -= step; }
                step >>= 1;
                if (diff >= step) { encoded |= 2; diff -= step; }
                step >>= 1;
                if (diff >= step) { encoded |= 1; }

                int predicted_diff = 0;
                if ((encoded & 8) != 0) predicted_diff -= StepTable[index];
                if ((encoded & 4) != 0) predicted_diff += StepTable[index] >> 1;
                if ((encoded & 2) != 0) predicted_diff += StepTable[index] >> 2;
                if ((encoded & 1) != 0) predicted_diff += StepTable[index] >> 3;
                predicted_diff += StepTable[index] >> 4;

                sample += predicted_diff;
                sample = Math.Max(-32768, Math.Min(32767, sample));

                index += IndexTable[encoded];
                index = Math.Max(0, Math.Min(88, index));

                if ((i & 1) == 0)
                    adpcm[outPos] = (byte)(encoded & 0xF);
                else
                    adpcm[outPos++] |= (byte)((encoded & 0xF) << 4);
            }

            // Final state
            state.PredScale = (ushort)sample;
            state.Yn1 = (ushort)index;

            return (adpcm, state);
        }
    }
}
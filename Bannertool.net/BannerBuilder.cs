using System;
using System.IO;
using System.Threading.Tasks;
using Bannertool.Net.Audio;
using Bannertool.Net.Compression;
using Bannertool.Net.Graphics;
using Bannertool.Net.CTR;

namespace Bannertool.Net
{
    public class BannerOptions
    {
        // This class remains unchanged
        public string ImagePath { get; set; }
        public string AudioPath { get; set; }
        public bool LoopAudio { get; set; } = false;
        public uint LoopStartSample { get; set; } = 0;
        public uint LoopEndSample { get; set; } = 0;
        public bool Lz11Compress { get; set; } = true;
    }

    public static class BannerBuilder
    {
        // NEW: Static overload for convenience.
        // Takes parameters directly instead of an options object.
        public static Task<byte[]> CreateBannerAsync(
            string imagePath,
            string audioPath,
            bool loopAudio = false,
            uint loopStartSample = 0,
            uint loopEndSample = 0,
            bool lz11Compress = true)
        {
            // Create the options object internally and call the original method.
            var options = new BannerOptions
            {
                ImagePath = imagePath,
                AudioPath = audioPath,
                LoopAudio = loopAudio,
                LoopStartSample = loopStartSample,
                LoopEndSample = loopEndSample,
                Lz11Compress = lz11Compress
            };
            return CreateBannerAsync(options);
        }

        /// <summary>
        /// Creates a complete 3DS banner file (.bnr) from the provided options.
        /// </summary>
        /// <param name="options">The settings for creating the banner.</param>
        /// <returns>A byte array containing the banner.bnr file data.</returns>
        public static async Task<byte[]> CreateBannerAsync(BannerOptions options)
        {
            // This original method remains the core logic.
            if (string.IsNullOrEmpty(options.ImagePath))
                throw new ArgumentNullException(nameof(options.ImagePath), "Image path must be provided.");
            if (string.IsNullOrEmpty(options.AudioPath))
                throw new ArgumentNullException(nameof(options.AudioPath), "Audio path must be provided.");

            // 1. Load and convert the image to CGFX format
            byte[] cgfxData;
            using (var imageStream = File.OpenRead(options.ImagePath))
            {
                cgfxData = await Cgfx.FromPngAsync(imageStream, 256, 128, PixelFormat.Rgba8);
            }

            // 2. Load and convert the audio to CWAV format
            byte[] cwavData;
            using (var audioStream = File.OpenRead(options.AudioPath))
            {
                var audioSettings = new Cwav.AudioSettings
                {
                    Looping = options.LoopAudio,
                    LoopStartSample = options.LoopStartSample,
                    LoopEndSample = options.LoopEndSample,
                    Encoding = Cwav.CwavEncoding.ImaAdpcm
                };
                cwavData = await Cwav.FromAudioAsync(audioStream, audioSettings);
            }

            // 3. Build the CBMD (common banner data) structure
            var cbmdPayload = Cbmd.Build(cgfxData, cwavData);

            // 4. Build the final BNR, which wraps the CBMD
            var bnrPayload = Cbmd.BuildBanner(cbmdPayload, options.Lz11Compress);

            return bnrPayload;
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Bannertool.Net.CTR;

namespace Bannertool.Net
{
    public class SmdhOptions
    {
        // This class remains unchanged
        public string LargeIconPath { get; set; }
        public string SmallIconPath { get; set; }
        public string ShortTitle { get; set; }
        public string LongTitle { get; set; }
        public string Publisher { get; set; }
    }

    public static class SmdhBuilder
    {
        // NEW: Static overload for convenience.
        public static Task<byte[]> CreateSmdhAsync(
            string largeIconPath,
            string smallIconPath,
            string shortTitle,
            string longTitle,
            string publisher)
        {
            // Create the options object internally.
            var options = new SmdhOptions
            {
                LargeIconPath = largeIconPath,
                SmallIconPath = smallIconPath,
                ShortTitle = shortTitle,
                LongTitle = longTitle,
                Publisher = publisher
            };
            return CreateSmdhAsync(options);
        }

        /// <summary>
        /// Creates an SMDH file (icon.icn) from the provided options.
        /// </summary>
        public static async Task<byte[]> CreateSmdhAsync(SmdhOptions options)
        {
            // This original method remains the core logic.
            if (string.IsNullOrEmpty(options.LargeIconPath) || string.IsNullOrEmpty(options.SmallIconPath))
                throw new ArgumentException("Both large and small icon paths must be provided.");

            var smdh = new Smdh();

            // Set all language titles to match the provided one as a fallback.
            smdh.SetAllTitlesTo(options.ShortTitle, options.LongTitle, options.Publisher);

            // Load icons
            using (var largeIconStream = File.OpenRead(options.LargeIconPath))
            {
                await smdh.SetLargeIconFromPngAsync(largeIconStream);
            }
            using (var smallIconStream = File.OpenRead(options.SmallIconPath))
            {
                await smdh.SetSmallIconFromPngAsync(smallIconStream);
            }

            // Serialize the SMDH structure to bytes
            return smdh.ToByteArray();
        }
    }
}
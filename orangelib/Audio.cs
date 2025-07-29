using System.Diagnostics;
using System.Threading.Tasks;

namespace OrangeLib
{
    public static class AudioConverter
    {
        /// <summary>
        /// Converts a WAV audio file to a specific format using ffmpeg.
        /// The output format is 16-bit PCM, 32,000 Hz, stereo.
        /// </summary>
        /// <param name="inputPath">The path to the input WAV file.</param>
        /// <param name="outputPath">The path for the converted output WAV file.</param>
        /// <returns>A task that returns true if conversion is successful, otherwise false.</returns>
        public static async Task<bool> ConvertWavTo3dsFormatAsync(string inputPath, string outputPath)
        {
            // ffmpeg arguments for converting to 16-bit PCM, 32kHz, stereo.
            // -y: Overwrite output file if it exists
            // -i: Input file
            // -ar: Audio sample rate
            // -ac: Audio channels (2 for stereo)
            // -c:a: Audio codec (pcm_s16le for 16-bit signed little-endian PCM)
            string arguments = $"-y -i \"{inputPath}\" -ar 32000 -ac 2 -c:a pcm_s16le \"{outputPath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                // Asynchronously read the output and error streams.
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    // Log the error from ffmpeg for diagnostics.
                    Console.Error.WriteLine("ffmpeg conversion failed.");
                    Console.Error.WriteLine(error);
                    return false;
                }
            }
        }
    }
}
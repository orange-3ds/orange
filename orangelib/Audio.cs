using System.Diagnostics;
using System.Threading.Tasks;

namespace OrangeLib
{
    public static class AudioConverter
    {
        public static async Task<bool> ConvertWavTo3dsFormatAsync(string inputPath, string outputPath)
        {
            // Corrected ffmpeg arguments for 3DS banner audio:
            // -ar 32000: Sets the sample rate to 32,000 Hz.
            // -t 3: Ensures the audio is no longer than 3 seconds.
            // -map_metadata -1: Removes all metadata to prevent compatibility issues.
            // -fflags +bitexact: Ensures a clean, bit-exact WAV file.
            string arguments = $"-y -i \"{inputPath}\" -t 3 -ar 32000 -ac 2 -c:a pcm_s16le -map_metadata -1 -fflags +bitexact \"{outputPath}\"";


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
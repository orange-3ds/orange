using System;
using System.IO;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace OrangeLib.Net
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public static class Internet
    {
        private static readonly string DefaultWebPath = "https://orange.collinsoftware.dev/";
        static string _webPath = DefaultWebPath;

        public static string GetWebPath()
        {
            return _webPath;
        }
        public static void SetWebPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Web path cannot be null or empty.", nameof(path));
            }
            _webPath = path;
        }
        public static void ResetWebPath()
        {
            _webPath = DefaultWebPath;
        }
        public static async Task Getlibrary(string libraryName)
        {
            if (string.IsNullOrWhiteSpace(libraryName))
            {
                throw new ArgumentException("library name cannot be null or empty.", nameof(libraryName));
            }
            string url = $"{_webPath}libraries/{libraryName}.zip";
            Console.WriteLine($"Fetching library from: {url}");

            // Use a unique temporary file path for the download
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{libraryName}_{Guid.NewGuid()}.zip");
            try
            {
                await Utils.DownloadFileAsync(url, tempFilePath);
                // Validate ZIP file before installing
                try
                {
                    using (var zip = System.IO.Compression.ZipFile.OpenRead(tempFilePath))
                    {
                        // If we can open, it's a valid ZIP
                    }
                }
                catch (System.IO.InvalidDataException)
                {
                    await Console.Error.WriteLineAsync($"Downloaded file is not a valid ZIP archive: {tempFilePath}").ConfigureAwait(false);
                    return;
                }
                library.Installlibrary(tempFilePath);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error during library download or installation: {ex.Message}").ConfigureAwait(false);
                throw;
            }
            finally
            {
                // Ensure cleanup of the temporary file
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { /* Ignore cleanup errors */ }
                }
            }
        }
    }
}
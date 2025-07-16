using System;
using System.IO;
using System.Threading.Tasks;

namespace OrangeLib.Net
{
    public static class Internet
    {
        private const string DefaultWebPath = "https://orange.orbical.xyz/";
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
        public static async Task GetPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));
            }
            string url = $"{_webPath}packages/{packageName}.zip";
            Console.WriteLine($"Fetching package from: {url}");

            // Use a unique temporary file path for the download
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{packageName}_{Guid.NewGuid()}.zip");
            try
            {
                await Utils.DownloadFileAsync(url, tempFilePath);
                Package.InstallPackage(tempFilePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during package download or installation: {ex.Message}");
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
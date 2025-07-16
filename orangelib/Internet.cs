using System.Threading.Tasks;

namespace OrangeLib.Net
{
    public static class Internet
    {
        static string _webPath = "https://orange.orbical.xyz/";

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
            _webPath = "https://orange.orbical.xyz/";
        }
        public static async Task GetPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));
            }
            string url = $"{_webPath}packages/{packageName}.zip";
            Console.WriteLine($"Fetching package from: {url}");
            // TODO: Implement Download Logic
            // Get File from url
            await Utils.DownloadFileAsync(url, packageName + ".zip");
            Package.InstallPackage(packageName + ".zip");
            // Remove package zip file if exists
            if (File.Exists(packageName + ".zip"))
            {
                File.Delete(packageName + ".zip");
            }
        }
    }
}
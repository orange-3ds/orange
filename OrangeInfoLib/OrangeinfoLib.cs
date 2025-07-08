using configlibnet;

//TODO: Move this code to OrangeLib.cs or a new file in the project.
namespace OrangeInfoLib
{
    [Obsolete("Code will be moved to OrangeLib and removed from OrangeinfoLib.")]
    public struct Information
    {
        public string Title;
        public string Description;
        public string Author;
        public string Dependencies;
        public string ReadmeContents;
    }
    [Obsolete("Code will be moved to OrangeLib and removed from OrangeinfoLib.")]
    public class PackageInfo
    {
        protected string Title = "Oranges";
        protected string Description = "3ds Homebrew library";
        protected string Author = "Me :)";
        protected string[] Dependencies = new string[] { };
        protected string ReadmeContents = "Oranges readme.";


        public string GetPackageTitle()
        {
            return Title;
        }
        public string GetPackageDescription()
        {
            return Description;
        }
        public string GetPackageAuthor()
        {
            return Author;
        }
        public string GetDependencies()
        {
            string result = ArrayToStringSpaceSeperate(Dependencies);
            return result;
        }
        static public string GetReadmeContents(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return "README path not provided";
                if (!File.Exists(path))
                    return $"README file not found: {path}";
                
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                return $"Error reading README file '{path}': {ex.Message}";
            }
        }
        public static string ArrayToStringSpaceSeperate(string[] array)
        {
            if (array == null || array.Length == 0)
                return string.Empty;
            return string.Join(" ", array);
        }


        public Information GetInformation()
        {
            return new Information
            {
                Title = GetPackageTitle(),
                Description = GetPackageDescription(),
                Author = GetPackageAuthor(),
                Dependencies = GetDependencies(),
                ReadmeContents = ReadmeContents
            };
        }
        public Information LoadCfg(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Configuration filename cannot be null or empty.", nameof(filename));
            
            // Check if file exists and if the file extension is .cfg
            if (!System.IO.File.Exists(filename) || System.IO.Path.GetExtension(filename).ToLower() != ".cfg")
            {
                throw new System.IO.FileNotFoundException("Configuration file not found or invalid file type.", filename);
            }
            
            try
            {
                // load file
                string filebuffer = File.ReadAllText(filename);
                ConfigFile configFile = ConfigFile.Parse(filebuffer);
                
                // Get information from config file
                Title = configFile.GetVariable("info", "Title") ?? Title;
                Description = configFile.GetVariable("info", "Description")  ?? Description;
                Author = configFile.GetVariable("info", "Author") ?? Author;
                Dependencies = configFile.GetArray("dependencies");
                string readmePath = configFile.GetVariable("info", "README") ?? ReadmeContents;
                
                // Resolve relative path to config file directory
                if (!string.IsNullOrWhiteSpace(readmePath) && !System.IO.Path.IsPathRooted(readmePath))
                {
                    string configDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filename)) ?? string.Empty;
                    readmePath = System.IO.Path.Combine(configDir, readmePath);
                    
                    // Validate resolved path to prevent directory traversal
                    string fullConfigDir = Path.GetFullPath(configDir);
                    string fullReadmePath = Path.GetFullPath(readmePath);
                    if (!fullReadmePath.StartsWith(fullConfigDir, StringComparison.OrdinalIgnoreCase))
                    {
                        ReadmeContents = $"README path outside config directory is not allowed: {readmePath}";
                        return GetInformation();
                    }
                }
                
                ReadmeContents = System.IO.File.Exists(readmePath)
                    ? GetReadmeContents(readmePath)
                    : $"README file not found: {readmePath}";
                
                // Return information
                return GetInformation();
            }
            catch (Exception ex) when (!(ex is FileNotFoundException))
            {
                throw new InvalidOperationException(
                    $"Failed to load configuration from '{filename}': {ex.Message}", ex);
            }
        }
    }
}

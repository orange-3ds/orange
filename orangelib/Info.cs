using configlibnet;

namespace OrangeLib.Info
{
    public struct Information
    {
        public string Title;
        public string Description;
        public string Author;
        public string Dependencies;
        public string ReadmeContents;
    }
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

        // Helper: Validate dependency
        private static bool IsValidDependency(string dependency)
        {
            return !string.IsNullOrWhiteSpace(dependency);
        }

        // Helper: Read config file
        private static ConfigFile ReadConfig(string cfgPath)
        {
            if (!File.Exists(cfgPath))
                throw new FileNotFoundException("Configuration file not found.", cfgPath);
            string filebuffer = File.ReadAllText(cfgPath);
            return ConfigFile.Parse(filebuffer);
        }

        // Helper: Write config file, preserving comments and formatting
        private static void WriteConfigWithUpdatedDependencies(string cfgPath, ConfigFile configFile, string[] dependencies)
        {
            var lines = File.ReadAllLines(cfgPath);
            using (var writer = new StreamWriter(cfgPath, false))
            {
                bool inDependencies = false;
                foreach (var line in lines)
                {
                    if (line.Trim() == "[dependencies]")
                    {
                        writer.WriteLine(line);
                        inDependencies = true;
                        // Write updated dependencies
                        foreach (var dep in dependencies)
                            writer.WriteLine(dep);
                        continue;
                    }
                    if (inDependencies)
                    {
                        // Skip old dependencies
                        if (line.StartsWith("["))
                        {
                            inDependencies = false;
                            writer.WriteLine(line);
                        }
                        // else skip
                        continue;
                    }
                    // For README, write path not contents
                    if (line.StartsWith("README:", StringComparison.OrdinalIgnoreCase))
                    {
                        string readmePath = configFile.GetVariable("info", "README") ?? "README.md";
                        writer.WriteLine($"README: {readmePath}");
                        continue;
                    }
                    writer.WriteLine(line);
                }
            }
        }

        // Refactored method
        public Information AddDependencyToCfg(string dependency, string cfgPath)
        {
            if (!IsValidDependency(dependency))
                throw new ArgumentException("Invalid dependency.", nameof(dependency));

            // Always use only the filename without extension
            string depName = Path.GetFileNameWithoutExtension(dependency);

            var configFile = ReadConfig(cfgPath);
            var currentDeps = configFile.GetArray("dependencies").ToList();
            if (currentDeps.Contains(depName))
                throw new InvalidOperationException($"Dependency '{depName}' already exists.");
            currentDeps.Add(depName);
            configFile.AddToArray("dependencies", depName);
            Dependencies = currentDeps.ToArray();
            WriteConfigWithUpdatedDependencies(cfgPath, configFile, Dependencies);
            return GetInformation();
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
                Description = configFile.GetVariable("info", "Description") ?? Description;
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

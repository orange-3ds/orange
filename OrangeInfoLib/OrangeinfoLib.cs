using configlibnet;
namespace OrangeInfoLib
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
        public string GetReadmeContents(string path)
        {
            string contents = File.ReadAllText(path);
            return contents;
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
            // Check if file exists and if the file extension is .cfg
            if (!System.IO.File.Exists(filename) || System.IO.Path.GetExtension(filename).ToLower() != ".cfg")
            {
                throw new System.IO.FileNotFoundException("Configuration file not found or invalid file type.", filename);
            }
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
            }
            ReadmeContents = System.IO.File.Exists(readmePath) ? GetReadmeContents(readmePath) : $"README file not found: {readmePath}";
            // Return information
            return GetInformation();
        }
    }
}

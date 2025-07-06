using configlibnet;
namespace OrangeInfoLib
{
    public struct Information
    {
        public string AppTitle; 
        public string AppDescription;
        public string AppAuthor; 
        public string Dependencies;
    }
    public class ProjectInfo
    {
        protected string AppTitle = "Oranges";
        protected string AppDescription = "3ds Homebrew application";
        protected string AppAuthor = "Me :)";
        protected string[] Dependencies = new string[] { "-lctru", "-m" };
        

        public string GetAppTitle()
        {
            return AppTitle;
        }
        public string GetAppDescription()
        {
            return AppDescription;
        }
        public string GetAppAuthor()
        {
            return AppAuthor;
        }
        public string GetDependencies()
        {
            string result = ArrayToStringSpaceSeperate(Dependencies);
            return result;
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
                AppTitle = GetAppTitle(),
                AppDescription = GetAppDescription(),
                AppAuthor = GetAppAuthor(),
                Dependencies = GetDependencies()
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
            AppTitle = configFile.GetVariable("info", "AppTitle") ?? AppTitle;
            AppDescription = configFile.GetVariable("info", "AppDescription") ?? AppDescription;
            AppAuthor = configFile.GetVariable("info", "AppAuthor") ?? AppAuthor;
            Dependencies = configFile.GetArray("dependencies");
            // Return information
            return GetInformation();
        }
    }
}

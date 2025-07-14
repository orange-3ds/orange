namespace OrangeLib.Net
{
    public static class Internet
    {
        static string _webPath = "https://orange.orbical.xyz/";
        /// <summary>
        /// Gets the web path for OrangeLib.
        /// </summary>
        /// <returns>The web path as a string.</returns>
        public static string GetWebPath()
        {
            return _webPath;
        }
    }
    
}
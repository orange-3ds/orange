namespace configlibnet
{
    public class ConfigFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> sections = new();
        private readonly Dictionary<string, List<string>> arraySections = new();

        public static ConfigFile Parse(string buffer)
        {
            var config = new ConfigFile();
            string? currentSection = null;

            foreach (var rawLine in buffer.Split(new[] { '\n', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();

                if (IsCommentOrEmpty(line))
                    continue;

                if (IsSectionHeader(line, out var sectionName))
                {
                    currentSection = sectionName;
                    InitializeSection(config, currentSection);
                    continue;
                }

                if (currentSection == null)
                    continue;

                line = RemoveInlineComment(line);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                ProcessLine(config, currentSection, line);
            }

            return config;
        }

        private static bool IsCommentOrEmpty(string line)
        {
            return string.IsNullOrWhiteSpace(line) || line.StartsWith("#");
        }

        private static bool IsSectionHeader(string line, out string sectionName)
        {
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                sectionName = line[1..^1].Trim();
                return true;
            }

            sectionName = string.Empty;
            return false;
        }

        private static void InitializeSection(ConfigFile config, string section)
        {
            config.sections.TryAdd(section, new Dictionary<string, string>());
            config.arraySections.TryAdd(section, new List<string>());
        }

        private static string RemoveInlineComment(string line)
        {
            int commentIdx = line.IndexOf('#');
            return commentIdx >= 0 ? line.Substring(0, commentIdx).TrimEnd() : line;
        }

        private static void ProcessLine(ConfigFile config, string currentSection, string line)
        {
            if (line.Contains(":"))
            {
                var idx = line.IndexOf(":");
                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                config.sections[currentSection][key] = value;
            }
            else
            {
                config.arraySections[currentSection].Add(line);
            }
        }

        public string? GetVariable(string section, string key)
        {
            if (sections.TryGetValue(section, out var dict) && dict.TryGetValue(key, out var value))
                return value;
            return null;
        }

        public string[] GetArray(string section)
        {
            if (arraySections.TryGetValue(section, out var list))
                return list.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            return Array.Empty<string>();
        }
    }
}

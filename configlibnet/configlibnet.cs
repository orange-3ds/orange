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
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1].Trim();
                    config.sections.TryAdd(currentSection, new Dictionary<string, string>());
                    config.arraySections.TryAdd(currentSection, new List<string>());
                    continue;
                }
                if (currentSection == null)
                    continue;
                // Remove inline comments after #
                int commentIdx = line.IndexOf('#');
                if (commentIdx >= 0)
                    line = line.Substring(0, commentIdx).TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
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
            return config;
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

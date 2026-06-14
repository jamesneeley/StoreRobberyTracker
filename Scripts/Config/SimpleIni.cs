using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace StoreRobberyEnhanced
{
    internal class SimpleIni
    {
        private readonly string _path;

        // Key/value data
        private readonly Dictionary<string, Dictionary<string, string>> _data =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Comments per section
        private readonly Dictionary<string, List<string>> _comments =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public SimpleIni(string path)
        {
            _path = path;

            if (File.Exists(_path))
                Load();
        }

        private void Load()
        {
            string currentSection = "";

            foreach (string rawLine in File.ReadAllLines(_path))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();

                    if (!_data.ContainsKey(currentSection))
                        _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                if (!_data.ContainsKey(currentSection))
                    _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                _data[currentSection][key] = value;
            }
        }

        // ------------------------------------------------------------
        // SAVE WITH COMMENT + ORDER CONTROL
        // ------------------------------------------------------------
        public void Save()
        {
            using (var writer = new StreamWriter(_path, false))
            {
                // 1. Write all non-debug sections first
                foreach (var section in SortedSections(excludeDebug: true))
                    WriteSection(writer, section);

                // 2. Write Debug LAST
                if (_data.ContainsKey("Debug") || _comments.ContainsKey("Debug"))
                    WriteSection(writer, "Debug");
            }
        }

        private void WriteSection(StreamWriter writer, string sectionName)
        {
            // Write comments
            if (_comments.TryGetValue(sectionName, out var commentList))
            {
                foreach (string c in commentList)
                    writer.WriteLine("; " + c);
            }

            // Section header
            writer.WriteLine("[" + sectionName + "]");

            // Keys
            if (_data.TryGetValue(sectionName, out var sec))
            {
                foreach (var kv in sec)
                    writer.WriteLine(kv.Key + "=" + kv.Value);
            }

            writer.WriteLine();
        }

        private IEnumerable<string> SortedSections(bool excludeDebug)
        {
            var list = new List<string>(_data.Keys);

            if (excludeDebug)
                list.RemoveAll(s => s.Equals("Debug", StringComparison.OrdinalIgnoreCase));

            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        // ------------------------------------------------------------
        // COMMENT SUPPORT
        // ------------------------------------------------------------
        public void WriteComment(string section, string comment)
        {
            if (!_comments.TryGetValue(section, out var list))
            {
                list = new List<string>();
                _comments[section] = list;
            }

            list.Add(comment);
        }

        // ------------------------------------------------------------
        // READ METHODS
        // ------------------------------------------------------------
        public string ReadString(string section, string key, string defaultValue)
        {
            if (_data.TryGetValue(section, out var sec))
            {
                if (sec.TryGetValue(key, out var val))
                    return val;
            }

            return defaultValue;
        }

        public int ReadInt(string section, string key, int defaultValue)
        {
            string s = ReadString(section, key, defaultValue.ToString());
            return int.TryParse(s, out int v) ? v : defaultValue;
        }

        public float ReadFloat(string section, string key, float defaultValue)
        {
            string s = ReadString(section, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                ? v
                : defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            string s = ReadString(section, key, defaultValue ? "true" : "false");
            return bool.TryParse(s, out bool v) ? v : defaultValue;
        }

        // ------------------------------------------------------------
        // WRITE METHODS
        // ------------------------------------------------------------
        public void WriteString(string section, string key, string value)
        {
            if (!_data.TryGetValue(section, out var sec))
            {
                sec = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _data[section] = sec;
            }

            if (value == null)
            {
                if (sec.ContainsKey(key))
                    sec.Remove(key);
                return;
            }

            sec[key] = value;
        }

        public void WriteInt(string section, string key, int value)
        {
            WriteString(section, key, value.ToString());
        }

        public void WriteFloat(string section, string key, float value)
        {
            WriteString(section, key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteBool(string section, string key, bool value)
        {
            WriteString(section, key, value ? "true" : "false");
        }

        public void DeleteKey(string section, string key)
        {
            if (_data.TryGetValue(section, out var sec))
            {
                if (sec.ContainsKey(key))
                    sec.Remove(key);
            }
        }

        // ------------------------------------------------------------
        // SECTION & KEY HELPERS (required for auto-merge logic)
        // ------------------------------------------------------------
        public bool SectionExists(string section)
        {
            return _data.ContainsKey(section);
        }

        public bool KeyExists(string section, string key)
        {
            if (_data.TryGetValue(section, out var sec))
                return sec.ContainsKey(key);

            return false;
        }

        public IEnumerable<string> GetSections()
        {
            return _data.Keys;
        }

        public IEnumerable<string> GetKeys(string section)
        {
            if (_data.TryGetValue(section, out var sec))
                return sec.Keys;

            return Array.Empty<string>();
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IntelliTrader.Web
{
    public static class Utils
    {
        private static Regex fixJsonPattern = new Regex(@"\w[^,{]+[\w""]", RegexOptions.Compiled);

        public static string FixInvalidJson(string json)
        {
            string fixedJson = fixJsonPattern.Replace(json, match =>
            {
                string matchString = match.ToString();
                if (!matchString.EndsWith("\""))
                {
                    string[] split = matchString.Split(": ");

                    if (split.Length == 1)
                    {
                        return $"\"{matchString.Trim('"', ' ')}\"";
                    }
                    else
                    {
                        string left = split[0].Trim('"', ' ');
                        string right = split[1].Trim('"', ' ');

                        if (right[0] == '[')
                        {
                            return $"\"{left}\": {right.Replace("[", "[\"")}\"";
                        }
                        else if (right == "True" || right == "False")
                        {
                            return $"\"{left}\": \"{right.ToLowerInvariant()}\"";
                        }
                        else if (right == "null")
                        {
                            return $"\"{left}\": {right}";
                        }
                        else
                        {
                            return $"\"{left}\": \"{right}\"";
                        }
                    }
                }
                else
                {
                    return matchString;
                }
            });
            return fixedJson;
        }

        public static IEnumerable<string> ReadAllLinesWriteSafe(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                while (!sr.EndOfStream)
                {
                    yield return sr.ReadLine();
                }
            }
        }
    }
}

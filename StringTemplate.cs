using Dacris.Maestro.Core;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dacris.Maestro
{
    internal class StringTemplate
    {
        public const string Regex = "\\#\\@([^\\@]+)\\@";
        public static string MatchEval(Match match)
        {
            var state = AppState.Instance.StateObject;
            var jsonPath = match.Groups[1].Value;
            if (jsonPath.EndsWith(".length()"))
            {
                return ((JArray?)state.SelectToken(jsonPath.Replace(".length()", string.Empty)))?.Count.ToString() ?? "0";
            }
            var token = state.SelectToken(jsonPath);
            if (token is JArray)
            {
                StringBuilder output = new StringBuilder();
                int index = 0;
                foreach (JToken item in token)
                {
                    var level = int.Parse(AppState.Instance.ReadKey("_templateLevel")!.ToString());
                    level++;
                    AppState.Instance.WriteKey("_templateLevel", level.ToString());
                    AppState.Instance.WriteKey("level" + level + "Item", item);
                    AppState.Instance.WriteKey("level" + level + "Index", index.ToString());
                    var template = AppState.Instance.ReadKey("level" + level + "Template")?.ToString();
                    var templateFile = AppState.Instance.ReadKey("level" + level + "TemplateFile")?.ToString();
                    if (!string.IsNullOrEmpty(templateFile))
                    {
                        template = File.ReadAllText(templateFile);
                    }
                    if (string.IsNullOrEmpty(template))
                    {
                        template = "#@$.level" + level + "Item@";
                    }
                    output.Append(System.Text.RegularExpressions.Regex.Replace(template, Regex, MatchEval));
                    AppState.Instance.WriteKey("_templateLevel", (level - 1).ToString());
                    index++;
                }
                return output.ToString();
            }
            if (token is JObject)
            {
                return token?.ToString() ?? string.Empty;
            }
            // expecting a string:
            FormatValue.EncodeValue(
                AppState.Instance.ReadKey("_templateFormat")?.ToString() ?? "txt",
                token?.ToString() ?? string.Empty,
                "$._templateMatchTmp");
            var tmp = AppState.Instance.ReadKey("_templateMatchTmp");
            AppState.Instance.ClearKey("_templateMatchTmp");
            return tmp?.ToString() ?? string.Empty;
        }
    }
}

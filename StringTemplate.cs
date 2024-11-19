using Dacris.Maestro.Core;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;
/*
<html>
<head>
<title>App Explanation</title>
<style>
* { font-family: Verdana, Arial, Helvetica, Sans-serif; }
body { padding: 20px; margin: 20px; border: none; background-color: #fff; }
h1 { font-size: 18pt; color: #996699; }
h2 { font-size: 16pt; color: #663366; margin-top: 6px; font-weight: normal; }
a { font-weight: bold; color: #36c; text-decoration: none; }
a:hover { text-decoration: underline; color: #03c; }
hr { border: none; height: 2px; background-color: #ccf; }
div {
margin: 0; width:90%;
float:left;
}
.indent { padding-left: 30px; border-left: 1px solid #ccc; }
.submit { padding-top: 5px; padding-bottom: 5px; }
@media (min-width: 360px) {
  body {
    font-size: 1.0em;
  }
}
</style>
</head>
<body>
<h1>Executive Summary</h1>
<h1>Detailed Explanation</h1>
#@$.Statements.$values@
</body>
</html>
*/

namespace Dacris.Maestro
{
    internal class StringTemplate
    {
        public const string Regex = "\\#\\@(((\\\\\\@)*[^\\@])+[^\\\\])\\@";
        public static string MatchEval(Match match)
        {
            var state = AppState.Instance.StateObject;
            var jsonPath = match.Groups[1].Value;
            jsonPath = jsonPath.Replace("\\@", "@");
            if (jsonPath.EndsWith(".length()"))
            {
                var multiValue = state.SelectToken(jsonPath.Replace(".length()", string.Empty));
                if (multiValue is JArray)
                {
                    return ((JArray?)multiValue)?.Count.ToString() ?? "0";
                }
                else
                {
                    return ((JValue?)multiValue)?.ToString()?.Length.ToString() ?? "0";
                }
            }
            if (jsonPath.Contains(".keyAtPath("))
            {
                var startIdx = 0;
                do
                {
                    var idx = jsonPath.IndexOf(".keyAtPath(", startIdx);
                    if (idx < 0)
                        break;
                    var endIdx = jsonPath.IndexOf(")", idx);
                    var subPath = jsonPath.Substring(idx + ".keyAtPath(".Length, endIdx - idx - ".keyAtPath(".Length);
                    var subToken = state.SelectToken(subPath);
                    jsonPath = jsonPath.Replace(".keyAtPath(" + subPath + ")", "['" + (subToken?.ToString() ?? "2vb792bv393hkabxs3bskz") + "']");
                    startIdx = idx + 1;
                }
                while (true);
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

using Newtonsoft.Json.Linq;

namespace Dacris.Maestro
{
    internal class DocGenerator
    {
        public Dictionary<string, string> LoadDescriptions()
        {
            var descriptions = new Dictionary<string, string>();
            var lines1 = File.ReadAllLines("InteractionMap.tsv");
            var lines2 = File.ReadAllLines("BuiltInInteractionMap.tsv");
            var lines = new List<string>();
            lines.AddRange(lines1.Skip(1));
            lines.AddRange(lines2.Skip(1));
            foreach (string line in lines)
            {
                var columns = line.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var fqn = columns[0].Substring(0, columns[0].IndexOf(','));
                var desc = columns[1];
                descriptions.Add(fqn, "<p>" + desc + "</p>");
            }
            return descriptions;
        }

        public void GenerateSampleState(JObject state, string interactionFQN, string? inputStateKey)
        {
            // 1. Find pattern json
            var jsonPatternsPath = "Json";
            if (!Path.Exists(jsonPatternsPath))
            {
                return;
            }
            var patternFilePath = Path.Combine(jsonPatternsPath,
                interactionFQN.Substring(0, interactionFQN.LastIndexOf('.')),
                interactionFQN.Substring(interactionFQN.LastIndexOf('.') + 1,
                    interactionFQN.Length - interactionFQN.LastIndexOf('.') - 1)
                + ".json");
            if (!Path.Exists(patternFilePath))
            {
                return;
            }
            // 2. Read pattern spec
            var pattern = JObject.Parse(File.ReadAllText(patternFilePath));
            foreach (var rootKey in pattern["Inputs"]!)
            {
                // 3. Generate sample inputs
                var keyName = rootKey["Name"]!.ToString();
                if (keyName == "InputState")
                {
                    keyName = inputStateKey;
                }
                AddSampleKeyIfNotExists(state, keyName, rootKey["ValueSpec"]);
            }
        }

        private void AddSampleKeyIfNotExists(JObject state, string? keyName, JToken? valueSpec)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                return;

            if (state.ContainsKey(keyName))
                return;

            var type = valueSpec!["ValueType"]!.ToString();
            JToken value = type switch
            {
                "Object" => new JObject(),
                "Array" => new JArray(),
                _ => new JValue(CreateSampleString(type, keyName, valueSpec["AcceptedValues"] as JArray))
            };
            AddSampleValue(value, valueSpec);
            state.Add(keyName, value);
        }

        private void AddSampleValue(JToken value, JToken valueSpec)
        {
            if (value is JObject)
            {
                foreach (var objectSpec in valueSpec["ObjectSpecs"]!)
                {
                    var innerValueSpec = objectSpec["ValueSpec"];
                    var innerType = innerValueSpec!["ValueType"]!.ToString();
                    JToken innerValue = innerType switch
                    {
                        "Object" => new JObject(),
                        "Array" => new JArray(),
                        _ => new JValue(CreateSampleString(innerType, objectSpec["Name"]!.ToString(), innerValueSpec["AcceptedValues"] as JArray))
                    };
                    AddSampleValue(innerValue, innerValueSpec);
                    ((JObject)value).Add(objectSpec["Name"]!.ToString().Replace("@", "your_"), innerValue);
                }
            }
            else if (value is JArray)
            {
                var innerValueSpec = valueSpec["InnerSpec"];
                var innerType = innerValueSpec!["ValueType"]!.ToString();
                JToken innerValue = innerType switch
                {
                    "Object" => new JObject(),
                    "Array" => new JArray(),
                    _ => new JValue(CreateSampleString(innerType, "arrayValue", innerValueSpec["AcceptedValues"] as JArray))
                };
                AddSampleValue(innerValue, innerValueSpec);
                ((JArray)value).Add(innerValue);
            }
        }

        private string CreateSampleString(string type, string keyDescriptor, JArray? acceptedValues)
        {
            return type switch
            {
                "JsonPath" => "$.yourStateKey",
                "StoragePath" => "prefix:folder/yourFile.txt",
                "LocalPath" => "folder1/folder2/yourFile.txt",
                "Integer" => "10",
                "Number" => "12.3",
                "Boolean" => "False",
                "Key" => "yourStateKey",
                "Expression" => "1 + 2",
                "String" => "yourText",
                "Enum" => string.Join("|", (acceptedValues ?? new JArray()).Select(x => x.ToString())) + "|CustomValue",
                "Date" => "2020/01/01",
                "Constant" => "Txt_File_In_Constants_Folder",
                _ => string.Empty
            };
        }
    }
}

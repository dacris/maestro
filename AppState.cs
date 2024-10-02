using Newtonsoft.Json.Linq;
using System.Collections.Immutable;

namespace Dacris.Maestro;

public class AppState
{
    public HashSet<string> SensitiveKeys { get; set; } = new();
    public JObject StateObject { get; set; } = JObject.Parse("{}");
    public ImmutableDictionary<string, string> Constants { get; init; }
    public static readonly AppState Instance;
    private readonly ImmutableDictionary<string, string> _defaultConstants = new Dictionary<string, string>()
            {
                { "_SelectMock", "SELECT * FROM _tmp" },
                { "_DeleteMock", "DELETE FROM _tmp" }
            }.ToImmutableDictionary();
    
    static AppState()
    {
        Instance = new AppState();
    }

    public AppState()
    {
        if (!Directory.Exists("Constants"))
        {
            Constants = _defaultConstants;
            return;
        }

        var constants = new Dictionary<string, string>();
        var constantFiles = Directory.EnumerateFiles("Constants");
        foreach (var defaultConstant in _defaultConstants.Keys)
        {
            constants[defaultConstant] = _defaultConstants[defaultConstant];
        }
        foreach (var path in constantFiles)
        {
            var contents = File.ReadAllText(path);
            constants[Path.GetFileNameWithoutExtension(path)] = contents;
        }
        Constants = constants.ToImmutableDictionary();
    }

    public void Reload()
    {
        SensitiveKeys = new HashSet<string>();
        StateObject = JObject.Parse("{}");
        //Won't reload constants; they should never change!
    }

    public bool IsMock()
    {
        return ReadKey("mock")?.ToString().ToLowerInvariant() == "true";
    }

    public void WriteKey(string key, JToken value, bool sensitive = false)
    {
        var obj = StateObject;
        obj.Remove(key);
        obj.Add(key, value);
        StateObject = obj;
        if (sensitive)
        {
            SensitiveKeys.Remove(key);
            SensitiveKeys.Add(key);
            SensitiveKeys.Any(s => s.Equals(key)).ShouldBe(true);
        }
        ReadKey(key)!.ToString().ShouldBe(value.ToString());
    }

    public void WritePath(string outputPath, JToken outputValue)
    {
        UpdateJson(StateObject, outputPath, outputValue);
        if (outputValue.Type == JTokenType.Null)
        {
            StateObject.SelectToken(outputPath).ShouldBe(null);
        }
        else
        {
            StateObject.SelectToken(outputPath)!.ToString().ShouldBe(outputValue.ToString());
        }
    }

    public void ClearKey(string key)
    {
        var obj = StateObject;
        obj.Remove(key);
        StateObject = obj;
        ReadKey(key).ShouldBe(null);
    }

    public JToken? ReadKey(string key)
    {
        var obj = StateObject;
        return obj[key];
    }

    public void SanitizeSensitiveKeys()
    {
        foreach (string key in SensitiveKeys)
        {
            ClearKey(key);
        }
    }

    private static void UpdateJson(JToken source, string path, JToken value)
    {
        if (source.SelectToken(path) is not null)
        {
            source.SelectToken(path)?.Replace(value);
            return;
        }
        if (path.StartsWith("$."))
        {
            path = path.Substring(2);
        }
        UpdateJsonInternal(source, path.Split('.'), 0, value);
    }

    private static void UpdateJsonInternal(JToken source, string[] path, int pathIndex, JToken value)
    {
        if (pathIndex == path.Length - 1)
        {
            if (((JObject)source)[path[pathIndex]] == null)
            {
                ((JObject)source).Add(path[pathIndex], value);
            }
            else
            {
                if (value.Type == JTokenType.Null)
                {
                    ((JObject)source).Remove(path[pathIndex]);
                }
                else
                {
                    ((JObject)source)[path[pathIndex]] = value;
                }
            }
        }
        else if (source is JObject)
        {
            if (((JObject)source)[path[pathIndex]] == null)
            {
                ((JObject)source).Add(path[pathIndex], new JObject());
            }
            UpdateJsonInternal(((JObject)source)[path[pathIndex]]!, path, pathIndex + 1, value);
        }
    }
}

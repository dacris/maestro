using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Core;

public class ReadStateFromFile : Interaction
{
    public override void Specify()
    {
        // no input
    }

    public override async Task RunAsync()
    {
        if (InputStateKey != null) // If an input key is provided, read only that key from file
        {
            var obj = JToken.Parse(await File.ReadAllTextAsync(InputStateKey + ".json"));
            AppState.Instance.WriteKey(InputStateKey, obj);
        }
        else // If no input key is provided, read entire state from State.json
        {
            var json = await File.ReadAllTextAsync(AppState.Instance.IsMock() ? "MockState.json" : "State.json");
            AppState.Instance.StateObject = JObject.Parse(json);
            if (AppState.Instance.IsMock())
            {
                AppState.Instance.WriteKey("_mockFile", "Mock.json");
                await new MergeStateFromFile() { InputStateKey = "_mockFile" }.RunAsync();
                AppState.Instance.ClearKey("_mockFile");
            }
        }
    }
}

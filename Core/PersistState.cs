using Newtonsoft.Json;

namespace Dacris.Maestro.Core;

public class PersistState : Interaction
{
    public override void Specify()
    {
        Description = "Writes JSON state to a file.";
        // no input
    }

    public override async Task RunAsync()
    {
        if (InputStateKey != null) // If an input key is provided, write only that key to file
        {
            using JsonWriter writer = new JsonTextWriter(new StreamWriter(InputStateKey + ".json", false));
            writer.Formatting = Formatting.Indented;
            await InputState!.WriteToAsync(writer);
        }
        else // If no input key is provided, write entire state to State.json
        {
            AppState.Instance.SanitizeSensitiveKeys();
            var state = AppState.Instance.StateObject;
            var stateJson = JsonConvert.SerializeObject(state, Formatting.Indented);
            await File.WriteAllTextAsync(AppState.Instance.IsMock() ? "MockState.json" : "State.json", stateJson);
        }
    }
}

using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Core;
public class MergeStateFromFile : Interaction
{
    public override void Specify()
    {
        Description = "Merges state from a JSON file into memory.";
        InputSpec.AddDefaultInput();
        InputSpec.Inputs[0].WithSimpleType(ValueTypeSpec.LocalPath);
    }

    public override async Task RunAsync()
    {
        var json = await File.ReadAllTextAsync(InputState!.ToString());
        JObject inMemory = AppState.Instance.StateObject;
        JObject incoming = JObject.Parse(json);
        inMemory.Merge(incoming);
    }
}

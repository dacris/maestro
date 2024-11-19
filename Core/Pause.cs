using Newtonsoft.Json;

namespace Dacris.Maestro.Core;

public class Pause : Interaction
{
    public override void Specify()
    {
    }

    public override async Task RunAsync()
    {
        // Save state snapshot
        AppState.Instance.SanitizeSensitiveKeys();
        var state = AppState.Instance.StateObject;
        var stateJson = JsonConvert.SerializeObject(state, Formatting.Indented);
        await File.WriteAllTextAsync("StateSnapshot.json", stateJson);

        // Wait for continue signal
        System.Console.WriteLine("Press any key [State dumped to StateSnapshot.json; Delete the file to continue]...");
        while (!System.Console.KeyAvailable)
        {
            if (!File.Exists("StateSnapshot.json"))
                break;
            await Task.Delay(10);
        }
        if (System.Console.KeyAvailable)
        {
            System.Console.ReadKey();
        }
        if (File.Exists("StateSnapshot.json"))
        {
            File.Delete("StateSnapshot.json");
        }
    }
}
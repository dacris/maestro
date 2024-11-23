namespace Dacris.Maestro.Core;

public class ReadStringFromFile : Interaction
{
    public override void Specify()
    {
        Description = "Reads a string from a file.";
        InputSpec.AddInputs("inputFile", "outputPath");
    }

    public override async Task RunAsync()
    {
        var str = await File.ReadAllTextAsync(InputState!["inputFile"]!.ToString());
        AppState.Instance.WritePath(InputState!["outputPath"]!.ToString(), str);
    }
}

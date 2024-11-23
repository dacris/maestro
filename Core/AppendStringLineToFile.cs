
namespace Dacris.Maestro.Core
{
    public class AppendStringLineToFile : Interaction
    {
        public override void Specify()
        {
            Description = "Appends a string to a file as a single line.";
            InputSpec.AddInputs("inputPath", "outputFile");
        }

        public override async Task RunAsync()
        {
            var contents = AppState.Instance.StateObject.SelectToken(InputState!["inputPath"]!.ToString())!.ToString();
            await File.AppendAllLinesAsync(InputState["outputFile"]!.ToString(), new string[] { contents });
        }
    }
}

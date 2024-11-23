using System.Diagnostics;

namespace Dacris.Maestro.Core;

public class RunExe : Interaction
{
    public override void Specify()
    {
        Description = "Runs a command line.";
        InputSpec.AddInputs("workingDir", "args", "exe");
        InputSpec.StateObjectKey("exe").WithSimpleType(ValueTypeSpec.LocalPath);
    }

    public override async Task RunAsync()
    {
        var path = InputState!["workingDir"]?.ToString() ?? Environment.CurrentDirectory;
        var args = InputState!["args"]?.ToString() ?? string.Empty;
        var exe = InputState!["exe"]!.ToString();
        using var process = Process.Start(new ProcessStartInfo { WorkingDirectory = path, FileName = exe, Arguments = args });
        await process!.WaitForExitAsync();
    }
}

namespace Dacris.Maestro.Core;
public class Log : Interaction
{
    public override void Specify()
    {
        Description = "Writes a message to Serilog sinks.";
        InputSpec.AddInputs("level", "message");
    }

    public override async Task RunAsync()
    {
        await Task.CompletedTask;
        switch (InputState!["level"]!.ToString().ToLowerInvariant())
        {
            case "error":
                AppState.Instance.Logger?.Error(InputState!["message"]!.ToString());
                break;
            case "fatal":
                AppState.Instance.Logger?.Fatal(InputState!["message"]!.ToString());
                break;
            case "debug":
                AppState.Instance.Logger?.Debug(InputState!["message"]!.ToString());
                break;
            case "information":
                AppState.Instance.Logger?.Information(InputState!["message"]!.ToString());
                break;
            case "warning":
                AppState.Instance.Logger?.Warning(InputState!["message"]!.ToString());
                break;
        }
    }
}

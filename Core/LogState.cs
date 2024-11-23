namespace Dacris.Maestro.Core;
public class LogState : Interaction
{
    public override void Specify()
    {
        Description = "Writes a piece of the state to Serilog sinks.";
        InputSpec.AddDefaultInput();
    }

    public override async Task RunAsync()
    {
        await Task.CompletedTask;
        AppState.Instance.Logger?.Debug($"State path: {InputState} = {AppState.Instance.StateObject.SelectToken(InputState!.ToString())}");
    }
}

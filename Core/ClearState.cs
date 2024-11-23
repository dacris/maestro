namespace Dacris.Maestro.Core;

public class ClearState : Interaction
{
    public override void Specify()
    {
        Description = "Removes a key from the state.";
        // no input
    }

    public override Task RunAsync()
    {
        AppState.Instance.ClearKey(InputStateKey!);
        return Task.CompletedTask;
    }
}

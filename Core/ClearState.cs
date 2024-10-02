namespace Dacris.Maestro.Core;

public class ClearState : Interaction
{
    public override void Specify()
    {
        // no input
    }

    public override Task RunAsync()
    {
        AppState.Instance.ClearKey(InputStateKey!);
        return Task.CompletedTask;
    }
}

namespace Dacris.Maestro.Core;

public class ReadSystemVariable : Interaction
{
    public override void Specify()
    {
        InputSpec.AddDefaultInput();
        InputSpec.Inputs[0].ValueSpec = new ValueSpec { ValueType = ValueTypeSpec.Enum, AcceptedValues = ["now", "nowUtc"] };
    }

    public override Task RunAsync()
    {
        switch (InputStateKey)
        {
            case "now":
                AppState.Instance.WriteKey("now", DateTime.Now);
                break;
            case "nowUtc":
                AppState.Instance.WriteKey("nowUtc", DateTime.UtcNow);
                break;
            default:
                AppState.Instance.WriteKey(InputStateKey!, Environment.GetEnvironmentVariable(InputStateKey!), true);
                break;
        }
        return Task.CompletedTask;
    }
}

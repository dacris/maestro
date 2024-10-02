
namespace Dacris.Maestro.Core
{
    public class MakeSensitive : Interaction
    {
        public override void Specify()
        {
            // no input
        }

        public override Task RunAsync()
        {
            AppState.Instance.SensitiveKeys.Remove(InputStateKey!);
            AppState.Instance.SensitiveKeys.Add(InputStateKey!);
            return Task.CompletedTask;
        }
    }
}

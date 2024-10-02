
namespace Dacris.Maestro.Core
{
    public class NoOp : Interaction
    {
        public override void Specify()
        {
            /* no op */
        }

        public override async Task RunAsync()
        {
            await Task.CompletedTask;
        }
    }
}

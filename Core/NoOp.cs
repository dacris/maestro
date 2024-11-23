
namespace Dacris.Maestro.Core
{
    public class NoOp : Interaction
    {
        public override void Specify()
        {
            Description = "Does nothing.";
            AiExclude = true;
            /* no op */
        }

        public override async Task RunAsync()
        {
            await Task.CompletedTask;
        }
    }
}


namespace Dacris.Maestro.Core
{
    public class Delay : Interaction
    {
        public override async Task RunAsync()
        {
            await Task.Delay(int.Parse(InputState!["timeout"]!.ToString()));
        }

        public override void Specify()
        {
            Description = "Waits for specified time period.";
            InputSpec.AddTimeout();
        }
    }
}

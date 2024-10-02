
namespace Dacris.Maestro.Core
{
    public class RunSubApp : Interaction
    {
        public override void Specify()
        {
            InputSpec.AddDefaultInput();
        }

        public override async Task RunAsync()
        {
            System.Console.WriteLine($"Running plain English app: {InputState} from saved file...");
            Block myBlock = await Block.ReadFromFileAsync(InputState!.ToString(), true);
            if (Options.TryGetValue("quiet", out var quiet))
            {
                myBlock.Options.Add("quiet", quiet);
            }
            await myBlock.RunAsync();
        }
    }
}

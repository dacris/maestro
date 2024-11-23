
namespace Dacris.Maestro.Core
{
    public class RaiseError : Interaction
    {
        public override Task RunAsync()
        {
            throw new Exception(InputState?.ToString() ?? "Unspecified error");
        }

        public override void Specify()
        {
            Description = "Reports an error.";
            InputSpec.AddDefaultInput();
        }
    }
}

namespace Dacris.Maestro.Zip
{
    public class Compress : Interaction
    {
        public override void Specify()
        {
            InputSpec.AddDefaultInput();
            InputSpec.Inputs[0].WithSimpleType(ValueTypeSpec.LocalPath);
        }

        public override Task RunAsync()
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(InputState!.ToString(), InputStateKey + ".zip");
            File.Exists(InputStateKey + ".zip").ShouldBe(true);
            return Task.CompletedTask;
        }
    }
}

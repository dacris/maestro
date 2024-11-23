namespace Dacris.Maestro.Zip
{
    public class Decompress : Interaction
    {
        public override void Specify()
        {
            Description = "Decompresses a ZIP to a directory.";
            InputSpec.AddInputs("outputDir", "inputFile");
        }

        public override Task RunAsync()
        {
            var destination = InputState!["outputDir"]!.ToString();
            var source = InputState!["inputFile"]!.ToString();
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(source, destination, true);
            Directory.Exists(destination).ShouldBe(true);
            return Task.CompletedTask;
        }
    }
}

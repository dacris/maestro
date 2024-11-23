namespace Dacris.Maestro.Core
{
    public class MakeRandom : Interaction
    {
        private static bool hasDoneSelfTest = false;
        public override void Specify()
        {
            Description = "Makes a random value in the state.";
            InputSpec.AddInputs("type", "start", "end", "outputPath");
            InputSpec.StateObjectKey("type").ValueSpec = new ValueSpec
            {
                ValueType = ValueTypeSpec.Enum,
                AcceptedValues = ["boolean", "integer", "number", "date"]
            };
        }

        private void Generate(string? type, string? start, string? end, string outputPath)
        {
            switch (type ?? "integer")
            {
                case "boolean":
                    {
                        var i = Random.Shared.Next(0, 2);
                        AppState.Instance.WritePath(outputPath, i == 0 ? "False" : "True");
                        break;
                    }
                case "integer":
                    {
                        var j = Random.Shared.NextInt64(long.Parse(start!), long.Parse(end!) + 1);
                        AppState.Instance.WritePath(outputPath, j.ToString());
                        break;
                    }
                case "number":
                    {
                        var endN = double.Parse(end!);
                        var startN = double.Parse(start!);
                        var n = startN + Random.Shared.NextDouble() * (endN - startN);
                        AppState.Instance.WritePath(outputPath, n.ToString("F9"));
                    }
                    break;
                case "date":
                    {
                        var endD = DateTime.Parse(end!).Date;
                        var startD = DateTime.Parse(start!).Date;
                        var newDate = startD.AddDays(Random.Shared.NextDouble() * (endD - startD).TotalDays);
                        AppState.Instance.WritePath(outputPath, newDate.ToString());
                        break;
                    }
                default:
                    break;
            }
        }

        private void SelfTest(string type, string? start, string? end, string outputPath)
        {
            Generate(type, start, end, outputPath);
            switch (type)
            {
                case "boolean":
                    bool.Parse(AppState.Instance.ReadKey(outputPath)!.ToString());
                    break;
                case "date":
                    DateTime.Parse(AppState.Instance.ReadKey(outputPath)!.ToString()).ShouldBeBetween(start!, end!);
                    break;
                case "integer":
                    long.Parse(AppState.Instance.ReadKey(outputPath)!.ToString()).ShouldBeBetween(start!, end!);
                    break;
                case "number":
                    double.Parse(AppState.Instance.ReadKey(outputPath)!.ToString()).ShouldBeBetween(start!, end!);
                    break;
            }
            AppState.Instance.ClearKey(outputPath);
        }

        public override Task RunAsync()
        {
            if (!hasDoneSelfTest)
            {
                SelfTest("boolean", null, null, "_randomSelfCheck");
                SelfTest("boolean", null, null, "_randomSelfCheck");
                SelfTest("boolean", null, null, "_randomSelfCheck");
                SelfTest("boolean", null, null, "_randomSelfCheck");

                for (int i = 0; i < 10000; i++)
                {
                    SelfTest("integer", "-10", "10", "_randomSelfCheck");
                    SelfTest("number", "-11.5", "11.5", "_randomSelfCheck");
                    SelfTest("date", "2021/01/01", "2022/01/01", "_randomSelfCheck");
                }
            }

            hasDoneSelfTest = true;

            Generate(InputState!["type"]?.ToString(), InputState["start"]?.ToString(), InputState["end"]?.ToString(),
                InputState["outputPath"]!.ToString());
            
            return Task.CompletedTask;
        }
    }
}

using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Core
{
    public class TransferState : Interaction
    {
        public override void Specify()
        {
            Description = "Transfers multiple state values within state.";
            InputSpec.AddInputs("inputPaths", "outputPaths");
            InputSpec.Inputs[0].ValueSpec.ObjectSpecs!.ForEach(x => x.ValueSpec = new ValueSpec
            {
                ValueType = ValueTypeSpec.Array,
                InnerSpec = new ValueSpec
                {
                    ValueType = ValueTypeSpec.JsonPath
                }
            });
        }

        public override Task RunAsync()
        {
            var inputPaths = InputState!["inputPaths"]!.Select(x => x.ToString()).ToList();
            var outputPaths = InputState!["outputPaths"]!.Select(x => x.ToString()).ToList();
            for (int i = 0; i < inputPaths.Count; i++)
            {
                if (string.IsNullOrEmpty(outputPaths[i]?.ToString()))
                {
                    AppState.Instance.WritePath(inputPaths[i], JValue.CreateNull());
                    continue;
                }
                var outputValue = AppState.Instance.StateObject.SelectToken(inputPaths[i]);
                if (outputValue is not null)
                {
                    AppState.Instance.WritePath(outputPaths[i], outputValue);
                }
            }
            return Task.CompletedTask;
        }
    }
}

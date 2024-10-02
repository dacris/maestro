
using OllamaSharp;
using System.Text;

namespace Dacris.Maestro.AI
{
    public class Chatbot : Interaction
    {
        public override void Specify()
        {
            InputSpec.AddInputs("chatSettingsPath", "prompt", "isCode");
            InputSpec.StateObjectKey("prompt").WithSimpleType(ValueTypeSpec.String);
            InputSpec.StateObjectKey("isCode").WithSimpleType(ValueTypeSpec.Boolean);
            InputSpec.Inputs.Add(new InputSpec("chatSettings")
            {
                ValueSpec =
                {
                    ValueType = ValueTypeSpec.Object,
                    ObjectSpecs = [new InputSpec
                        {
                            Name = "@chatConfiguration",
                            ValueSpec = new ValueSpec
                            {
                                ValueType = ValueTypeSpec.Object,
                                ObjectSpecs = [
                                    new InputSpec("systemType")
                                    {
                                        ValueSpec = new ValueSpec
                                        {
                                            ValueType = ValueTypeSpec.Enum,
                                            AcceptedValues = ["OpenAI", "Ollama"]
                                        }
                                    },
                                    new InputSpec("secretKey").DependsOn("systemType", ["OpenAI"]).WithSimpleType(ValueTypeSpec.Key),
                                    new InputSpec("model").DependsOn("systemType", ["Ollama"]),
                                    new InputSpec("port").DependsOn("systemType", ["Ollama"]).WithSimpleType(ValueTypeSpec.Integer)
                                ]
                            }
                        }]
                }
            });
        }

        public override async Task RunAsync()
        {
            // TODO: Support OpenAI (v1.2)
            var config = AppState.Instance.ReadKey("chatSettings")!
                        .SelectToken(InputState!["chatSettingsPath"]!.ToString())!;
            var port = int.Parse(config["port"]?.ToString() ?? "11434");
            // set up the client
            var uri = new Uri("http://localhost:" + port);
            var ollama = new OllamaApiClient(uri);
            // select a model which should be used for further operations
            ollama.SelectedModel = config["model"]!.ToString();
            ConversationContext? context = null;
            StringBuilder stringBuilder = new StringBuilder();
            await foreach (var stream in ollama.StreamCompletion(InputState["prompt"]!.ToString(), context))
            {
                stringBuilder.Append(stream?.Response ?? string.Empty);
            }
            var response = stringBuilder.ToString();
            if (InputState["isCode"]?.ToString().ToLowerInvariant() == "true")
            {
                var idx = response.IndexOf("```");
                var nextLine = response.IndexOf('\n', idx) + 1;
                var endIdx = response.IndexOf("```", nextLine);
                response = response.Substring(nextLine, endIdx - nextLine);
            }
            File.WriteAllText("ChatResponse.txt", response);
        }
    }
}

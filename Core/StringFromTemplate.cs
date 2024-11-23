using System.Text.RegularExpressions;

namespace Dacris.Maestro.Core;

public class StringFromTemplate : Interaction
{
    public override void Specify()
    {
        Description = "Creates a formatted string from a template and saves it in the state.";
        AiExclude = true;
        InputSpec.AddInputs("template", "outputPath", "format");
        InputSpec.StateObjectKey("format").ValueSpec = new ValueSpec
        {
            ValueType = ValueTypeSpec.Enum,
            AcceptedValues = FormatValue.StringFormats
        };
    }

    public override Task RunAsync()
    {
        var input = InputState!;
        var template = input["template"]!.ToString();
        var outputPath = input["outputPath"]!.ToString();
        var format = input["format"]?.ToString() ?? "txt";
        AppState.Instance.WriteKey("_templateLevel", "1");
        AppState.Instance.WriteKey("_templateFormat", format);
        var outputValue = Regex.Replace(template, StringTemplate.Regex, MatchEval);
        AppState.Instance.ClearKey("_templateLevel");
        AppState.Instance.ClearKey("_templateFormat");
        AppState.Instance.WritePath(outputPath, outputValue);
        return Task.CompletedTask;
    }

    private string MatchEval(Match match) => StringTemplate.MatchEval(match);
}

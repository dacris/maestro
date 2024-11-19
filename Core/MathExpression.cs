using Newtonsoft.Json.Linq;
using org.matheval;

namespace Dacris.Maestro.Core;

public class MathExpression : Interaction
{
    public override void Specify()
    {
        InputSpec.AddInputs("formula", "outputPath", "precision", "@operand");
        InputSpec.StateObjectKey("precision")
            .ValueSpec = new ValueSpec
            {
                ValueType = ValueTypeSpec.Integer
            };
        InputSpec.StateObjectKey("@operand")
            .ValueSpec = new ValueSpec
            {
                ValueType = ValueTypeSpec.JsonPath
            };
    }

    public override Task RunAsync()
    {
        Expression e = new Expression(InputState!["formula"]?.ToString() ?? "0 + 0");
        foreach (JProperty parameter in InputState!)
        {
            if (parameter.Name == "formula" || parameter.Name == "outputPath" || parameter.Name == "precision")
                continue;

            var valueAsString = AppState.Instance.StateObject.SelectToken(parameter.Value.ToString())!.ToString();
            object parsedValue = 0;
            if (bool.TryParse(valueAsString, out var b))
            {
                parsedValue = b;
            }
            else if (decimal.TryParse(valueAsString, out var num))
            {
                parsedValue = num;
            }
            e.Bind(parameter.Name, parsedValue);
        }
        object output;
        output = e.Eval();
        List<string> errors = e.GetError();
        if (errors.Count > 0)
        {
            foreach (string error in errors)
            {
                System.Console.Error.WriteLine(error);
            }
            return Task.CompletedTask;
        }
        var outputStr = output.ToString();
        if (typeof(decimal).IsAssignableFrom(output.GetType()))
        {
            outputStr = ((decimal)output).ToString("F" + int.Parse(InputState["precision"]?.ToString() ?? "9"));
        }
        AppState.Instance.WritePath(InputState["outputPath"]!.ToString(), outputStr);
        return Task.CompletedTask;
    }
}

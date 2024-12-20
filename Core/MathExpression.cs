﻿using Newtonsoft.Json.Linq;
using org.matheval;

namespace Dacris.Maestro.Core;

public class MathExpression : Interaction
{
    public override void Specify()
    {
        Description = "Evaluates a math or boolean formula.";
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
            object parsedValue = valueAsString;
            if (bool.TryParse(valueAsString, out var b))
            {
                parsedValue = b;
            }
            else if (decimal.TryParse(valueAsString, out var num))
            {
                parsedValue = num;
            }
            else if (DateTime.TryParse(valueAsString, out var dt))
            {
                parsedValue = (dt - new DateTime(1899, 12, 30)).TotalDays;
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

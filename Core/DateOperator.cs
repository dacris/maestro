
using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Core;

public class DateOperator : Interaction
{
    public override void Specify()
    {
        InputSpec.AddInputs("operation", "outputPath", "@operand");
        InputSpec.StateObjectKey("operation").ValueSpec = new ValueSpec
        {
            ValueType = ValueTypeSpec.Enum,
            AcceptedValues = ["addDays",
                "addSeconds",
                "addMinutes",
                "addHours",
                "addYears",
                "addMonths",
                "dayOfWeek",
                "dayOfYear",
                "day",
                "month",
                "year",
                "millisecond",
                "second",
                "minute",
                "hour"]
        };
        InputSpec.StateObjectKey("@operand").ValueSpec = new ValueSpec
        {
            ValueType = ValueTypeSpec.JsonPath
        };
    }

    public override Task RunAsync()
    {
        string op = InputState!["operation"]!.ToString();
        object?[] inputs = InputState!
            .Where(x => new string[] { "operation", "outputPath" }.Contains(((JProperty)x).Name) == false)
            .OrderBy(x => x.Path)
            .Select(x => {
                var v = AppState.Instance.StateObject.SelectToken(((JProperty)x).Value.ToString());
                if (double.TryParse(v!.ToString(), out var num))
                {
                    return (object)num;
                }
                else if (DateTime.TryParse(v.ToString(), out var date))
                {
                    return date;
                }
                throw new Exception("Invalid argument type!");
            })
            .ToArray();

        object? output = null;
        switch (op)
        {
            //todo: add more ops here
            case "addDays":
                output = ((DateTime)inputs[0]!).AddDays((double)inputs[1]!);
                break;
            case "addSeconds":
                output = ((DateTime)inputs[0]!).AddSeconds((double)inputs[1]!);
                break;
            case "addMinutes":
                output = ((DateTime)inputs[0]!).AddMinutes((double)inputs[1]!);
                break;
            case "addHours":
                output = ((DateTime)inputs[0]!).AddHours((double)inputs[1]!);
                break;
            case "addYears":
                output = ((DateTime)inputs[0]!).AddYears((int)(double)inputs[1]!);
                break;
            case "addMonths":
                output = ((DateTime)inputs[0]!).AddMonths((int)(double)inputs[1]!);
                break;
            case "dayOfWeek":
                output = ((int)(((DateTime)inputs[0]!).DayOfWeek)).ToString();
                break;
            case "dayOfYear":
                output = ((DateTime)inputs[0]!).DayOfYear.ToString();
                break;
            case "day":
                output = ((DateTime)inputs[0]!).Day.ToString();
                break;
            case "month":
                output = ((DateTime)inputs[0]!).Month.ToString();
                break;
            case "year":
                output = ((DateTime)inputs[0]!).Year.ToString();
                break;
            case "millisecond":
                output = ((DateTime)inputs[0]!).Millisecond.ToString();
                break;
            case "second":
                output = ((DateTime)inputs[0]!).Second.ToString();
                break;
            case "minute":
                output = ((DateTime)inputs[0]!).Minute.ToString();
                break;
            case "hour":
                output = ((DateTime)inputs[0]!).Hour.ToString();
                break;
        }
        AppState.Instance.WritePath(InputState!["outputPath"]!.ToString(), new JValue(output));
        return Task.CompletedTask;
    }
}

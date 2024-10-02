using Newtonsoft.Json;
using System.Text;
using System.Text.Encodings.Web;

namespace Dacris.Maestro.Core;

public class FormatValue : Interaction
{
    public static readonly string[] StringFormats = ["txt", "url", "html", "json", "csv", "toBase64UTF8", "fromBase64UTF8"];
    public override void Specify()
    {
        InputSpec.AddInputs("inputPath", "format", "outputPath");
    }

    public override Task RunAsync()
    {
        var valueAsString = AppState.Instance.StateObject.SelectToken(InputState!["inputPath"]!.ToString())?.ToString();
        var format = InputState["format"]!.ToString();
        if (decimal.TryParse(valueAsString, out var num))
        {
            AppState.Instance.WritePath(InputState["outputPath"]!.ToString(), num.ToString(format));
        }
        else if (DateTime.TryParse(valueAsString, out var date))
        {
            AppState.Instance.WritePath(InputState["outputPath"]!.ToString(), date.ToString(format));
        }
        else
        {
            EncodeValue(format, valueAsString!, InputState["outputPath"]!.ToString());
        }
        return Task.CompletedTask;
    }

    public static void EncodeValue(string format, string valueAsString, string outputPath)
    {
        switch (format)
        {
            case "txt":
                AppState.Instance.WritePath(outputPath, valueAsString);
                break;
            case "url":
                var urlE = System.Net.WebUtility.UrlEncode(valueAsString);
                AppState.Instance.WritePath(outputPath, urlE);
                break;
            case "html":
                HtmlEncoder encoder = HtmlEncoder.Default;
                var htmlE = encoder.Encode(valueAsString);
                AppState.Instance.WritePath(outputPath, htmlE);
                break;
            case "json":
                var json = JsonConvert.ToString(valueAsString);
                AppState.Instance.WritePath(outputPath, json);
                break;
            case "csv":
                var csv = "\"" + valueAsString.Replace("\"", "\"\"") + "\"";
                AppState.Instance.WritePath(outputPath, csv);
                break;
            case "toBase64UTF8":
                var base64e = Convert.ToBase64String(Encoding.UTF8.GetBytes(valueAsString));
                AppState.Instance.WritePath(outputPath, base64e);
                break;
            case "fromBase64UTF8":
                var base64d = Encoding.UTF8.GetString(Convert.FromBase64String(valueAsString));
                AppState.Instance.WritePath(outputPath, base64d);
                break;
            default:
                System.Console.Error.WriteLine($"Unsupported format: {format}");
                break;
        }
    }
}

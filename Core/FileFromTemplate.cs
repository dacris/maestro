using System.Text.RegularExpressions;

namespace Dacris.Maestro.Core;

public abstract class FileFromTemplate : Interaction
{
    public override void Specify()
    {
        InputSpec.AddDefaultInput();
        InputSpec.Inputs[0].WithSimpleType(ValueTypeSpec.LocalPath);
    }

    protected async Task RunAsync(string format)
    {
        var fileName = AppState.Instance.ReadKey(InputStateKey ?? format + "TemplateFile");
        var template = await File.ReadAllTextAsync(fileName!.ToString());
        AppState.Instance.WriteKey("_templateLevel", "1");
        AppState.Instance.WriteKey("_templateFormat", format);
        var output = Regex.Replace(template, StringTemplate.Regex, StringTemplate.MatchEval);
        AppState.Instance.ClearKey("_templateLevel");
        AppState.Instance.ClearKey("_templateFormat");
        await File.WriteAllTextAsync("Output." + format, output);
    }
}

public class HtmlFileFromTemplate : FileFromTemplate
{
    public override async Task RunAsync()
    {
        await RunAsync("html");
    }

    public override void Specify()
    {
        base.Specify();
        Description = "Creates an HTML file from a JSON array or object in the state.";
    }
}

public class TxtFileFromTemplate : FileFromTemplate
{
    public override async Task RunAsync()
    {
        await RunAsync("txt");
    }

    public override void Specify()
    {
        base.Specify();
        Description = "Creates an text file from a JSON array or object in the state.";
    }
}

public class CsvFileFromTemplate : FileFromTemplate
{
    public override async Task RunAsync()
    {
        await RunAsync("csv");
    }

    public override void Specify()
    {
        base.Specify();
        Description = "Creates a CSV file from a JSON array or object in the state.";
    }
}

public class JsonFileFromTemplate : FileFromTemplate
{
    public override async Task RunAsync()
    {
        await RunAsync("json");
    }

    public override void Specify()
    {
        base.Specify();
        Description = "Creates an JSON file from a JSON array or object in the state.";
    }
}

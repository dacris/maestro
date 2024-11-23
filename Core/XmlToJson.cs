using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace Dacris.Maestro.Core;

public class XmlToJson : Interaction
{
    public override void Specify()
    {
        Description = "Converts an XML document to JSON.";
        InputSpec.AddInputs("inputFile", "outputFile", "outputPath");
    }

    public override async Task RunAsync()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(InputState!["inputFile"]!.ToString());
        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
        var outputFile = InputState!["outputFile"]?.ToString();
        var outputPath = InputState!["outputPath"]?.ToString();
        if (outputPath is not null)
        {
            AppState.Instance.WritePath(outputPath, JToken.Parse(json));
        }
        else
        {
            await File.WriteAllTextAsync(outputFile!, json);
        }
    }
}

public class JsonToXml : Interaction
{
    public override void Specify()
    {
        Description = "Converts a JSON file to XML.";
        InputSpec.AddInputs("inputFile", "outputFile", "xmlRoot");
    }

    public override async Task RunAsync()
    {
        var json = await File.ReadAllTextAsync(InputState!["inputFile"]!.ToString());
        var xml = JsonConvert.DeserializeXNode(json, InputState!["xmlRoot"]?.ToString());
        var outputFile = InputState!["outputFile"]!.ToString();
        if (xml is null)
        {
            throw new Exception("Failed to create XML from JSON.");
        }
        using var xw = XmlWriter.Create(outputFile!, new XmlWriterSettings { Async = true, WriteEndDocumentOnClose = true, Indent = true });
        await xml.WriteToAsync(xw, CancellationToken.None);
        xw.Close();
    }
}

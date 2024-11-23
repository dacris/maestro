using Csv;
using FluentEmail.Core;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace Dacris.Maestro.Storage;

public class MoveFile : Interaction
{
    public override void Specify()
    {
        Description = "Moves a file.";
        InputSpec.AddInputs("inputFile", "outputFile");
        InputSpec.UseStoragePaths();
        InputSpec.AddStorageAbstractionInput();
        InputSpec.AddRetry();
    }

    public override async Task RunAsync()
    {
        var source = InputState!["inputFile"]!.ToString();
        var destination = InputState["outputFile"]!.ToString();
        await RetryAsync(async (o) =>
        {
            var sourceLocation = new FileLocation(this, source);
            var destinationLocation = new FileLocation(this, destination);
            using (var sourceStream = await sourceLocation.FileProvider.ReadAsync(sourceLocation))
                await destinationLocation.FileProvider.WriteAsync(sourceStream, destinationLocation);
            await sourceLocation.FileProvider.DeleteAsync(sourceLocation);
            (await destinationLocation.FileProvider.ExistsAsync(destinationLocation)).ShouldBe(true);
            (await sourceLocation.FileProvider.ExistsAsync(sourceLocation)).ShouldBe(false);
        }, (object?)null);
    }
}

public class CopyFile : Interaction
{
    public override void Specify()
    {
        Description = "Copies a file.";
        InputSpec.AddInputs("inputFile", "outputFile");
        InputSpec.UseStoragePaths();
        InputSpec.AddStorageAbstractionInput();
        InputSpec.AddRetry();
    }

    public override async Task RunAsync()
    {
        var source = InputState!["inputFile"]!.ToString();
        var destination = InputState["outputFile"]!.ToString();
        await RetryAsync(async (o) =>
        {
            var sourceLocation = new FileLocation(this, source);
            var destinationLocation = new FileLocation(this, destination);
            using (var sourceStream = await sourceLocation.FileProvider.ReadAsync(sourceLocation))
                await destinationLocation.FileProvider.WriteAsync(sourceStream, destinationLocation);
            (await destinationLocation.FileProvider.ExistsAsync(destinationLocation)).ShouldBe(true);
            (await sourceLocation.FileProvider.ExistsAsync(sourceLocation)).ShouldBe(true);
        }, (object?)null);
    }
}

public class DeleteFile : Interaction
{
    public override void Specify()
    {
        Description = "Deletes a file if it exists.";
        InputSpec.AddInputs("inputFile");
        InputSpec.UseStoragePaths();
        InputSpec.AddStorageAbstractionInput();
        InputSpec.AddRetry();
    }

    public override async Task RunAsync()
    {
        await RetryAsync(async (o) =>
        {
            var location = new FileLocation(this, InputState!["inputFile"]!.ToString());
            if (await location.FileProvider.ExistsAsync(location))
            {
                await location.FileProvider.DeleteAsync(location);
                (await location.FileProvider.ExistsAsync(location)).ShouldBe(false);
            }
        }, (object?)null);
    }
}

public class DoesFileExist : Interaction
{
    public override void Specify()
    {
        Description = "Checks if a file exists.";
        InputSpec.AddInputs("inputFile", "outputPath");
        InputSpec.UseStoragePaths();
        InputSpec.AddStorageAbstractionInput();
        InputSpec.AddRetry();
    }

    public override async Task RunAsync()
    {
        await RetryAsync(async (o) =>
        {
            var location = new FileLocation(this, InputState!["inputFile"]!.ToString());
            var exists = await location.FileProvider.ExistsAsync(location);
            AppState.Instance.WritePath(InputState!["outputPath"]!.ToString(), exists.ToString());
        }, (object?)null);
    }
}

public class FileMatch : Interaction
{
    public override void Specify()
    {
        Description = "Compares two files to see if they match.";
        InputSpec.AddInputs("file1", "file2", "separator", "outputPath");
        InputSpec.UseStoragePaths();
        InputSpec.AddStorageAbstractionInput();
        InputSpec.AddRetry();
    }

    public override async Task RunAsync()
    {
        await RetryAsync(async (o) =>
        {
            await TryRunAsync();
        }, (object?)null);
    }

    public async Task TryRunAsync()
    {
        var a = new FileLocation(this, InputState!["file1"]!.ToString());
        var b = new FileLocation(this, InputState["file2"]!.ToString());
        bool match = false;
        var format = Path.GetExtension(a.Path).TrimStart('.').ToLowerInvariant();
        switch (format)
        {
            case "csv":
                {
                    using var streamA = await a.FileProvider.ReadAsync(a);
                    using var streamB = await b.FileProvider.ReadAsync(b);
                    match = CsvReader.ReadFromStream(streamA,
                            new CsvOptions { Separator = (InputState["separator"]?.ToString() ?? ",").First() }
                        )
                        .SelectMany(x => x.Values)
                        .Except(
                        CsvReader.ReadFromStream(streamB,
                            new CsvOptions { Separator = (InputState["separator"]?.ToString() ?? ",").First() }
                        )
                        .SelectMany(x => x.Values))
                        .Count() == 0;
                    break;
                }
            case "xml":
                {
                    var aXml = new XmlDocument();
                    using var streamA = await a.FileProvider.ReadAsync(a);
                    using var streamB = await b.FileProvider.ReadAsync(b);
                    aXml.Load(streamA);
                    var bXml = new XmlDocument();
                    bXml.Load(streamB);
                    match = aXml.ToString()!.Equals(bXml.ToString());
                    break;
                }
            case "json":
                {
                    using var streamA = await a.FileProvider.ReadAsync(a);
                    using var streamB = await b.FileProvider.ReadAsync(b);
                    using var srA = new StreamReader(streamA);
                    using var srB = new StreamReader(streamB);
                    using var jtA = new JsonTextReader(srA);
                    using var jtB = new JsonTextReader(srB);
                    var tokenA = await JToken.ReadFromAsync(jtA);
                    var tokenB = await JToken.ReadFromAsync(jtB);
                    match = tokenA.ToString().Equals(tokenB.ToString());
                    break;
                }
            default:
                {
                    using var aStream = await a.FileProvider.ReadAsync(a);
                    using var bStream = await b.FileProvider.ReadAsync(b);
                    match = await new StreamCompare().AreEqualAsync(aStream, bStream);
                    break;
                }
        }
        AppState.Instance.WritePath(InputState["outputPath"]!.ToString(), match.ToString());
    }
}

public class ListFiles : Interaction
{
    public override void Specify()
    {
        Description = "Lists the files in a directory.";
        InputSpec.AddInputs("inputDir", "pattern", "outputPath");
        InputSpec.UseStoragePaths();
        InputSpec.AddStorageAbstractionInput();
        InputSpec.AddRetry();
    }

    public override async Task RunAsync()
    {
        var inputDir = InputState!["inputDir"]!.ToString();
        var pattern = InputState!["pattern"]?.ToString() ?? "*.*";
        await RetryAsync(async (o) =>
        {
            var location = new FileLocation(this, inputDir);
            var files = (await location.FileProvider.ListAsync(location, pattern)).ToArray();
            AppState.Instance.WritePath(InputState["outputPath"]!.ToString(), JArray.FromObject(files));
            var filesOutput = AppState.Instance.StateObject.SelectToken(InputState["outputPath"]!.ToString())!
                .Select(x => x.ToString());
            files.ForEach(x => filesOutput.Contains(x).ShouldBe(true));
        }, (object?)null);
    }
}


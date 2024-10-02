using Csv;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Dacris.Maestro.Data
{
    public class ReadCsvFile : DataInteraction
    {
        public override void Specify()
        {
            base.Specify();
            InputSpec.AddInputs("inputFile", "inputPath", "schemaPath", "separator", "tableName", "createTable");
            InputSpec.StateObjectKey("createTable")
                .WithSimpleType(ValueTypeSpec.Boolean);
            InputSpec.AddRetry();
        }

        public override async Task RunAsync()
        {
            await RetryAsync(async (x) =>
            {
                await DoReadCsvFile();
            }, (object?)null);
        }

        private async Task DoReadCsvFile()
        {
            using Stream memStream = new MemoryStream();
            using var sw = new StreamWriter(memStream, Encoding.UTF8);
            var inputFile = InputState!["inputFile"]?.ToString();
            var inputPath = InputState!["inputPath"]?.ToString();
            var schemaPath = InputState!["schemaPath"]?.ToString();
            var separator = InputState!["separator"]?.ToString() ?? ",";
            var tableName = InputState!["tableName"]!.ToString();
            var createTable = InputState!["createTable"]?.ToString() ?? "false";
            var schemaDef = schemaPath is null ? null : (JObject?)AppState.Instance.StateObject.SelectToken(schemaPath);
            var sb = new StringBuilder();
            if (inputPath is not null)
            {
                // Turn state objects into CSV text
                var input = (JArray)AppState.Instance.StateObject.SelectToken(inputPath)!;
                using var csvw = new StringWriter(sb);
                CsvWriter.Write(csvw, schemaDef!.Properties().Select(p => p.Name).ToArray(),
                    input.Select(o => ((JObject)o).Properties().Select(p => p.Value.ToString()).ToArray()),
                    separator.First());
                csvw.Close();
            }
            // Read a CSV file or string buffer into memory
            if (sb.Length > 0)
            {
                sw.Write(sb.ToString());
                sw.Flush();
                memStream.Seek(0, SeekOrigin.Begin);
            }
            using var fileStream = sb.Length <= 0 ? File.OpenRead(inputFile!) : null;
            CsvMemoryReader.ReadCsv(separator,
                sb.Length > 0 ? memStream : fileStream!,
                out var headerLine, out var csvLines, out var isEmpty);
            if (isEmpty)
            {
                return;
            }
            if (schemaDef is null)
            {
                schemaDef = CsvMemoryReader.StringSchemaFromHeader(headerLine!.Values)!;
            }
            var session = await EnsureSessionAsync();
            var repo = session.Item1;
            var conn = session.Item2;

            if (bool.Parse(createTable))
            {
                repo.CreateTable(conn, schemaDef, tableName);
            }
            foreach (var line in csvLines)
            {
                repo.BulkLoad(conn, line, schemaDef, tableName);
            }
            repo.FinishBulkLoad(conn, tableName);
            await Task.CompletedTask;
        }
    }
}

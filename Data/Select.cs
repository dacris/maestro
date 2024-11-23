namespace Dacris.Maestro.Data
{
    public class Select : DataInteraction
    {
        public override void Specify()
        {
            base.Specify();
            InputSpec.AddInputs("outputFile", "outputPath", "schemaPath", "separator", "query", "parameters");
            InputSpec.AddRetry();
            Description = "Executes a SQL SELECT query on an in-memory SQL table, producing a JSON array in the state.";
        }

        public override async Task RunAsync()
        {
            var outputFile = InputState!["outputFile"]?.ToString();
            var outputPath = InputState!["outputPath"]?.ToString();
            var schemaPath = InputState!["schemaPath"]?.ToString();
            var separator = InputState!["separator"]?.ToString() ?? ",";
            var query = InputState!["query"]!.ToString();
            var parameters = InputState!["parameters"];
            if (AppState.Instance.IsMock() && AppState.Instance.ReadKey("dataConnections")!
                        .SelectToken(InputState!["connPath"]!.ToString())!["systemType"]!.ToString() == "MockData")
            {
                var inputFile = Path.Combine(
                        AppState.Instance.ReadKey("dataConnections")!
                        .SelectToken(InputState!["connPath"]!.ToString())!
                        ["connString"]?
                        .ToString() ?? Environment.CurrentDirectory, query + ".csv");

                using var stream = File.OpenRead(inputFile!);
                CsvMemoryReader.ReadCsv(separator, stream, out var headerLine, out var csvLines, out var isEmpty);
                if (isEmpty)
                {
                    return;
                }
                var session = await EnsureSessionAsync();
                var repo = session.Item1;
                var conn = session.Item2;
                var schemaDef = CsvMemoryReader.StringSchemaFromHeader(headerLine!.Values);
                repo.CreateTable(conn, schemaDef, "_tmp");
                repo.Modify(conn, "_DeleteMock", null);
                foreach (var line in csvLines)
                {
                    repo.BulkLoad(conn, line, schemaDef, "_tmp");
                }
                repo.FinishBulkLoad(conn, "_tmp");
                repo.Select(conn, "_SelectMock", outputFile, separator.First(), parameters, schemaPath, outputPath);
                return;
            }

            await RetryAsync(async (x) =>
            {
                var session = await EnsureSessionAsync();
                var repo = session.Item1;
                var conn = session.Item2;
                repo.Select(conn, query, outputFile, separator.First(), parameters, schemaPath, outputPath);
                await Task.CompletedTask;
            }, (object?)null);
        }
    }
}

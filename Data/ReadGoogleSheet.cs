using GoogleSheetsWrapper;

namespace Dacris.Maestro.Data
{
    public class ReadGoogleSheet : Interaction
    {
        public override void Specify()
        {
            InputSpec.AddInputs("spreadsheetId", "serviceAccountName", "mainSheetName", "tabName", "jsonCredentialFile");
            InputSpec.AddRetry();
        }

        public override async Task RunAsync()
        {
            await RetryAsync(async (x) =>
            {
                await DoReadSheetAsCsv();
            }, (object?)null);
        }

        private async Task DoReadSheetAsCsv()
        {
            var spreadsheetId = InputState!["spreadsheetId"]!.ToString();
            var serviceAccountName = InputState!["serviceAccountName"]!.ToString();
            var mainSheetName = InputState!["mainSheetName"]!.ToString();
            var tabName = InputState!["tabName"]!.ToString();

            if (AppState.Instance.IsMock())
                return;
   
            var exporter = new SheetExporter(spreadsheetId, serviceAccountName, mainSheetName);
            exporter.Init(File.ReadAllText(InputState!["jsonCredentialFile"]!.ToString()));

            using (var stream = new FileStream("Output.csv", FileMode.Create))
            {
                var range = new SheetRange(tabName, 1, 1);
                exporter.ExportAsCsv(range, stream);
            }
            await Task.CompletedTask;
        }
    }

    public class UploadToGoogleSheet : Interaction
    {
        public override void Specify()
        {
            InputSpec.AddInputs("inputFile", "spreadsheetId", "serviceAccountName", "mainSheetName", "tabName", "jsonCredentialFile");
            InputSpec.AddRetry();
        }

        public override async Task RunAsync()
        {
            await RetryAsync(async (x) =>
            {
                await DoWriteSheetAsCsv();
            }, (object?)null);
        }

        private async Task DoWriteSheetAsCsv()
        {
            var inputFile = InputState!["inputFile"]!.ToString();
            var spreadsheetId = InputState!["spreadsheetId"]!.ToString();
            var serviceAccountName = InputState!["serviceAccountName"]!.ToString();
            var mainSheetName = InputState!["mainSheetName"]!.ToString();
            var tabName = InputState!["tabName"]!.ToString();

            if (AppState.Instance.IsMock())
                return;
   
            var importer = new SheetHelper(spreadsheetId, serviceAccountName, mainSheetName);
            importer.Init(File.ReadAllText(InputState!["jsonCredentialFile"]!.ToString()));

            // Get the total row count for the existing sheet
            var rows = importer.GetRows(new SheetRange(tabName, 1, 1, 1));

            // Delete all of the rows
            importer.DeleteRows(1, rows.Count);

            // Create the SheetAppender class
            var appender = new SheetAppender(importer);

            using (var stream = new FileStream(inputFile, FileMode.Open))
            {
                // Append the csv file to Google sheets, include the header row 
                // and wait 1000 milliseconds between batch updates 
                // Google Sheets API throttles requests per minute so you may need to play
                // with this setting. 
                appender.AppendCsv(
                    stream, // The CSV FileStrem 
                    true, // true indicating to include the header row
                    1000); // 1000 milliseconds to wait every 100 rows that are batch sent to the Google Sheets API
            }
            await Task.CompletedTask;
        }
    }
}

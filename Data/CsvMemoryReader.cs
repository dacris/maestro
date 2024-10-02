using Csv;
using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Data
{
    public static class CsvMemoryReader
    {
        public static void ReadCsv(string separator, Stream stream, out ICsvLineFromMemory? headerLine, out List<ICsvLine> csvLines, out bool isEmpty)
        {
            var innerHeaderLine = (ICsvLineFromMemory?)null;
            var csvFile = CsvReader.ReadFromStream(stream, new CsvOptions
            {
                Separator = separator.First(),
                SkipRow = (line, i) =>
                {
                    if (innerHeaderLine is not null)
                        return false;

                    innerHeaderLine = CsvReader.ReadFromMemory(line, new CsvOptions
                    {
                        Separator = separator.First(),
                        HeaderMode = HeaderMode.HeaderAbsent
                    }).First();
                    return false;
                }
            });
            csvLines = csvFile.ToList();
            headerLine = innerHeaderLine;
            isEmpty = headerLine is null;
        }

        public static JObject? StringSchemaFromHeader(ReadOnlyMemory<char>[] csvLine)
        {
            var schema = new JObject();
            foreach (var header in csvLine)
            {
                schema.Add(header.AsString(), "string");
            }
            return schema;
        }
    }
}

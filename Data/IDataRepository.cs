using Csv;
using Dacris.Maestro.Core;
using Newtonsoft.Json.Linq;
using System.Data.Common;

namespace Dacris.Maestro.Data
{
    public interface IDataRepository
    {
        int Timeout { get; set; }
        DbConnection OpenSession(List<IDisposable> sessionResources, string connString);
        void BulkLoad(DbConnection conn, ICsvLine line, JToken? schemaDef, string tableName);
        void FinishBulkLoad(DbConnection conn, string tableName);
        void CreateTable(DbConnection conn, JToken? schemaDef, string tableName);
        void Select(DbConnection conn, string queryConst, string? outputCsvFile, char separator, JToken? parameters, string? schemaPath, string? outputPath);
        void Modify(DbConnection conn, string queryConst, JToken? parameters);
    }
}

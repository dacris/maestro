using Csv;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Dacris.Maestro.Data
{
    public class MemorySql : BaseDataRepository
    {
        public MemorySql()
        {
            Types = new Dictionary<string, string>()
            {
                { "string", "TEXT" },
                { "number", "NUMERIC" },
                { "integer", "INTEGER" },
                { "date", "TEXT" }
            };
            CreateTableSql = "CREATE TABLE IF NOT EXISTS";
        }

        public override DbConnection OpenSession(List<IDisposable> resources, string connString)
        {
            SqliteConnection conn = new(connString);
            conn.Open();
            CheckConnection(conn);
            resources.Add(conn);
            return conn;
        }

        public override void BulkLoad(DbConnection conn, ICsvLine line, JToken? schemaDef, string tableName)
        {
            CheckConnection(conn);
            tableName = Regex.Match(tableName, "[\\#_a-zA-Z0-9]+").Value;
            var schema = ParseSchema(schemaDef);
            using var insertCommand = conn.CreateCommand();
            insertCommand.CommandTimeout = Timeout;
            var columns = string.Join(',', schema.Select(x => x.Name));
            var parameters = string.Join(',', schema.Select((x, i) => "@p" + i));
            var headers = line.Headers
                .Select((x, i) => (x, i))
                .ToDictionary(k => k.Item1, v => v.Item2);
            insertCommand.CommandText = $@"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
            for (int i = 0; i < schema.Count(); i++)
            {
                var type = schema[i].DataType;
                var name = schema[i].Name;
                var value = line.Values[headers[name]];
                insertCommand.Parameters.Add(new SqliteParameter("@p" + i, GetValueByType(type, value)));
            }
            insertCommand.ExecuteNonQuery();
        }

        protected override DbParameter ConstructParameter(string? name, object? value)
        {
            return new SqliteParameter(name, value);
        }
    }
}

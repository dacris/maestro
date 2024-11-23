using Csv;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Dacris.Maestro.Data
{
    public class MySQL : BaseDataRepository
    {
        // https://www.datacamp.com/tutorial/set-up-and-configure-mysql-in-docker
        public MySQL()
        {
            Types = new Dictionary<string, string>()
            {
                { "string", "NVARCHAR(1600)" },
                { "number", "DECIMAL(26,9)" },
                { "integer", "BIGINT" },
                { "date", "DATETIME" }
            };
            CreateTableSql = @"CREATE TABLE";
        }

        public override void CreateTable(DbConnection conn, JToken? schemaDef, string tableName)
        {
            CheckConnection(conn);
            tableName = Regex.Match(tableName, "[\\#_a-zA-Z0-9]+").Value;
            var properties = ((JObject)schemaDef!).Properties().ToImmutableArray();
            var schema = string.Join("," + Environment.NewLine,
                properties.Select(p => Regex.Match(p.Name, "[a-zA-Z0-9_]+").Value + " " + TranslateType(p.Value.ToString())));
            using var createCommand = conn.CreateCommand();
            createCommand.CommandText = $@"
{CreateTableSql} {tableName} (
    {schema}
);
";
            createCommand.Parameters.Add(ConstructParameter("tableName", tableName));
            createCommand.CommandTimeout = Timeout;
            try
            {
                createCommand.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            when (ex.Message.ToLowerInvariant().Contains("already exists"))
            {
                //swallow
            }
        }


        public override DbConnection OpenSession(List<IDisposable> resources, string connString)
        {
            var conn = new MySqlConnection(connString);
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
            insertCommand.CommandText = $@"INSERT INTO {tableName} ({columns}) VALUES ({parameters});";
            for (int i = 0; i < schema.Count(); i++)
            {
                var type = schema[i].DataType;
                var name = schema[i].Name;
                var value = line.Values[headers[name]];
                insertCommand.Parameters.Add(new MySqlParameter("@p" + i, GetValueByType(type, value)));
            }
            insertCommand.ExecuteNonQuery();
        }

        protected override DbParameter ConstructParameter(string? name, object? value)
        {
            return new MySqlParameter(name, value);
        }
    }
}

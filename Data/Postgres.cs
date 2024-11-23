using Csv;
using Npgsql;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Dacris.Maestro.Data
{
    public class Postgres : BaseDataRepository
    {
        // https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/
        public Postgres()
        {
            Types = new Dictionary<string, string>()
            {
                { "string", "varchar(1600)" },
                { "number", "decimal(18,9)" },
                { "integer", "bigint" },
                { "date", "timestamp" }
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
            catch (NpgsqlException ex)
            when (ex.Message.ToLowerInvariant().Contains("already exists"))
            {
                //swallow
            }
        }


        public override DbConnection OpenSession(List<IDisposable> resources, string connString)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
            var dataSource = dataSourceBuilder.Build();
            var conn = dataSource.OpenConnection();
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
                insertCommand.Parameters.Add(new NpgsqlParameter("@p" + i, GetValueByType(type, value)));
            }
            insertCommand.ExecuteNonQuery();
        }

        protected override DbParameter ConstructParameter(string? name, object? value)
        {
            return new NpgsqlParameter(name, value);
        }
    }
}

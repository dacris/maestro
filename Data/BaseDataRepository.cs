using Csv;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Dacris.Maestro.Data
{
    public abstract class BaseDataRepository : IDataRepository
    {
        public int Timeout { get; set; }
        protected Dictionary<string, string>? Types { get; set; }
        protected string? CreateTableSql { get; init; }
        protected abstract DbParameter ConstructParameter(string? name, object? value);
        public abstract void BulkLoad(DbConnection conn, ICsvLine line, JToken? schemaDef, string tableName);
        public abstract DbConnection OpenSession(List<IDisposable> sessionResources, string connString);

        protected virtual void CheckConnection(DbConnection conn)
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Broken)
            {
                throw new Exception("Data connection was closed or broken.");
            }
        }

        protected (string Name, string DataType)[] ParseSchema(JToken? schemaDef)
        {
            if (schemaDef is null)
            {
                return Array.Empty<(string, string)>();
            }
            var properties = ((JObject)schemaDef!).Properties().ToImmutableArray();
            var schema = properties.Select(p => (
                Regex.Match(p.Name, "[a-zA-Z0-9_]+").Value,
                TranslateType(p.Value.ToString())
            )).ToArray();
            return schema;
        }

        protected (string Name, string Value)[] ParseValues(JToken? valueDef)
        {
            if (valueDef is null)
            {
                return Array.Empty<(string, string)>();
            }
            var properties = ((JObject)valueDef!).Properties().ToImmutableArray();
            var values = properties.Select(p => (
                Regex.Match(p.Name, "[a-zA-Z0-9_]+").Value,
                p.Value.ToString()
            )).ToArray();
            return values;
        }

        protected virtual void OutputSchemaToState(ReadOnlyCollection<DbColumn> schema, string? schemaPath)
        {
            if (schemaPath is null)
            {
                return;
            }
            JObject schemaJson = new JObject();
            foreach (var column in schema)
            {
                if (typeof(long).IsAssignableFrom(column.DataType ?? typeof(string)))
                {
                    schemaJson.Add(column.ColumnName, "integer");
                }
                else if (typeof(decimal).IsAssignableFrom(column.DataType ?? typeof(string)))
                {
                    schemaJson.Add(column.ColumnName, "number");
                }
                else if (typeof(DateTime).IsAssignableFrom(column.DataType ?? typeof(string)))
                {
                    schemaJson.Add(column.ColumnName, "date");
                }
                else
                {
                    schemaJson.Add(column.ColumnName, "string");
                }
            }
            AppState.Instance.WritePath(schemaPath, schemaJson);
        }

        protected virtual IEnumerable<string[]> GetRows(DbDataReader reader)
        {
            while (reader.Read())
            {
                List<string> fields = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    fields.Add(reader[i]?.ToString() ?? string.Empty);
                }
                yield return fields.ToArray();
            }
        }

        protected virtual void AddParameters(JToken? parameters, DbCommand command)
        {
            if (parameters is null)
            {
                return;
            }
            var schema = ParseSchema(parameters["schema"]);
            var values = ParseValues(parameters["data"]);
            command.Parameters.AddRange(schema.Select(p =>
            {
                return ConstructParameter(p.Name, GetValueByType(p.DataType, values.Single(v => v.Name.Equals(p.Name)).Value));
            }).ToArray());
        }

        protected virtual string TranslateType(string inputType)
        {
            Dictionary<string, string> map = Types!;
            return map[inputType];
        }

        protected virtual object? GetValueByType(string type, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            var matchedSourceType = Types!
                .FirstOrDefault(v => v.Value.ToUpperInvariant().Equals(type.ToUpperInvariant()))
                .Key;
            switch (matchedSourceType.ToLowerInvariant())
            {
                case "integer":
                    return long.Parse(value);
                case "number":
                    return decimal.Parse(value);
                case "date":
                    return DateTime.Parse(value);
                default:
                    return value;
            }
        }

        protected virtual Type GetColumnType(string jsonType)
        {
            switch (jsonType.ToLowerInvariant())
            {
                case "integer":
                    return typeof(long?);
                case "number":
                    return typeof(decimal?);
                case "date":
                    return typeof(DateTime?);
                default:
                    return typeof(string);
            }
        }

        public virtual void FinishBulkLoad(DbConnection conn, string tableName)
        {
            //no-op here
        }

        public virtual void CreateTable(DbConnection conn, JToken? schemaDef, string tableName)
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
)
";
            createCommand.Parameters.Add(ConstructParameter("tableName", tableName));
            createCommand.CommandTimeout = Timeout;
            createCommand.ExecuteNonQuery();
        }

        public virtual void Select(DbConnection conn, string queryConst, string? outputCsvFile, char separator, JToken? parameters, string? schemaPath, string? outputPath)
        {
            CheckConnection(conn);
            using var selectCommand = conn.CreateCommand();
            selectCommand.CommandText = AppState.Instance.Constants[queryConst];
            AddParameters(parameters, selectCommand);
            selectCommand.CommandTimeout = Timeout;
            using (var reader = selectCommand.ExecuteReader())
            {
                var schema = reader.GetColumnSchema();
                var headers = schema.Select(x => x.ColumnName).ToArray();
                if (!string.IsNullOrEmpty(outputPath))
                {
                    var array = new JArray();
                    foreach (var line in GetRows(reader))
                    {
                        var obj = new JObject();
                        var i = 0;
                        foreach (var header in headers)
                        {
                            obj.Add(header, line[i]);
                            i++;
                        }
                        array.Add(obj);
                    }
                    AppState.Instance.WritePath(outputPath, array);
                }
                else
                {
                    using var writer = new StreamWriter(outputCsvFile!, false);
                    CsvWriter.Write(writer, headers, GetRows(reader), separator);
                }
                OutputSchemaToState(schema, schemaPath);
            }
        }

        public virtual void Modify(DbConnection conn, string queryConst, JToken? parameters)
        {
            CheckConnection(conn);
            using var modifyCommand = conn.CreateCommand();
            modifyCommand.CommandText = AppState.Instance.Constants[queryConst];
            AddParameters(parameters, modifyCommand);
            modifyCommand.CommandTimeout = Timeout;
            modifyCommand.ExecuteNonQuery();
        }
    }
}

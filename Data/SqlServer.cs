using Csv;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;

namespace Dacris.Maestro.Data
{
    public class SqlServer : BaseDataRepository
    {
        private const int BatchSize = 50000;
        private DataTable _buffer = new();
        public SqlServer()
        {
            Types = new Dictionary<string, string>()
            {
                { "string", "nvarchar(1600)" },
                { "number", "decimal(26,15)" },
                { "integer", "bigint" },
                { "date", "datetime" }
            };
            CreateTableSql = "IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName)"
                + Environment.NewLine +
                "CREATE TABLE";
        }

        public override void BulkLoad(DbConnection conn, ICsvLine line, JToken? schemaDef, string tableName)
        {
            if (_buffer.Columns.Count == 0)
            {
                var schema = ParseSchema(schemaDef);
                foreach (var column in schema)
                {
                    _buffer.Columns.Add(column.Name, GetColumnType(column.DataType));
                }
            }
            _buffer.Rows.Add(line.Values);
            if (_buffer.Rows.Count >= BatchSize)
            {
                FlushBuffer(conn, tableName);
            }
        }

        private void FlushBuffer(DbConnection conn, string tableName)
        {
            CheckConnection(conn);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy((SqlConnection)conn);
            sqlBulkCopy.BatchSize = BatchSize / 10;
            sqlBulkCopy.BulkCopyTimeout = 0;
            sqlBulkCopy.DestinationTableName = tableName;
            sqlBulkCopy.EnableStreaming = false;
            sqlBulkCopy.WriteToServer(_buffer);
            _buffer.Rows.Clear();
        }

        public override void FinishBulkLoad(DbConnection conn, string tableName)
        {
            if(_buffer.Rows.Count > 0)
            {
                FlushBuffer(conn, tableName);
            }
            _buffer.Dispose();
            _buffer = new DataTable();
        }

        public override DbConnection OpenSession(List<IDisposable> sessionResources, string connString)
        {
            //sample: Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=DacrisDb;Integrated Security=True
            var conn = new SqlConnection(connString);
            conn.Open();
            CheckConnection(conn);
            sessionResources.Add(conn);
            return conn;
        }

        protected override DbParameter ConstructParameter(string? name, object? value)
        {
            return new SqlParameter(name, value);
        }
    }
}

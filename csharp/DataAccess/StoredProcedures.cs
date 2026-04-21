// ADDS Stored Procedure Wrappers
// ODP.NET managed Oracle.ManagedDataAccess.Core (.NET 10)

using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace ADDS.DataAccess
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand(procName, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            for (int i = 0; i < args.Length; i++)
            {
                cmd.Parameters.Add(new OracleParameter($"p{i}", args[i]));
            }

            var outParam = new OracleParameter("result", OracleDbType.Varchar2, 4000);
            outParam.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(outParam);

            cmd.ExecuteNonQuery();
            return outParam.Value;
        }

        public static DataTable RunQuery(string sql)
        {
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        public static void GenerateReport(string reportType, string outputPath)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "SELECT * FROM REPORT_TEMPLATES WHERE REPORT_TYPE=:reportType";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("reportType", reportType));
            using var reader = cmd.ExecuteReader();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                while (reader.Read())
                {
            var lines = System.IO.File.ReadAllLines(filePath);
            var conn = OracleConnectionFactory.GetConnection();

            foreach (var line in lines)
            {
                // NOTE: tableName must be validated by the caller against an allowlist.
                // Line values are passed as a single bind variable; callers should
                // ensure the file format matches the target table's single-column contract,
                // or this method should be replaced with OracleBulkCopy for multi-column loads.
                var sql = $"INSERT INTO {tableName} VALUES (:lineVal)";
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(new OracleParameter("lineVal", line));
                try { cmd.ExecuteNonQuery(); }
                catch (OracleException ex) { System.Diagnostics.Trace.TraceError($"BulkLoad row error: {ex.Message}"); }
            }
        }


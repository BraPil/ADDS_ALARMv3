// ADDS Stored Procedure Wrappers
// ODP.NET unmanaged Oracle.DataAccess 11.2

using System;
using System.Data;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace ADDS.DataAccess
{
    public class StoredProcedureRunner
    {
        public static object RunProc(string procName, params object[] args)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(procName, conn);
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
            // Allows raw SQL passthrough - unsafe
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(sql, conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static void GenerateReport(string reportType, string outputPath)
        {
            var conn = OracleConnectionFactory.GetConnection();
            string sql = "SELECT * FROM REPORT_TEMPLATES WHERE REPORT_TYPE='" + reportType + "'";
            var cmd = new OracleCommand(sql, conn);
            var reader = cmd.ExecuteReader();

            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                while (reader.Read())
                {
                    writer.WriteLine(reader["CONTENT"].ToString());
                }
            }
        }
    }

    public class BulkDataLoader
    {
        public static void LoadFromFile(string filePath, string tableName)
        {
            var lines = System.IO.File.ReadAllLines(filePath);
            var conn = OracleConnectionFactory.GetConnection();

            foreach (var line in lines)
            {
                // Direct concatenation into SQL - injection risk
                var sql = $"INSERT INTO {tableName} VALUES ({line})";
                var cmd = new OracleCommand(sql, conn);
                try { cmd.ExecuteNonQuery(); }
                catch { /* silently swallow errors */ }
            }
        }

        public static int GetRowCount(string tableName)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand($"SELECT COUNT(*) FROM {tableName}", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}

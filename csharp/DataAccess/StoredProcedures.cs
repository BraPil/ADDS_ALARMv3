// ADDS Stored Procedure Wrappers
// ODP.NET unmanaged Oracle.DataAccess 11.2

using System;
using System.Data;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace ADDS.DataAccess
{
    // ---------------------------------------------------------------------------
    // Concrete Oracle implementation of IStoredProcedureRunner
    // ---------------------------------------------------------------------------

    public class OracleStoredProcedureRunner : IStoredProcedureRunner
    {
        // Static singleton so call-sites that use the old static class continue
        // to compile without modification.
        public static readonly OracleStoredProcedureRunner Default =
            new OracleStoredProcedureRunner();

        public object RunProc(string procName, params object[] args)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(procName, conn);
            return outParam.Value;
        }

        public DataTable RunQuery(string sql)
        {
            // Allows raw SQL passthrough - unsafe
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(sql, conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }

    // ---------------------------------------------------------------------------
    // Keep old static façade so existing call-sites compile unchanged
    // ---------------------------------------------------------------------------

    [Obsolete("Use OracleStoredProcedureRunner / IStoredProcedureRunner instead.")]
    public static class StoredProcedureRunner
    {
        public static object RunProc(string procName, params object[] args)
            => OracleStoredProcedureRunner.Default.RunProc(procName, args);

        public static DataTable RunQuery(string sql)
            => OracleStoredProcedureRunner.Default.RunQuery(sql);

        public static void GenerateReport(string reportType, string outputPath)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(
                "SELECT * FROM REPORT_TEMPLATES WHERE REPORT_TYPE=:reportType", conn);
            cmd.Parameters.Add(new OracleParameter("reportType", reportType));
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

    // ---------------------------------------------------------------------------
    // BulkDataLoader – extracted to its own class (was mixed into StoredProcedures)
    // ---------------------------------------------------------------------------

    public class BulkDataLoader
    {
        public static void LoadFromFile(string filePath, string tableName)

// ADDS Stored Procedure Wrappers
// ODP.NET Managed Core (Oracle.ManagedDataAccess)

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace ADDS.DataAccess
{
    public class StoredProcedureRunner
    {
        public static async Task<object> RunProcAsync(string procName,
            CancellationToken ct = default, params object[] args)
        {
            await OracleConnectionFactory.GetConnectionAsync(ct).ConfigureAwait(false);
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

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            return outParam.Value;
        }

        public static async Task<DataTable> RunQueryAsync(string sql,
            CancellationToken ct = default)
        {
            // Allows raw SQL passthrough - unsafe
            await OracleConnectionFactory.GetConnectionAsync(ct).ConfigureAwait(false);
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            var dt = new DataTable();
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            dt.Load(reader);
            return dt;
        }

        public static async Task GenerateReportAsync(string reportType, string outputPath,
            CancellationToken ct = default)
        {
            await OracleConnectionFactory.GetConnectionAsync(ct).ConfigureAwait(false);
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT * FROM REPORT_TEMPLATES WHERE REPORT_TYPE = :reportType", conn);
            cmd.Parameters.Add(new OracleParameter("reportType", OracleDbType.Varchar2)
                { Value = reportType });
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    await writer.WriteLineAsync(reader["CONTENT"].ToString())
                        .ConfigureAwait(false);
                }
            }
        }

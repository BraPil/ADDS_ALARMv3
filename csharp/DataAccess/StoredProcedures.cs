// ADDS Stored Procedure Wrappers
// Modernized: ODP.NET managed 19c, array binding for bulk ops, no raw SQL passthrough, async/await

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace ADDS.DataAccess
{
    public class StoredProcedureRunner
    {
        private readonly ILogger<StoredProcedureRunner> _logger;

        public StoredProcedureRunner(ILogger<StoredProcedureRunner> logger)
        {
            _logger = logger;
        }

        public async Task<string> RunProcAsync(string procName, params OracleParameter[] parameters)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(procName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            var outParam = new OracleParameter("result", OracleDbType.Varchar2, 4000)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("RunProc: {Proc}", procName);
            return outParam.Value?.ToString();
        }

        public async Task GenerateReportAsync(string reportType, string outputPath)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "SELECT CONTENT FROM REPORT_TEMPLATES WHERE REPORT_TYPE=:rtype", conn);
            cmd.Parameters.Add(new OracleParameter("rtype", reportType));
            using var reader = await cmd.ExecuteReaderAsync();
            using var writer = new System.IO.StreamWriter(outputPath);
            while (await reader.ReadAsync())
                await writer.WriteLineAsync(reader["CONTENT"].ToString());
            _logger.LogInformation("GenerateReport: {Type} -> {Path}", reportType, outputPath);
        }
    }

    public class BulkDataLoader
    {
        private readonly ILogger<BulkDataLoader> _logger;

        public BulkDataLoader(ILogger<BulkDataLoader> logger)
        {
            _logger = logger;
        }

        // ODP.NET array binding — one round-trip for all rows instead of N sequential inserts
        public async Task LoadFromFileAsync(string filePath, string tableName)
        {
            if (!IsAllowedTable(tableName))
                throw new ArgumentException($"Table '{tableName}' is not in the allowed import list.");

            var lines = await System.IO.File.ReadAllLinesAsync(filePath);
            var values = new List<string>(lines.Length);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    values.Add(line.Trim());
            }

            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand($"INSERT INTO {tableName} VALUES (:val)", conn);
            cmd.ArrayBindCount = values.Count;
            cmd.Parameters.Add(new OracleParameter("val", values.ToArray()));
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("BulkLoad: {Count} rows into {Table}", values.Count, tableName);
        }

        public async Task<int> GetRowCountAsync(string tableName)
        {
            if (!IsAllowedTable(tableName))
                throw new ArgumentException($"Table '{tableName}' is not in the allowed import list.");

            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand($"SELECT COUNT(*) FROM {tableName}", conn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private static readonly HashSet<string> _allowedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "EQUIPMENT", "INSTRUMENTS", "PIPE_ROUTES", "VESSELS", "HEAT_EXCHANGERS"
        };

        private static bool IsAllowedTable(string tableName) => _allowedTables.Contains(tableName);
    }
}

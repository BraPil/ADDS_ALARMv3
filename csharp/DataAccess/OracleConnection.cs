// ADDS Oracle Data Access Layer
// Modernized: ODP.NET managed 19c, credentials externalized, SQL parameterized, async/await

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;   // ODP.NET managed 19c

namespace ADDS.DataAccess
{
    public class OracleConnectionFactory
    {
        private static string _connectionString;

        public static void Configure(IConfiguration config)
        {
            // Credentials loaded from environment / appsettings — never hardcoded
            var host = config["Oracle:Host"] ?? Environment.GetEnvironmentVariable("ADDS_ORACLE_HOST");
            var port = config["Oracle:Port"] ?? Environment.GetEnvironmentVariable("ADDS_ORACLE_PORT") ?? "1521";
            var sid  = config["Oracle:Sid"]  ?? Environment.GetEnvironmentVariable("ADDS_ORACLE_SID");
            var user = config["Oracle:User"] ?? Environment.GetEnvironmentVariable("ADDS_ORACLE_USER");
            var pass = config["Oracle:Password"] ?? Environment.GetEnvironmentVariable("ADDS_ORACLE_PASS");

            _connectionString =
                $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))" +
                $"(CONNECT_DATA=(SID={sid})));User Id={user};Password={pass};";
        }

        public static OracleConnection GetConnection()
        {
            if (_connectionString == null)
                throw new InvalidOperationException("OracleConnectionFactory.Configure() must be called at startup.");
            var conn = new OracleConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public static async Task<OracleConnection> GetConnectionAsync()
        {
            if (_connectionString == null)
                throw new InvalidOperationException("OracleConnectionFactory.Configure() must be called at startup.");
            var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }
    }

    public class EquipmentRepository
    {
        private readonly ILogger<EquipmentRepository> _logger;

        public EquipmentRepository(ILogger<EquipmentRepository> logger)
        {
            _logger = logger;
        }

        public async Task<DataTable> GetAllEquipmentAsync()
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand("SELECT * FROM EQUIPMENT ORDER BY TAG", conn);
            using var adapter = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            await Task.Run(() => adapter.Fill(dt));
            _logger.LogInformation("GetAllEquipment returned {Count} rows", dt.Rows.Count);
            return dt;
        }

        public async Task SaveEquipmentAsync(string tag, string type, string model)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) VALUES(:tag,:type,:model,SYSDATE)", conn);
            cmd.Parameters.Add(new OracleParameter("tag",   tag));
            cmd.Parameters.Add(new OracleParameter("type",  type));
            cmd.Parameters.Add(new OracleParameter("model", model));
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("SaveEquipment: inserted {Tag}", tag);
        }

        public async Task DeleteEquipmentAsync(string tag)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand("DELETE FROM EQUIPMENT WHERE TAG=:tag", conn);
            cmd.Parameters.Add(new OracleParameter("tag", tag));
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("DeleteEquipment: removed {Tag}", tag);
        }

        public async Task<DataTable> GetPipeRoutesAsync()
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand("SELECT * FROM PIPE_ROUTES", conn);
            using var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            await Task.Run(() => da.Fill(dt));
            return dt;
        }
    }

    public class InstrumentRepository
    {
        private readonly ILogger<InstrumentRepository> _logger;

        public InstrumentRepository(ILogger<InstrumentRepository> logger)
        {
            _logger = logger;
        }

        public async Task<DataTable> GetInstrumentsByAreaAsync(string area)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "SELECT * FROM INSTRUMENTS WHERE AREA=:area", conn);
            cmd.Parameters.Add(new OracleParameter("area", area));
            using var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            await Task.Run(() => da.Fill(dt));
            return dt;
        }

        public async Task UpdateInstrumentAsync(string tag, string value)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "UPDATE INSTRUMENTS SET LAST_VALUE=:val, UPDATED=SYSDATE WHERE TAG=:tag", conn);
            cmd.Parameters.Add(new OracleParameter("val", value));
            cmd.Parameters.Add(new OracleParameter("tag", tag));
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("UpdateInstrument: tag={Tag}", tag);
        }
    }
}

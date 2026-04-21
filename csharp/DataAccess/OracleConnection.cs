// ADDS Oracle Data Access Layer
// .NET Framework 4.5+ / Oracle.ManagedDataAccess (ODP.NET Managed Core)

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace ADDS.DataAccess
{
        private static OracleConnection _sharedConnection;

        public static OracleConnection GetConnection()
        {
            if (_sharedConnection == null || _sharedConnection.State == ConnectionState.Closed)
            {
                string connStr = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                 $"(HOST={HOST})(PORT={PORT}))(CONNECT_DATA=(SID={SID})));" +
                                 $"User Id={USER};Password={PASS};";
                _sharedConnection = new OracleConnection(connStr);
                _sharedConnection.Open();
            }
            return _sharedConnection;
        }

        public static async Task GetConnectionAsync(CancellationToken ct = default)
        {
            if (_sharedConnection == null || _sharedConnection.State == ConnectionState.Closed)
            {
                string connStr = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                 $"(HOST={HOST})(PORT={PORT}))(CONNECT_DATA=(SID={SID})));" +
                                 $"User Id={USER};Password={PASS};";
                _sharedConnection = new OracleConnection(connStr);
                await _sharedConnection.OpenAsync(ct).ConfigureAwait(false);
            }
        }

        public static async Task CloseConnectionAsync()
        {
            if (_sharedConnection != null && _sharedConnection.State == ConnectionState.Open)
            {
                await Task.Run(() => _sharedConnection.Close()).ConfigureAwait(false);
                _sharedConnection.Dispose();
                _sharedConnection = null;
            }
    public class EquipmentRepository
    {
        public async Task<DataTable> GetAllEquipmentAsync(CancellationToken ct = default)
        {
            await OracleConnectionFactory.GetConnectionAsync(ct).ConfigureAwait(false);
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand("SELECT * FROM EQUIPMENT ORDER BY TAG", conn);
            var dt = new DataTable();
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            dt.Load(reader);
            return dt;
        }

        {
            var conn = OracleConnectionFactory.GetConnection();
            // Raw string concatenation - SQL injection risk
            var sql = $"INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) VALUES('{tag}','{type}','{model}',SYSDATE)";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public async Task DeleteEquipmentAsync(string tag, CancellationToken ct = default)
        {
            await OracleConnectionFactory.GetConnectionAsync(ct).ConfigureAwait(false);
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand(
                "DELETE FROM EQUIPMENT WHERE TAG = :tag", conn);
            cmd.Parameters.Add(new OracleParameter("tag", OracleDbType.Varchar2) { Value = tag });
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        public async Task<DataTable> GetPipeRoutesAsync(CancellationToken ct = default)
        {
            await OracleConnectionFactory.GetConnectionAsync(ct).ConfigureAwait(false);
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand("SELECT * FROM PIPE_ROUTES", conn);
            var dt = new DataTable();
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            dt.Load(reader);
            return dt;
        }
    }
        {
            var conn = OracleConnectionFactory.GetConnection();
            // Injection vulnerability
            string sql = "SELECT * FROM INSTRUMENTS WHERE AREA='" + area + "'";
            var cmd = new OracleCommand(sql, conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();

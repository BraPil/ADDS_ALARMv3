// ADDS Oracle Data Access Layer
// .NET Framework 4.5 / Oracle.DataAccess 11.2 (ODP.NET unmanaged)
// TODO: still using OCI8 for some legacy stored proc calls

using System;
using System.Data;
using Oracle.DataAccess.Client;   // ODP.NET unmanaged - deprecated
using Oracle.DataAccess.Types;
using Microsoft.Extensions.Logging;

namespace ADDS.DataAccess
{
        private const string PASS = "adds_p@ss_2003!";  // plaintext

        private static OracleConnection _sharedConnection;
        private static readonly ILogger _logger =
            LoggerFactory.GetLogger<OracleConnectionFactory>();

        public static OracleConnection GetConnection()
        {
            if (_sharedConnection == null || _sharedConnection.State == ConnectionState.Closed)
            {
                _logger.LogInformation(
                    "OracleConnectionFactory.GetConnection: Opening new connection to {Host}:{Port}/{Sid} as {User}.",
                    HOST, PORT, SID, USER);
                string connStr = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                 $"(HOST={HOST})(PORT={PORT}))(CONNECT_DATA=(SID={SID})));" +
                                 $"User Id={USER};Password={PASS};";
                _sharedConnection = new OracleConnection(connStr);
                try
                {
                    _sharedConnection.Open();
                    _logger.LogInformation(
                        "OracleConnectionFactory.GetConnection: Connection opened successfully (State={State}).",
                        _sharedConnection.State);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "OracleConnectionFactory.GetConnection: Failed to open Oracle connection to {Host}:{Port}/{Sid}. Error: {Message}",
                        HOST, PORT, SID, ex.Message);
                    throw;
                }
            }
            else
            {
                _logger.LogDebug("OracleConnectionFactory.GetConnection: Reusing existing connection (State={State}).", _sharedConnection.State);
            }
            return _sharedConnection;
        }

        public static void CloseConnection()
        {
            if (_sharedConnection != null && _sharedConnection.State == ConnectionState.Open)
            {
                _logger.LogInformation("OracleConnectionFactory.CloseConnection: Closing shared Oracle connection.");
                _sharedConnection.Close();
                _sharedConnection.Dispose();
                _sharedConnection = null;
                _logger.LogInformation("OracleConnectionFactory.CloseConnection: Shared Oracle connection closed and disposed.");
            }
        }
    }
    public class EquipmentRepository
    {
        private static readonly ILogger _logger =
            LoggerFactory.GetLogger<EquipmentRepository>();

        public DataTable GetAllEquipment()
        {
            _logger.LogDebug("EquipmentRepository.GetAllEquipment: Querying all equipment records.");
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand("SELECT * FROM EQUIPMENT ORDER BY TAG", conn);
            var adapter = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            _logger.LogInformation("EquipmentRepository.GetAllEquipment: Retrieved {RowCount} equipment records.", dt.Rows.Count);
            return dt;
        }

        public void SaveEquipment(string tag, string type, string model)
        {
            _logger.LogInformation("EquipmentRepository.SaveEquipment: Inserting equipment TAG={Tag}, TYPE={Type}, MODEL={Model}.", tag, type, model);
            var conn = OracleConnectionFactory.GetConnection();
            // Raw string concatenation - SQL injection risk
            var sql = $"INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) VALUES('{tag}','{type}','{model}',SYSDATE)";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
            _logger.LogInformation("EquipmentRepository.SaveEquipment: Equipment TAG={Tag} inserted successfully.", tag);
        }

        public void DeleteEquipment(string tag)
        {
            _logger.LogInformation("EquipmentRepository.DeleteEquipment: Deleting equipment TAG={Tag}.", tag);
            var conn = OracleConnectionFactory.GetConnection();
            var sql = $"DELETE FROM EQUIPMENT WHERE TAG='{tag}'";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
            _logger.LogInformation("EquipmentRepository.DeleteEquipment: Equipment TAG={Tag} deleted.", tag);
        }

        public DataTable GetPipeRoutes()
        {
            _logger.LogDebug("EquipmentRepository.GetPipeRoutes: Querying pipe routes.");
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand("SELECT * FROM PIPE_ROUTES", conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            _logger.LogInformation("EquipmentRepository.GetPipeRoutes: Retrieved {RowCount} pipe route records.", dt.Rows.Count);
            return dt;
        }
    }

    public class InstrumentRepository
    {
        private static readonly ILogger _logger =
            LoggerFactory.GetLogger<InstrumentRepository>();

        public DataTable GetInstrumentsByArea(string area)
        {
            _logger.LogDebug("InstrumentRepository.GetInstrumentsByArea: Querying instruments for AREA={Area}.", area);
            var conn = OracleConnectionFactory.GetConnection();
            // Injection vulnerability
            string sql = "SELECT * FROM INSTRUMENTS WHERE AREA='" + area + "'";
            var cmd = new OracleCommand(sql, conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            _logger.LogInformation("InstrumentRepository.GetInstrumentsByArea: Retrieved {RowCount} instruments for AREA={Area}.", dt.Rows.Count, area);
            return dt;
        }

        public void UpdateInstrument(string tag, string value)
        {
            _logger.LogInformation("InstrumentRepository.UpdateInstrument: Updating TAG={Tag} with new value.", tag);
            var conn = OracleConnectionFactory.GetConnection();
            string sql = "UPDATE INSTRUMENTS SET LAST_VALUE='" + value +
                         "', UPDATED=SYSDATE WHERE TAG='" + tag + "'";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
            _logger.LogInformation("InstrumentRepository.UpdateInstrument: TAG={Tag} updated successfully.", tag);
        }
    }
}

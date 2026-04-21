// ADDS Oracle Data Access Layer
// .NET Framework 4.5 / Oracle.DataAccess 11.2 (ODP.NET unmanaged)
// TODO: still using OCI8 for some legacy stored proc calls

using System;
using System.Data;
using Oracle.DataAccess.Client;   // ODP.NET unmanaged - deprecated
using Oracle.DataAccess.Types;

namespace ADDS.DataAccess
{
    public class OracleConnectionFactory
    {
        // Hardcoded credentials - legacy pattern from 2004
        private const string HOST = "ORACLE11G-PROD";
        private const int PORT = 1521;
        private const string SID = "ADDSDB";
        private const string USER = "adds_user";
        private const string PASS = "adds_p@ss_2003!";  // plaintext

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

        public static void CloseConnection()
        {
            if (_sharedConnection != null && _sharedConnection.State == ConnectionState.Open)
            {
                _sharedConnection.Close();
                _sharedConnection.Dispose();
                _sharedConnection = null;
            }
        }
    }

    public class EquipmentRepository
    {
        public DataTable GetAllEquipment()
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand("SELECT * FROM EQUIPMENT ORDER BY TAG", conn);
            var adapter = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public void SaveEquipment(string tag, string type, string model)
        {
            var conn = OracleConnectionFactory.GetConnection();
            // Raw string concatenation - SQL injection risk
            var sql = $"INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) VALUES('{tag}','{type}','{model}',SYSDATE)";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public void DeleteEquipment(string tag)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var sql = $"DELETE FROM EQUIPMENT WHERE TAG='{tag}'";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public DataTable GetPipeRoutes()
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand("SELECT * FROM PIPE_ROUTES", conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }

    public class InstrumentRepository
    {
        public DataTable GetInstrumentsByArea(string area)
        {
            var conn = OracleConnectionFactory.GetConnection();
            // Injection vulnerability
            string sql = "SELECT * FROM INSTRUMENTS WHERE AREA='" + area + "'";
            var cmd = new OracleCommand(sql, conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public void UpdateInstrument(string tag, string value)
        {
            var conn = OracleConnectionFactory.GetConnection();
            string sql = "UPDATE INSTRUMENTS SET LAST_VALUE='" + value +
                         "', UPDATED=SYSDATE WHERE TAG='" + tag + "'";
            var cmd = new OracleCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}

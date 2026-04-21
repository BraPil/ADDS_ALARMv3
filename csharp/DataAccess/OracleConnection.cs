// ADDS Oracle Data Access Layer
// .NET 10 / Oracle.ManagedDataAccess.Core (ODP.NET managed)

using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;   // ODP.NET managed - Oracle.ManagedDataAccess.Core NuGet
using Oracle.ManagedDataAccess.Types;

namespace ADDS.DataAccess
        // Hardcoded credentials - legacy pattern from 2004
        private const string HOST = "ORACLE11G-PROD";
        private const int PORT = 1521;
        private const string SID = "ADDSDB";
        private const string USER = "adds_user";
        private const string PASS = "adds_p@ss_2003!";  // plaintext

        // Thread-safe lazy singleton using lock
        private static OracleConnection _sharedConnection;
        private static readonly object _lock = new object();

        public static OracleConnection GetConnection()
        {
            lock (_lock)
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
        }

        public static void CloseConnection()
        {
            lock (_lock)
            {
                if (_sharedConnection != null && _sharedConnection.State == ConnectionState.Open)
                {
                    _sharedConnection.Close();
                    _sharedConnection.Dispose();
                    _sharedConnection = null;
                }
            }
        }
    }
        public void SaveEquipment(string tag, string type, string model)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) VALUES(:tag,:type,:model,SYSDATE)";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("tag", tag));
            cmd.Parameters.Add(new OracleParameter("type", type));
            cmd.Parameters.Add(new OracleParameter("model", model));
            cmd.ExecuteNonQuery();
        }

        public void DeleteEquipment(string tag)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "DELETE FROM EQUIPMENT WHERE TAG=:tag";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("tag", tag));
            cmd.ExecuteNonQuery();
        }

        public DataTable GetPipeRoutes()
        {
            var conn = OracleConnectionFactory.GetConnection();
            using var cmd = new OracleCommand("SELECT * FROM PIPE_ROUTES", conn);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        public DataTable GetInstrumentsByArea(string area)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "SELECT * FROM INSTRUMENTS WHERE AREA=:area";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("area", area));
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public void UpdateInstrument(string tag, string value)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "UPDATE INSTRUMENTS SET LAST_VALUE=:value, UPDATED=SYSDATE WHERE TAG=:tag";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("value", value));
            cmd.Parameters.Add(new OracleParameter("tag", tag));
            cmd.ExecuteNonQuery();
        }
    }
}

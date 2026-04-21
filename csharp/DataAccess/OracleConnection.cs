// ADDS Oracle Data Access Layer
// .NET Framework 4.5 / Oracle.DataAccess 11.2 (ODP.NET unmanaged)
// NOTE: All dynamic SQL has been replaced with OracleParameter-bound parameterized queries.

using System;
using System.Data;
    public class OracleConnectionFactory
    {
        // Hardcoded credentials - legacy pattern from 2004
        // TODO: Replace with encrypted config / environment-variable-based credential store.
        private const string HOST = "ORACLE11G-PROD";
        private const int PORT = 1521;
        private const string SID = "ADDSDB";
        private const string USER = "adds_user";
        private const string PASS = "adds_p@ss_2003!";  // plaintext – must be moved to secure store

        private static OracleConnection _sharedConnection;

        public void SaveEquipment(string tag, string type, string model)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) " +
                               "VALUES(:tag,:type,:model,SYSDATE)";
            var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("tag",   OracleDbType.Varchar2) { Value = tag   });
            cmd.Parameters.Add(new OracleParameter("type",  OracleDbType.Varchar2) { Value = type  });
            cmd.Parameters.Add(new OracleParameter("model", OracleDbType.Varchar2) { Value = model });
            cmd.ExecuteNonQuery();
        }

        public void DeleteEquipment(string tag)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "DELETE FROM EQUIPMENT WHERE TAG=:tag";
            var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("tag", OracleDbType.Varchar2) { Value = tag });
            cmd.ExecuteNonQuery();
        }

        public DataTable GetPipeRoutes()
        {
            var conn = OracleConnectionFactory.GetConnection();
    public class InstrumentRepository
    {
        public DataTable GetInstrumentsByArea(string area)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "SELECT * FROM INSTRUMENTS WHERE AREA=:area";
            var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("area", OracleDbType.Varchar2) { Value = area });
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public void UpdateInstrument(string tag, string value)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "UPDATE INSTRUMENTS SET LAST_VALUE=:value, " +
                               "UPDATED=SYSDATE WHERE TAG=:tag";
            var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("value", OracleDbType.Varchar2) { Value = value });
            cmd.Parameters.Add(new OracleParameter("tag",   OracleDbType.Varchar2) { Value = tag   });
            cmd.ExecuteNonQuery();
        }
    }

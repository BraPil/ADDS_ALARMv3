// ADDS Oracle Data Access Layer
// .NET Framework 4.5 / Oracle.DataAccess 11.2 (ODP.NET unmanaged)
// TODO: still using OCI8 for some legacy stored proc calls

using System;
using System.Data;
using Oracle.DataAccess.Client;   // ODP.NET unmanaged - deprecated
using Oracle.DataAccess.Types;

namespace ADDS.DataAccess
{
    // ---------------------------------------------------------------------------
    // Repository interfaces – depend on abstractions, not on Oracle driver types
    // ---------------------------------------------------------------------------

    public interface IEquipmentRepository
    {
        DataTable GetAllEquipment();
        void SaveEquipment(string tag, string type, string model);
        void DeleteEquipment(string tag);
        DataTable GetPipeRoutes();
    }

    public interface IPipeRepository
    {
        DataTable GetPipeRoutes();
    }

    public interface IStoredProcedureRunner
    {
        object RunProc(string procName, params object[] args);
        DataTable RunQuery(string sql);
    }

    // ---------------------------------------------------------------------------
    // Connection factory (unchanged – single responsibility: open/close)
    // ---------------------------------------------------------------------------

    public class OracleConnectionFactory
    {
        // Hardcoded credentials - legacy pattern from 2004
        }
    }

    // ---------------------------------------------------------------------------
    // Concrete Oracle implementation of IEquipmentRepository
    // ---------------------------------------------------------------------------

    public class OracleEquipmentRepository : IEquipmentRepository
    {
        private readonly OracleConnectionFactory _factory;

        // Default constructor keeps backwards compatibility with existing call-sites
        // that create instances without DI.
        public OracleEquipmentRepository() { }

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
            var cmd = new OracleCommand(
                "INSERT INTO EQUIPMENT(TAG,TYPE,MODEL,CREATED_DATE) VALUES(:tag,:type,:model,SYSDATE)",
                conn);
            cmd.Parameters.Add(new OracleParameter("tag",   tag));
            cmd.Parameters.Add(new OracleParameter("type",  type));
            cmd.Parameters.Add(new OracleParameter("model", model));
            cmd.ExecuteNonQuery();
        }

        public void DeleteEquipment(string tag)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand("DELETE FROM EQUIPMENT WHERE TAG=:tag", conn);
            cmd.Parameters.Add(new OracleParameter("tag", tag));
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

    // ---------------------------------------------------------------------------
    // Keep the old name as a type alias so existing code compiles unchanged
    // ---------------------------------------------------------------------------

    [Obsolete("Use OracleEquipmentRepository / IEquipmentRepository instead.")]
    public class EquipmentRepository : OracleEquipmentRepository { }

    // ---------------------------------------------------------------------------
    // InstrumentRepository (parameterised queries – injection fix)
    // ---------------------------------------------------------------------------

    public class InstrumentRepository
    {
        public DataTable GetInstrumentsByArea(string area)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand("SELECT * FROM INSTRUMENTS WHERE AREA=:area", conn);
            cmd.Parameters.Add(new OracleParameter("area", area));
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
        public void UpdateInstrument(string tag, string value)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(
                "UPDATE INSTRUMENTS SET LAST_VALUE=:value, UPDATED=SYSDATE WHERE TAG=:tag", conn);
            cmd.Parameters.Add(new OracleParameter("value", value));
            cmd.Parameters.Add(new OracleParameter("tag",   tag));
            cmd.ExecuteNonQuery();
        }
    }

// ADDS Project Service
using System;
using System.Data;
using ADDS.DataAccess;

namespace ADDS.Services
{
    public class ProjectService
    {
        public static void OpenProject(string projectId)
        {
            var conn = OracleConnectionFactory.GetConnection();
            // Unsafe string interpolation
            var sql = $"SELECT * FROM PROJECTS WHERE PROJECT_ID='{projectId}'";
            var cmd = new Oracle.DataAccess.Client.OracleCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // Store project context in static state - not thread safe
                ProjectContext.CurrentProjectId = projectId;
                ProjectContext.CurrentProjectName = reader["NAME"].ToString();
                ProjectContext.OracleSchema = reader["SCHEMA_NAME"].ToString();
            }
        }

        public static DataTable GetProjectList()
        {
            return StoredProcedureRunner.RunQuery("SELECT * FROM PROJECTS ORDER BY CREATED_DATE DESC");
        }

        public static void CreateProject(string name, string description)
        {
            var sql = $"INSERT INTO PROJECTS(PROJECT_ID,NAME,DESCRIPTION,CREATED_DATE)" +
                      $" VALUES(SYS_GUID(),'{name}','{description}',SYSDATE)";
            StoredProcedureRunner.RunQuery(sql);
        }
    }

    public static class ProjectContext
    {
        public static string CurrentProjectId { get; set; }
        public static string CurrentProjectName { get; set; }
        public static string OracleSchema { get; set; }
    }
}

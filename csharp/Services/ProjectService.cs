// ADDS Project Service
using System;
using System.Data;
using ADDS.DataAccess;
using Microsoft.Extensions.Logging;

namespace ADDS.Services
{
    public class ProjectService
    {
        private static readonly ILogger _logger =
            LoggerFactory.GetLogger<ProjectService>();

        public static void OpenProject(string projectId)
        {
            _logger.LogInformation("ProjectService.OpenProject: Opening project with PROJECT_ID={ProjectId}.", projectId);
            var conn = OracleConnectionFactory.GetConnection();
            // Unsafe string interpolation
            var sql = $"SELECT * FROM PROJECTS WHERE PROJECT_ID='{projectId}'";
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // Store project context in static state - not thread safe
                ProjectContext.CurrentProjectId = projectId;
                ProjectContext.CurrentProjectName = reader["NAME"].ToString();
                ProjectContext.OracleSchema = reader["SCHEMA_NAME"].ToString();
                _logger.LogInformation(
                    "ProjectService.OpenProject: Project loaded — ID={ProjectId}, Name='{ProjectName}', Schema='{Schema}'.",
                    projectId,
                    ProjectContext.CurrentProjectName,
                    ProjectContext.OracleSchema);
            }
            else
            {
                _logger.LogWarning("ProjectService.OpenProject: No project found for PROJECT_ID={ProjectId}.", projectId);
            }
        }

        public static DataTable GetProjectList()
        {
            _logger.LogDebug("ProjectService.GetProjectList: Retrieving project list.");
            var dt = StoredProcedureRunner.RunQuery("SELECT * FROM PROJECTS ORDER BY CREATED_DATE DESC");
            _logger.LogInformation("ProjectService.GetProjectList: Retrieved {RowCount} projects.", dt.Rows.Count);
            return dt;
        }

        public static void CreateProject(string name, string description)
        {
            _logger.LogInformation("ProjectService.CreateProject: Creating project Name='{Name}'.", name);
            var sql = $"INSERT INTO PROJECTS(PROJECT_ID,NAME,DESCRIPTION,CREATED_DATE)" +
                      $" VALUES(SYS_GUID(),'{name}','{description}',SYSDATE

// ADDS Project Service
// Modernized: parameterized SQL, async/await, no static mutable state

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using ADDS.DataAccess;

namespace ADDS.Services
{
    public class ProjectService
    {
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ILogger<ProjectService> logger)
        {
            _logger = logger;
        }

        public async Task<ProjectContext> OpenProjectAsync(string projectId)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "SELECT PROJECT_ID, NAME, SCHEMA_NAME FROM PROJECTS WHERE PROJECT_ID=:id", conn);
            cmd.Parameters.Add(new OracleParameter("id", projectId));
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException($"Project '{projectId}' not found.");

            var ctx = new ProjectContext
            {
                ProjectId   = reader["PROJECT_ID"].ToString(),
                ProjectName = reader["NAME"].ToString(),
                OracleSchema = reader["SCHEMA_NAME"].ToString(),
            };
            _logger.LogInformation("OpenProject: loaded project {Id} ({Name})", ctx.ProjectId, ctx.ProjectName);
            return ctx;
        }

        public async Task<DataTable> GetProjectListAsync()
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "SELECT PROJECT_ID, NAME, CREATED_DATE FROM PROJECTS ORDER BY CREATED_DATE DESC", conn);
            using var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            await Task.Run(() => da.Fill(dt));
            return dt;
        }

        public async Task CreateProjectAsync(string name, string description)
        {
            using var conn = await OracleConnectionFactory.GetConnectionAsync();
            using var cmd = new OracleCommand(
                "INSERT INTO PROJECTS(PROJECT_ID,NAME,DESCRIPTION,CREATED_DATE)" +
                " VALUES(SYS_GUID(),:name,:desc,SYSDATE)", conn);
            cmd.Parameters.Add(new OracleParameter("name", name));
            cmd.Parameters.Add(new OracleParameter("desc", description));
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("CreateProject: '{Name}'", name);
        }
    }

    public class ProjectContext
    {
        public string ProjectId    { get; init; }
        public string ProjectName  { get; init; }
        public string OracleSchema { get; init; }
    }
}

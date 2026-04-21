// ADDS Oracle Data Access Layer
// .NET Framework 4.5 / Oracle.DataAccess 11.2 (ODP.NET unmanaged)
// TODO: still using OCI8 for some legacy stored proc calls
// Credentials are now sourced from environment variables. Do NOT add secrets back to this file.

using System;
using System.Data;
{
    public class OracleConnectionFactory
    {
        private static OracleConnection _sharedConnection;

        /// <summary>
        /// Reads Oracle connection parameters from environment variables.
        /// Required variables:
        ///   ADDS_DB_HOST     – Oracle hostname or IP
        ///   ADDS_DB_PORT     – Oracle listener port (defaults to 1521 when absent)
        ///   ADDS_DB_SID      – Oracle SID or service name
        ///   ADDS_DB_USER     – Database username
        ///   ADDS_DB_PASSWORD – Database password
        /// Set these variables in the OS, a CI/CD secret store, or Azure Key Vault
        /// before starting the application. Never commit credential values to source control.
        /// </summary>
        private static string BuildConnectionString()
        {
            string host = GetRequiredEnv("ADDS_DB_HOST");
            string portStr = Environment.GetEnvironmentVariable("ADDS_DB_PORT");
            int port = string.IsNullOrWhiteSpace(portStr) ? 1521 : int.Parse(portStr);
            string sid  = GetRequiredEnv("ADDS_DB_SID");
            string user = GetRequiredEnv("ADDS_DB_USER");
            string pass = GetRequiredEnv("ADDS_DB_PASSWORD");

            return $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                   $"(HOST={host})(PORT={port}))(CONNECT_DATA=(SID={sid})));" +
                   $"User Id={user};Password={pass};";
        }

        private static string GetRequiredEnv(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Required environment variable '{name}' is not set. " +
                    "Configure it via the OS environment, a secrets manager, or Azure Key Vault.");
            return value;
        }

        public static OracleConnection GetConnection()
        {
            if (_sharedConnection == null || _sharedConnection.State == ConnectionState.Closed)
            {
                _sharedConnection = new OracleConnection(BuildConnectionString());
                _sharedConnection.Open();
            }
            return _sharedConnection;

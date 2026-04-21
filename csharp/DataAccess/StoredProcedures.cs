// ADDS Stored Procedure Wrappers
// ODP.NET unmanaged Oracle.DataAccess 11.2
// NOTE: Raw-SQL passthrough and string-concatenation patterns replaced with parameterized queries.

using System;
using System.Data;
            return outParam.Value;
        }

        /// <summary>
        /// Executes a named stored procedure (no ad-hoc SQL passthrough).
        /// Pass bind values via <paramref name="parameters"/>.
        /// </summary>
        public static DataTable RunProcQuery(string procName, params OracleParameter[] parameters)
        {
            var conn = OracleConnectionFactory.GetConnection();
            var cmd = new OracleCommand(procName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            foreach (var p in parameters)
                cmd.Parameters.Add(p);
            var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
        public static void GenerateReport(string reportType, string outputPath)
        {
            var conn = OracleConnectionFactory.GetConnection();
            const string sql = "SELECT * FROM REPORT_TEMPLATES WHERE REPORT_TYPE=:reportType";
            var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("reportType", OracleDbType.Varchar2)
                               { Value = reportType });
            using (var reader = cmd.ExecuteReader())
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                while (reader.Read())
                {
                    writer.WriteLine(reader["CONTENT"].ToString());
                }
            }

    public class BulkDataLoader
    {
        /// <summary>
        /// Allowed table names for bulk load operations.  Restricting to a whitelist prevents
        /// identifier injection because Oracle bind variables cannot be used for table/column
        /// names; we must validate the identifier out-of-band.
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> AllowedTables =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "EQUIPMENT",
                "INSTRUMENTS",
                "PIPE_ROUTES",
                "REPORT_TEMPLATES"
                // Extend this list for every table that legitimately receives bulk loads.
            };

        /// <summary>
        /// Loads CSV-style rows from <paramref name="filePath"/> into <paramref name="tableName"/>.
        /// Each non-empty line must contain exactly the column values in the order expected by the
        /// target table, separated by commas.  Values are bound as Varchar2 parameters; the
        /// database is responsible for implicit conversion to the column's native type.
        /// </summary>
        public static void LoadFromFile(string filePath, string tableName)
        {
            // Validate the identifier against a whitelist – bind variables cannot cover DDL names.
            if (!AllowedTables.Contains(tableName))
                throw new ArgumentException(
                    $"Table '{tableName}' is not in the bulk-load allowlist.", nameof(tableName));

            var lines = System.IO.File.ReadAllLines(filePath);
            var conn = OracleConnectionFactory.GetConnection();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Split the CSV line into individual field values.
                var fields = line.Split(',');

                // Build a parameterized INSERT: INSERT INTO <whitelisted_table> VALUES (:p0,:p1,…)
                // tableName is safe here because it passed the whitelist check above.
                var placeholders = new string[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                    placeholders[i] = $":p{i}";

                var sql = $"INSERT INTO {tableName} VALUES ({string.Join(",", placeholders)})";
                var cmd = new OracleCommand(sql, conn);

                for (int i = 0; i < fields.Length; i++)
                    cmd.Parameters.Add(new OracleParameter($"p{i}", OracleDbType.Varchar2)
                                       { Value = fields[i].Trim() });

                // Propagate exceptions to callers; swallowing errors hides data integrity problems.
                cmd.ExecuteNonQuery();
            }
        }

        public static int GetRowCount(string tableName)
        {
            if (!AllowedTables.Contains(tableName))
                throw new ArgumentException(
                    $"Table '{tableName}' is not in the bulk-load allowlist.", nameof(tableName));

            var conn = OracleConnectionFactory.GetConnection();
            // tableName is safe here because it passed the whitelist check above.
            var cmd = new OracleCommand($"SELECT COUNT(*) FROM {tableName}", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

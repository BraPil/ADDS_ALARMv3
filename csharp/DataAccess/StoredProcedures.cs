// ADDS Stored Procedure Wrappers
// ODP.NET unmanaged Oracle.DataAccess 11.2

using System;
using System.Collections.Generic;
using System.Data;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;


    public class BulkDataLoader
    {
        public const int DefaultBatchSize = 500;

        public static void LoadFromFile(string filePath, string tableName, int batchSize = DefaultBatchSize)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^\w+$"))
                throw new ArgumentException(
                    $"Invalid tableName '{tableName}': only alphanumeric characters and underscores are allowed.",
                    nameof(tableName));

            var lines = System.IO.File.ReadAllLines(filePath);
            var conn = OracleConnectionFactory.GetConnection();

            int columnCount = 0;
            string insertSql = null;
            var batch = new List<string[]>();
            int fileRowNumber = 0;
            int batchStartRow = 1;

            foreach (var line in lines)
            {
                fileRowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = line.Split(',');

                if (insertSql == null)
                {
                    columnCount = columns.Length;
                    var paramList = new System.Text.StringBuilder();
                    for (int i = 0; i < columnCount; i++)
                    {
                        if (i > 0) paramList.Append(", ");
                        paramList.Append($":p{i}");
                    }
                    insertSql = $"INSERT INTO {tableName} VALUES ({paramList})";
                    batchStartRow = fileRowNumber;
                }

                batch.Add(columns);

                if (batch.Count >= batchSize)
                {
                    ExecuteBatch(conn, insertSql, batch, columnCount, batchStartRow);
                    batchStartRow = fileRowNumber + 1;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                ExecuteBatch(conn, insertSql, batch, columnCount, batchStartRow);
        }

        private static void ExecuteBatch(
            OracleConnection conn,
            string insertSql,
            List<string[]> batch,
            int columnCount,
            int batchStartRow)
        {
            try
            {
                var cmd = new OracleCommand(insertSql, conn);
                cmd.ArrayBindCount = batch.Count;

                for (int i = 0; i < columnCount; i++)
                {
                    var colValues = new string[batch.Count];
                    for (int row = 0; row < batch.Count; row++)
                        colValues[row] = batch[row][i];

                    var param = new OracleParameter($":p{i}", OracleDbType.Varchar2);
                    param.Value = colValues;
                    cmd.Parameters.Add(param);
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Batch insert failed starting at file row {batchStartRow} " +
                    $"({batch.Count} rows in batch). See inner exception for details.",
                    ex);
            }
        }

        public static int GetRowCount(string tableName)

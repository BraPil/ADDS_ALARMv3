// ADDS Sync Service - background Oracle synchronization
// .NET Framework 4.5 - no async/await, uses BackgroundWorker

using System;
using System.ComponentModel;
using System.Data;
using ADDS.DataAccess;
using Microsoft.Extensions.Logging;

namespace ADDS.Services
{
    public class SyncService
    {
        private static BackgroundWorker _worker;
        private static bool _isRunning;
        private static readonly ILogger _logger =
            LoggerFactory.GetLogger<SyncService>();

        public static void StartSync()
        {
            if (_isRunning) return;
            _logger.LogInformation("SyncService: Starting background Oracle sync.");
            _worker = new BackgroundWorker();
            _worker.DoWork += DoSync;
            _worker.RunWorkerCompleted += SyncComplete;
            _worker.RunWorkerAsync();
            _isRunning = true;
        }

        private static void DoSync(object sender, DoWorkEventArgs e)
        {
            _logger.LogDebug("SyncService.DoSync: Acquiring Oracle connection.");
            var conn = OracleConnectionFactory.GetConnection();
            _logger.LogDebug("SyncService.DoSync: Oracle connection acquired. Querying changed records.");
            // Pull all changed records - no change tracking, full table scan
            var dt = StoredProcedureRunner.RunQuery(
                "SELECT * FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24");

            _logger.LogInformation("SyncService.DoSync: Retrieved {RowCount} changed equipment records.", dt.Rows.Count);

            foreach (DataRow row in dt.Rows)
            {
                SyncEquipmentRecord(row);
            }

            _logger.LogInformation("SyncService.DoSync: Sync pass completed successfully.");
        }

        private static void SyncEquipmentRecord(DataRow row)
        {
            var tag = row["TAG"].ToString();
            var type = row["TYPE"].ToString();
            _logger.LogDebug("SyncService.SyncEquipmentRecord: Updating cache for TAG={Tag}, TYPE={Type}.", tag, type);
            StoredProcedureRunner.RunProc("ADDS_PKG.UPDATE_EQUIPMENT_CACHE", tag, type);
            _logger.LogDebug("SyncService.SyncEquipmentRecord: Cache updated for TAG={Tag}.", tag);
        }

        private static void SyncComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            _isRunning = false;
            if (e.Error != null)
            {
                _logger.LogError(e.Error, "SyncService.SyncComplete: Sync failed with exception: {Message}", e.Error.Message);
                System.Diagnostics.EventLog.WriteEntry("ADDS",
                    $"Sync failed: {e.Error.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
            }
            else
            {
                _logger.LogInformation("SyncService.SyncComplete: Background sync worker completed without errors.");
            }
        }

        public static void StopSync()
        {
            _logger.LogInformation("SyncService.StopSync: Stopping background sync worker.");
            if (_worker != null && _worker.IsBusy)
                _worker.Dispose();
            _isRunning = false;
            _logger.LogInformation("SyncService.StopSync: Sync worker stopped.");
        }
    }

    public class ReportService
    {
        private static readonly ILogger _logger =
            LoggerFactory.GetLogger<ReportService>();

        public static void GenerateEquipmentReport(string outputPath)
        {
            _logger.LogInformation("ReportService.GenerateEquipmentReport: Generating equipment report to '{OutputPath}'.", outputPath);
            var dt = new EquipmentRepository().GetAllEquipment();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine("ADDS Equipment Report");
                writer.WriteLine(new string('=', 60));
                foreach (DataRow row in dt.Rows)
                {
                    writer.WriteLine($"{row["TAG"]}\t{row["TYPE"]}\t{row["MODEL"]}");
                }
            }
            _logger.LogInformation("ReportService.GenerateEquipmentReport: Equipment report written with {RowCount} rows.", dt.Rows.Count);
        }

        public static void GeneratePipeReport(string outputPath)
        {
            _logger.LogInformation("ReportService.GeneratePipeReport: Generating pipe route report to '{OutputPath}'.", outputPath);
            var dt = new EquipmentRepository().GetPipeRoutes();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine("ADDS Pipe Route Report");
                foreach (DataRow row in dt.Rows)
                    writer.WriteLine(row["TAG"] + "\t" + row["SPEC"]);
            }
            _logger.LogInformation("ReportService.GeneratePipeReport: Pipe route report written with {RowCount} rows.", dt.Rows.Count);
        }
    }
}

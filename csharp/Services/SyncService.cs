// ADDS Sync Service - background Oracle synchronization
// .NET Framework 4.5 - no async/await, uses BackgroundWorker

using System;
using System.ComponentModel;
using System.Data;
using ADDS.DataAccess;

namespace ADDS.Services
{
    public class SyncService
    {
        private static BackgroundWorker _worker;
        private static bool _isRunning;

        public static void StartSync()
        {
            if (_isRunning) return;
            _worker = new BackgroundWorker();
            _worker.DoWork += DoSync;
            _worker.RunWorkerCompleted += SyncComplete;
            _worker.RunWorkerAsync();
            _isRunning = true;
        }

        private static void DoSync(object sender, DoWorkEventArgs e)
        {
            var conn = OracleConnectionFactory.GetConnection();
            // Pull all changed records - no change tracking, full table scan
            var dt = StoredProcedureRunner.RunQuery(
                "SELECT * FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24");

            foreach (DataRow row in dt.Rows)
            {
                SyncEquipmentRecord(row);
            }
        }

        private static void SyncEquipmentRecord(DataRow row)
        {
            var tag = row["TAG"].ToString();
            var type = row["TYPE"].ToString();
            StoredProcedureRunner.RunProc("ADDS_PKG.UPDATE_EQUIPMENT_CACHE", tag, type);
        }

        private static void SyncComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            _isRunning = false;
            if (e.Error != null)
            {
                System.Diagnostics.EventLog.WriteEntry("ADDS",
                    $"Sync failed: {e.Error.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public static void StopSync()
        {
            if (_worker != null && _worker.IsBusy)
                _worker.Dispose();
            _isRunning = false;
        }
    }

    public class ReportService
    {
        public static void GenerateEquipmentReport(string outputPath)
        {
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
        }

        public static void GeneratePipeReport(string outputPath)
        {
            var dt = new EquipmentRepository().GetPipeRoutes();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine("ADDS Pipe Route Report");
                foreach (DataRow row in dt.Rows)
                    writer.WriteLine(row["TAG"] + "\t" + row["SPEC"]);
            }
        }
    }
}

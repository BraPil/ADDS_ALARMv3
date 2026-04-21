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
        // -----------------------------------------------------------------------
        // Dependencies injected via constructor; static helpers retained for
        // backwards-compatible call-sites that use SyncService.StartSync().
        // -----------------------------------------------------------------------

        private readonly IStoredProcedureRunner _runner;
        private readonly IEquipmentRepository   _equipmentRepo;

        public SyncService(IStoredProcedureRunner runner, IEquipmentRepository equipmentRepo)
        {
            _runner        = runner        ?? throw new ArgumentNullException("runner");
            _equipmentRepo = equipmentRepo ?? throw new ArgumentNullException("equipmentRepo");
        }

        // Instance-based sync (used by injected consumers / unit tests)
        public void RunSync()
        {
            var dt = _runner.RunQuery(
                "SELECT * FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24");

            foreach (DataRow row in dt.Rows)
                SyncEquipmentRecord(row);
        }

        private void SyncEquipmentRecord(DataRow row)
        {
            var tag  = row["TAG"].ToString();
            var type = row["TYPE"].ToString();
            _runner.RunProc("ADDS_PKG.UPDATE_EQUIPMENT_CACHE", tag, type);
        }

        // -----------------------------------------------------------------------
        // Static wrapper retained for existing call-sites
        // -----------------------------------------------------------------------

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
            // Resolve default implementations for the static path
            var svc = new SyncService(
                OracleStoredProcedureRunner.Default,
                new OracleEquipmentRepository());
            svc.RunSync();
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
        private readonly IEquipmentRepository _equipmentRepo;

        // Constructor injection
        public ReportService(IEquipmentRepository equipmentRepo)
        {
            _equipmentRepo = equipmentRepo ?? throw new ArgumentNullException("equipmentRepo");
        }

        // Default constructor for backwards-compatible static call-sites
        public ReportService() : this(new OracleEquipmentRepository()) { }

        public static void GenerateEquipmentReport(string outputPath)
        {
            var dt = new OracleEquipmentRepository().GetAllEquipment();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine("ADDS Equipment Report");

        public static void GeneratePipeReport(string outputPath)
        {
            var dt = new OracleEquipmentRepository().GetPipeRoutes();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine("ADDS Pipe Route Report");

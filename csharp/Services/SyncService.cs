// ADDS Sync Service - background Oracle synchronization
// Async/await with Task and CancellationToken

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ADDS.DataAccess;

namespace ADDS.Services
{
    public class SyncService
    {
        private static CancellationTokenSource _cts;
        private static Task _syncTask;
        private static bool _isRunning;

        public static void StartSync()
        {
            if (_isRunning) return;
            _cts = new CancellationTokenSource();
            _syncTask = Task.Run(() => DoSyncAsync(_cts.Token), _cts.Token)
                .ContinueWith(SyncComplete, TaskScheduler.Default);
            _isRunning = true;
        }

        private static async Task DoSyncAsync(CancellationToken ct)
        {
            // Pull all changed records - no change tracking, full table scan
            var dt = await StoredProcedureRunner.RunQueryAsync(
                "SELECT * FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24",
                ct).ConfigureAwait(false);

            foreach (DataRow row in dt.Rows)
            {
                ct.ThrowIfCancellationRequested();
                await SyncEquipmentRecordAsync(row, ct).ConfigureAwait(false);
            }
        }

        private static async Task SyncEquipmentRecordAsync(DataRow row, CancellationToken ct)
        {
            var tag = row["TAG"].ToString();
            var type = row["TYPE"].ToString();
            await StoredProcedureRunner.RunProcAsync(
                "ADDS_PKG.UPDATE_EQUIPMENT_CACHE", ct, tag, type).ConfigureAwait(false);
        }

        private static void SyncComplete(Task completedTask)
        {
            _isRunning = false;
            if (completedTask.IsFaulted && completedTask.Exception != null)
            {
                System.Diagnostics.EventLog.WriteEntry("ADDS",
                    $"Sync failed: {completedTask.Exception.GetBaseException().Message}",
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public static void StopSync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _isRunning = false;
        }
    }
    public class ReportService
    {
        public static async Task GenerateEquipmentReportAsync(string outputPath,
            CancellationToken ct = default)
        {
            var dt = await new EquipmentRepository().GetAllEquipmentAsync(ct)
                .ConfigureAwait(false);
            using (var writer = new System.IO.StreamWriter(outputPath))
            {

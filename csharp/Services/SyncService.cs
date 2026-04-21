// ADDS Sync Service - background Oracle synchronization
// .NET 10 - uses Task/async/await and CancellationToken

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

        public static void StartSync()
        {
            if (_syncTask != null && !_syncTask.IsCompleted) return;
            _cts = new CancellationTokenSource();
            _syncTask = Task.Run(() => DoSyncAsync(_cts.Token), _cts.Token);
        }

        private static async Task DoSyncAsync(CancellationToken token)
        {
            try
            {
                var dt = StoredProcedureRunner.RunQuery(
                    "SELECT * FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24");

                foreach (DataRow row in dt.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    SyncEquipmentRecord(row);
                }
            }
            catch (OperationCanceledException)
            {
                // Sync was cancelled - expected
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Sync failed: {ex.Message}");
            }
        }

            StoredProcedureRunner.RunProc("ADDS_PKG.UPDATE_EQUIPMENT_CACHE", tag, type);
        }

        public static void StopSync()
        {
            _cts?.Cancel();
            try { _syncTask?.Wait(TimeSpan.FromSeconds(5)); }
            catch (AggregateException) { /* cancellation expected */ }
            finally { _cts?.Dispose(); _cts = null; }
        }
    }

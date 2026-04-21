// ADDS Sync Service
// Modernized: BackgroundWorker replaced with async/await Task, structured logging

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ADDS.DataAccess;

namespace ADDS.Services
{
    public class SyncService
    {
        private readonly ILogger<SyncService> _logger;
        private readonly StoredProcedureRunner _sprRunner;
        private readonly EquipmentRepository _equipRepo;
        private CancellationTokenSource _cts;

        public SyncService(
            ILogger<SyncService> logger,
            StoredProcedureRunner sprRunner,
            EquipmentRepository equipRepo)
        {
            _logger = logger;
            _sprRunner = sprRunner;
            _equipRepo = equipRepo;
        }

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public void StartSync()
        {
            if (IsRunning)
            {
                _logger.LogWarning("SyncService.StartSync: already running, ignoring.");
                return;
            }
            _cts = new CancellationTokenSource();
            _ = RunSyncLoopAsync(_cts.Token);
            _logger.LogInformation("SyncService started.");
        }

        public void StopSync()
        {
            _cts?.Cancel();
            _logger.LogInformation("SyncService stop requested.");
        }

        private async Task RunSyncLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await DoSyncAsync(ct);
                    await Task.Delay(TimeSpan.FromMinutes(1), ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SyncService loop cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncService loop failed.");
            }
        }

        private async Task DoSyncAsync(CancellationToken ct)
        {
            _logger.LogInformation("SyncService: beginning sync pass.");
            var dt = await _equipRepo.GetAllEquipmentAsync();

            foreach (DataRow row in dt.Rows)
            {
                ct.ThrowIfCancellationRequested();
                await SyncEquipmentRecordAsync(row);
            }
            _logger.LogInformation("SyncService: sync pass complete, {Count} records.", dt.Rows.Count);
        }

        private async Task SyncEquipmentRecordAsync(DataRow row)
        {
            var tag  = row["TAG"].ToString();
            var type = row["TYPE"].ToString();
            await _sprRunner.RunProcAsync(
                "ADDS_PKG.UPDATE_EQUIPMENT_CACHE",
                new Oracle.ManagedDataAccess.Client.OracleParameter("tag",  tag),
                new Oracle.ManagedDataAccess.Client.OracleParameter("type", type));
        }
    }

    public class ReportService
    {
        private readonly ILogger<ReportService> _logger;
        private readonly EquipmentRepository _equipRepo;

        public ReportService(ILogger<ReportService> logger, EquipmentRepository equipRepo)
        {
            _logger = logger;
            _equipRepo = equipRepo;
        }

        public async Task GenerateEquipmentReportAsync(string outputPath)
        {
            var dt = await _equipRepo.GetAllEquipmentAsync();
            using var writer = new System.IO.StreamWriter(outputPath);
            await writer.WriteLineAsync("ADDS Equipment Report");
            await writer.WriteLineAsync(new string('=', 60));
            foreach (DataRow row in dt.Rows)
                await writer.WriteLineAsync($"{row["TAG"]}\t{row["TYPE"]}\t{row["MODEL"]}");
            _logger.LogInformation("GenerateEquipmentReport: wrote {Path}", outputPath);
        }

        public async Task GeneratePipeReportAsync(string outputPath)
        {
            var dt = await _equipRepo.GetPipeRoutesAsync();
            using var writer = new System.IO.StreamWriter(outputPath);
            await writer.WriteLineAsync("ADDS Pipe Route Report");
            foreach (DataRow row in dt.Rows)
                await writer.WriteLineAsync($"{row["TAG"]}\t{row["SPEC"]}");
        }
    }
}

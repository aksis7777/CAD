using System.Text.Json;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;
using MiniPdm.Modules.Import.Abstractions;
using MiniPdm.Storage.Abstractions.Import;

namespace MiniPdm.Modules.Import.Services;

public sealed class ImportSaveException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class ImportService(
    ICadSourceFactory cadSourceFactory,
    IImportPersistence persistence,
    IImportSourceStorage sourceStorage)
{
    private static readonly JsonSerializerOptions ReportJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ImportReportDto> ExecuteAsync(Guid importId, CadSourceDescriptor source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var replay = await persistence.FindAsync(importId, cancellationToken);
        if (replay is { State: ImportCommitState.Completed, ReportJson: not null })
            return DeserializeReport(replay.ReportJson);
        if (replay is { State: ImportCommitState.Unknown })
            replay = await persistence.ResolveAsync(importId, cancellationToken);
        if (replay is { State: ImportCommitState.Completed, ReportJson: not null })
            return DeserializeReport(replay.ReportJson);
        if (replay is { State: ImportCommitState.Unknown })
            throw new ImportSaveException("The previous import outcome is still unknown; its files were retained.");

        var package = await ReadPackageAsync(source, cancellationToken);
        var validator = new ImportPackageValidator(package);
        validator.Validate();
        var lookup = validator.CreateLookup();
        var promoted = false;
        ImportPackagePlan? finalPlan = null;
        ImportPersistenceResult result;
        try
        {
            // Cancellation is observed after the transaction lock is acquired and before file promotion.
            // Once promotion succeeds, persistence runs without the caller's token so the saga reaches a result.
            result = await persistence.ExecuteAsync(importId, lookup, async (snapshot, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var plan = ImportPackagePlanner.Prepare(validator, snapshot);
                finalPlan = plan;
                cancellationToken.ThrowIfCancellationRequested();
                var acceptedFiles = plan.Files.Where(x => x.Status == ImportFileStatus.Accepted)
                    .Select(x => x.FileName).ToArray();
                await sourceStorage.PromoteAsync(importId, source, acceptedFiles, CancellationToken.None);
                promoted = true;
                return plan.ToWritePlan(importId, sourceStorage);
            }, cancellationToken);
        }
        catch (Exception ex) when (promoted)
        {
            var resolved = await ResolveAfterFailureAsync(importId);
            if (resolved?.State == ImportCommitState.Completed && resolved.ReportJson is not null)
                return DeserializeReport(resolved.ReportJson);
            if (resolved?.State == ImportCommitState.ConfirmedRollback)
            {
                var recovery = await CompensateOrReplayAsync(importId);
                if (recovery.Report is not null) return recovery.Report;
                if (recovery.Compensated)
                    throw new ImportSaveException("Import persistence failed and the database confirmed rollback; promoted files were compensated.", ex);
                throw new ImportSaveException("Import outcome could not be resolved; promoted files were retained for recovery.", ex);
            }
            throw new ImportSaveException("Import persistence outcome is unknown; source files were retained for recovery.", ex);
        }

        if (result.State == ImportCommitState.Completed && result.ReportJson is not null)
            return DeserializeReport(result.ReportJson);
        if (result.State == ImportCommitState.ConfirmedRollback)
        {
            var recovery = await CompensateOrReplayAsync(importId);
            if (recovery.Report is not null) return recovery.Report;
            if (recovery.Compensated)
                throw new ImportSaveException(result.Error ?? "Import persistence failed and the database rolled back; promoted files were compensated.");
            throw new ImportSaveException("Import outcome could not be resolved; promoted files were retained for recovery.",
                result.Error is null ? null : new InvalidOperationException(result.Error));
        }

        var resolution = await ResolveAfterFailureAsync(importId);
        if (resolution?.State == ImportCommitState.Completed && resolution.ReportJson is not null)
            return DeserializeReport(resolution.ReportJson);
        if (resolution?.State == ImportCommitState.ConfirmedRollback)
        {
            var recovery = await CompensateOrReplayAsync(importId);
            if (recovery.Report is not null) return recovery.Report;
            if (recovery.Compensated)
                throw new ImportSaveException("Import persistence failed and the database confirmed rollback; promoted files were compensated.");
            throw new ImportSaveException("Import outcome could not be resolved; promoted files were retained for recovery.");
        }
        throw new ImportSaveException("Import persistence outcome is unknown; source files were retained for recovery.");
    }

    private async Task<ImportPersistenceResult?> ResolveAfterFailureAsync(Guid id)
    {
        try { return await persistence.ResolveAsync(id, CancellationToken.None); }
        catch { return null; }
    }

    private async Task<(ImportReportDto? Report, bool Compensated)> CompensateOrReplayAsync(Guid id)
    {
        bool compensated;
        try
        {
            compensated = await persistence.CompensateIfRolledBackAsync(id,
                ct => sourceStorage.CompensateAsync(id, ct), CancellationToken.None);
        }
        catch { return (null, false); }
        if (compensated) return (null, true);
        var resolution = await ResolveAfterFailureAsync(id);
        return (resolution is { State: ImportCommitState.Completed, ReportJson: not null }
            ? DeserializeReport(resolution.ReportJson)
            : null, false);
    }

    private static ImportReportDto DeserializeReport(string json) =>
        JsonSerializer.Deserialize<ImportReportDto>(json, ReportJsonOptions)
        ?? throw new ImportSaveException("The stored import report is invalid.");

    private async Task<IReadOnlyList<ImportPackageValidator.FileEntry>> ReadPackageAsync(CadSourceDescriptor descriptor, CancellationToken ct)
    {
        await using var session = await cadSourceFactory.OpenAsync(descriptor, ct);
        var files = new List<ImportPackageValidator.FileEntry>();
        await foreach (var reference in session.Source.GetDocumentsAsync(ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            var result = await session.Reader.ReadAsync(reference, ct);
            files.Add(new ImportPackageValidator.FileEntry(reference.FileName, result.Document, result.Error));
        }
        return files;
    }
}

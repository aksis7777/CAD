using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniPdm.Modules.Import.Abstractions;
using MiniPdm.Modules.Import.Features.GetImportReport;
using MiniPdm.Modules.Import.Features.ImportPackage;
using MiniPdm.Modules.Import.Infrastructure.SourceFiles;
using MiniPdm.Modules.Import.Services;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using Microsoft.Extensions.Logging;

namespace MiniPdm.Modules.Import.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController(ISender sender, IImportUploadStorage uploads, ILogger<ImportsController> logger) : ControllerBase
{
    [HttpPost("{importId:guid}")]
    [RequestSizeLimit(64L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 64L * 1024 * 1024, ValueLengthLimit = 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(Guid importId, CancellationToken cancellationToken)
    {
        if (importId == Guid.Empty) return BadRequest("Import ID must not be empty.");
        var existingReport = await sender.Send(new GetImportReportQuery(importId), cancellationToken);
        if (existingReport is not null) return Ok(existingReport);
        if (!Request.HasFormContentType) return BadRequest("Expected multipart/form-data.");
        try
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            var files = form.Files.Select(f => new ImportUploadFile(f.FileName, f.OpenReadStream())).ToArray();
            try
            {
                await using var attempt = await uploads.StageAsync(importId, files, cancellationToken);
                var report = await sender.Send(new ImportPackageCommand(importId, attempt.SourceDescriptor), cancellationToken);
                return Ok(report);
            }
            finally
            {
                foreach (var file in files) await file.Content.DisposeAsync();
            }
        }
        catch (ImportUploadValidationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (ImportSaveException ex) { logger.LogWarning(ex, "Import saga outcome could not be confirmed for {ImportId}", importId); return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Import outcome could not be confirmed. Retry with the same import ID." }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (InvalidDataException ex) { logger.LogInformation(ex, "Rejected malformed import upload {ImportId}", importId); return BadRequest(new { error = "Malformed multipart upload." }); }
        catch (BadHttpRequestException ex) { logger.LogInformation(ex, "Rejected malformed import request {ImportId}", importId); return BadRequest(new { error = "Malformed multipart upload." }); }
        catch (Exception ex) { logger.LogError(ex, "Import {ImportId} failed", importId); return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Import could not be completed." }); }
    }

    [HttpGet("{importId:guid}")]
    public async Task<IActionResult> GetReport(Guid importId, CancellationToken cancellationToken)
    {
        if (importId == Guid.Empty) return BadRequest("Import ID must not be empty.");
        var report = await sender.Send(new GetImportReportQuery(importId), cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }
}

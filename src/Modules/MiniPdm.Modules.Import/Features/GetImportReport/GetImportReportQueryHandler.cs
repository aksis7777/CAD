using System.Text.Json;
using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Storage.Abstractions.Import;

namespace MiniPdm.Modules.Import.Features.GetImportReport;

public sealed class GetImportReportQueryHandler(IImportPersistence persistence) : IRequestHandler<GetImportReportQuery, ImportReportDto?>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ImportReportDto?> Handle(GetImportReportQuery request, CancellationToken cancellationToken)
    {
        var result = await persistence.FindAsync(request.ImportId, cancellationToken);
        if (result is null || result.State != ImportCommitState.Completed || result.ReportJson is null) return null;
        return JsonSerializer.Deserialize<ImportReportDto>(result.ReportJson, JsonOptions);
    }
}

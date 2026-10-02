using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Modules.Import.Abstractions.Database;
using MiniPdm.Modules.Import.DtoModels.Database;
using System.Text.Json;

namespace MiniPdm.Modules.Import.Features.Queries.GetImportReport;

public sealed record GetImportReportQuery(Guid ImportId) : IRequest<ImportReportDto?>;

public sealed class GetImportReportQueryHandler(IImportDatabaseService persistence) : IRequestHandler<GetImportReportQuery, ImportReportDto?>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ImportReportDto?> Handle(GetImportReportQuery request, CancellationToken cancellationToken)
    {
        var result = await persistence.FindAsync(request.ImportId, cancellationToken);
        if (result is null || result.State != ImportCommitState.Completed || result.ReportJson is null) return null;
        return JsonSerializer.Deserialize<ImportReportDto>(result.ReportJson, JsonOptions);
    }
}

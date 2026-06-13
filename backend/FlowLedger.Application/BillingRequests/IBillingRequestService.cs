using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;

namespace FlowLedger.Application.BillingRequests;

public interface IBillingRequestService
{
    Task<PagedResult<BillingRequestListItemDto>> GetAsync(BillingRequestQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<CsvResult> ExportCsvAsync(BillingRequestQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<BillingRequestDetailDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(CreateBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, UpdateBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task SubmitAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task ApproveAsync(Guid id, ApproveBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task RejectAsync(Guid id, RejectBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task AddCommentAsync(Guid id, AddCommentDto request, CurrentUser currentUser, CancellationToken cancellationToken);
}

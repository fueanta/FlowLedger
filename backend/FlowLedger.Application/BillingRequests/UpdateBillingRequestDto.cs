namespace FlowLedger.Application.BillingRequests;

public sealed record UpdateBillingRequestDto(
    Guid CustomerId,
    string Title,
    string Description,
    IReadOnlyList<CreateBillingRequestLineItemDto> LineItems);

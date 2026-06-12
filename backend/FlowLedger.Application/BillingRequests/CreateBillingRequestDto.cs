namespace FlowLedger.Application.BillingRequests;

public sealed record CreateBillingRequestDto(
    Guid CustomerId,
    string Title,
    string Description,
    IReadOnlyList<CreateBillingRequestLineItemDto> LineItems);

public sealed record CreateBillingRequestLineItemDto(string Description, int Quantity, decimal UnitPrice);

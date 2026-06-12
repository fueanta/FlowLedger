namespace FlowLedger.Application.Customers;

public sealed record CustomerDto(Guid Id, string Name, string ContactEmail, string Phone, string BillingAddress);

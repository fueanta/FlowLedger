namespace FlowLedger.Domain.Enums;

public enum AuditActionType
{
    Created = 1,
    Updated = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    Commented = 6,
    InvoiceGenerated = 7,
    PaymentMarked = 8,
    Assigned = 9,
    Cancelled = 10
}

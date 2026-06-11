namespace FlowLedger.Domain.Enums;

public enum BillingRequestStatus
{
    Draft = 1,
    Submitted = 2,
    AccountsReview = 3,
    ManagerApproval = 4,
    Approved = 5,
    Rejected = 6,
    InvoiceGenerated = 7,
    Paid = 8,
    Cancelled = 9
}

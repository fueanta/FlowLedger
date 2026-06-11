namespace FlowLedger.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

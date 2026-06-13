using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Infrastructure.Persistence.SeedData;

public static class FlowLedgerSeedData
{
    public const string JwtAccessTokenMinutesKey = "Jwt.AccessTokenMinutes";
    public const string VatPercentageKey = "Billing.VatPercentage";
    public const string ManagerApprovalThresholdKey = "Billing.ManagerApprovalThreshold";
    public const string InvoiceDueDaysKey = "Billing.InvoiceDueDays";

    public static readonly Guid SalesUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AccountsUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ManagerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid FiberRetailCustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    public static readonly Guid MetroLogisticsCustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
    public static readonly Guid NorthstarCustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");
    public static readonly Guid GreenlineCustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
    public static readonly Guid BluePeakCustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5");
    public static readonly Guid EasternTradingCustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6");

    private static readonly DateTime BaseDate = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    public static readonly User[] Users =
    [
        new()
        {
            Id = SalesUserId,
            FullName = "Sarah Sales",
            Email = "sales@flowledger.local",
            Role = RoleName.Sales,
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAtUtc = BaseDate,
            UpdatedAtUtc = BaseDate
        },
        new()
        {
            Id = AccountsUserId,
            FullName = "Amir Accounts",
            Email = "accounts@flowledger.local",
            Role = RoleName.Accounts,
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAtUtc = BaseDate,
            UpdatedAtUtc = BaseDate
        },
        new()
        {
            Id = ManagerUserId,
            FullName = "Mona Manager",
            Email = "manager@flowledger.local",
            Role = RoleName.Manager,
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAtUtc = BaseDate,
            UpdatedAtUtc = BaseDate
        },
        new()
        {
            Id = AdminUserId,
            FullName = "Adam Admin",
            Email = "admin@flowledger.local",
            Role = RoleName.Admin,
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAtUtc = BaseDate,
            UpdatedAtUtc = BaseDate
        }
    ];

    public static readonly AppSetting[] AppSettings =
    [
        new()
        {
            Key = JwtAccessTokenMinutesKey,
            Value = "30",
            Description = "Access token lifetime in minutes."
        },
        new()
        {
            Key = VatPercentageKey,
            Value = "15",
            Description = "VAT percentage used for new billing request totals."
        },
        new()
        {
            Key = ManagerApprovalThresholdKey,
            Value = "100000",
            Description = "Total amount above which Accounts approval routes to Management."
        },
        new()
        {
            Key = InvoiceDueDaysKey,
            Value = "30",
            Description = "Number of days after issue date used for new invoice due dates."
        }
    ];

    public static readonly Customer[] Customers =
    [
        Customer(FiberRetailCustomerId, "Fiber Retail Ltd.", "Nadia Rahman", "billing@fiberretail.local", "+8801700000001", "House 11, Road 7, Dhaka", "TIN-FIBER-001"),
        Customer(MetroLogisticsCustomerId, "Metro Logistics Bangladesh", "Tariq Hasan", "finance@metrologistics.local", "+8801700000002", "Port Road, Chattogram", "TIN-METRO-002"),
        Customer(NorthstarCustomerId, "Northstar Enterprise", "Rumana Islam", "accounts@northstar.local", "+8801700000003", "Airport Road, Dhaka", "TIN-NORTH-003"),
        Customer(GreenlineCustomerId, "Greenline Distribution", "Farhan Kabir", "billing@greenline.local", "+8801700000004", "Industrial Area, Gazipur", "TIN-GREEN-004"),
        Customer(BluePeakCustomerId, "BluePeak Systems", "Mahira Chowdhury", "finance@bluepeak.local", "+8801700000005", "Banani, Dhaka", "TIN-BLUE-005"),
        Customer(EasternTradingCustomerId, "Eastern Trading Co.", "Sabbir Ahmed", "accounts@easterntrading.local", "+8801700000006", "Motijheel, Dhaka", "TIN-EAST-006")
    ];

    public static readonly BillingRequest[] BillingRequests =
    [
        BillingRequest(1, FiberRetailCustomerId, "Retail starter billing", BillingRequestStatus.Draft, 12000m, null, null, null),
        BillingRequest(2, MetroLogisticsCustomerId, "Monthly logistics support", BillingRequestStatus.Draft, 78000m, null, null, null),
        BillingRequest(3, GreenlineCustomerId, "Small distribution order", BillingRequestStatus.AccountsReview, 28000m, AccountsUserId, 1, null),
        BillingRequest(4, FiberRetailCustomerId, "Fiber Retail service package", BillingRequestStatus.AccountsReview, 45000m, AccountsUserId, 1, null),
        BillingRequest(5, BluePeakCustomerId, "Platform implementation advance", BillingRequestStatus.ManagerApproval, 125000m, ManagerUserId, 1, null),
        BillingRequest(6, MetroLogisticsCustomerId, "Metro Logistics annual support", BillingRequestStatus.AccountsReview, 180000m, AccountsUserId, 1, null),
        BillingRequest(7, EasternTradingCustomerId, "Enterprise procurement billing", BillingRequestStatus.ManagerApproval, 250000m, ManagerUserId, 1, null),
        BillingRequest(8, NorthstarCustomerId, "Northstar revised billing", BillingRequestStatus.Rejected, 65000m, SalesUserId, 1, 3),
        BillingRequest(9, GreenlineCustomerId, "Greenline returned billing", BillingRequestStatus.Rejected, 35000m, SalesUserId, 1, 3),
        BillingRequest(10, BluePeakCustomerId, "BluePeak subscription billing", BillingRequestStatus.InvoiceGenerated, 52000m, AccountsUserId, 1, null),
        BillingRequest(11, FiberRetailCustomerId, "Fiber Retail replenishment", BillingRequestStatus.InvoiceGenerated, 92000m, AccountsUserId, 1, null),
        BillingRequest(12, MetroLogisticsCustomerId, "Metro Logistics freight billing", BillingRequestStatus.InvoiceGenerated, 85000m, AccountsUserId, 1, null),
        BillingRequest(13, EasternTradingCustomerId, "Eastern Trading supply billing", BillingRequestStatus.InvoiceGenerated, 140000m, ManagerUserId, 1, null),
        BillingRequest(14, NorthstarCustomerId, "Northstar completion billing", BillingRequestStatus.Paid, 32000m, AccountsUserId, 1, null, true),
        BillingRequest(15, GreenlineCustomerId, "Greenline delivery billing", BillingRequestStatus.Paid, 88000m, AccountsUserId, 1, null, true),
        BillingRequest(16, BluePeakCustomerId, "BluePeak enterprise billing", BillingRequestStatus.Paid, 350000m, ManagerUserId, 1, null, true),
        BillingRequest(17, FiberRetailCustomerId, "Cancelled duplicate request", BillingRequestStatus.Cancelled, 40000m, null, null, null)
    ];

    public static readonly BillingRequestLineItem[] LineItems =
        BillingRequests.Select(request => new BillingRequestLineItem
        {
            Id = Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-{GetSuffix(request.RequestNumber)}"),
            BillingRequestId = request.Id,
            Description = $"{request.Title} line item",
            Quantity = 1,
            UnitPrice = request.SubtotalAmount,
            LineTotal = request.SubtotalAmount
        }).ToArray();

    public static readonly Invoice[] Invoices =
    [
        Invoice(1, 10, BluePeakCustomerId, InvoiceStatus.Issued, null),
        Invoice(2, 11, FiberRetailCustomerId, InvoiceStatus.Issued, null),
        Invoice(3, 12, MetroLogisticsCustomerId, InvoiceStatus.Issued, null),
        Invoice(4, 13, EasternTradingCustomerId, InvoiceStatus.Issued, null),
        Invoice(5, 14, NorthstarCustomerId, InvoiceStatus.Paid, 8),
        Invoice(6, 15, GreenlineCustomerId, InvoiceStatus.Paid, 7),
        Invoice(7, 16, BluePeakCustomerId, InvoiceStatus.Paid, 5)
    ];

    public static readonly Comment[] Comments =
    [
        Comment(1, 8, AccountsUserId, "Rejected pending corrected purchase order."),
        Comment(2, 9, ManagerUserId, "Rejected because billed amount needs clarification."),
        Comment(3, 6, AccountsUserId, "High-value request needs management approval after accounts review.")
    ];

    public static readonly AuditLog[] AuditLogs =
    [
        AuditLog(1, 1, SalesUserId, AuditActionType.Created, "Billing request created."),
        AuditLog(2, 4, SalesUserId, AuditActionType.Submitted, "Billing request submitted to Accounts."),
        AuditLog(3, 6, SalesUserId, AuditActionType.Submitted, "Billing request submitted to Accounts."),
        AuditLog(4, 8, AccountsUserId, AuditActionType.Rejected, "Accounts rejected request for revision."),
        AuditLog(5, 10, AccountsUserId, AuditActionType.InvoiceGenerated, "Invoice generated after Accounts approval."),
        AuditLog(6, 14, AccountsUserId, AuditActionType.PaymentMarked, "Invoice marked as paid.")
    ];

    public static readonly Notification[] Notifications =
    [
        Notification(1, AccountsUserId, "Request ready for review", "BR-2026-0004 is waiting for Accounts review."),
        Notification(2, ManagerUserId, "Manager approval needed", "BR-2026-0007 is waiting for Management approval."),
        Notification(3, SalesUserId, "Request rejected", "BR-2026-0008 needs revision.")
    ];

    private static Customer Customer(Guid id, string name, string contactPerson, string email, string phone, string address, string taxIdentifier)
    {
        return new Customer
        {
            Id = id,
            Name = name,
            ContactPerson = contactPerson,
            ContactEmail = email,
            Phone = phone,
            BillingAddress = address,
            TaxIdentifier = taxIdentifier,
            Status = ClientStatus.Active,
            UpdatedAtUtc = BaseDate,
            CreatedAtUtc = BaseDate
        };
    }

    private static BillingRequest BillingRequest(
        int number,
        Guid customerId,
        string title,
        BillingRequestStatus status,
        decimal totalAmount,
        Guid? assignedToUserId,
        int? submittedAfterDays,
        int? rejectedAfterDays,
        bool paid = false)
    {
        var createdAtUtc = BaseDate.AddDays(number);
        DateTime? submittedAtUtc = submittedAfterDays is null ? null : createdAtUtc.AddDays(submittedAfterDays.Value);
        DateTime? approvedAtUtc = status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid ? createdAtUtc.AddDays(2) : null;
        DateTime? rejectedAtUtc = rejectedAfterDays is null ? null : createdAtUtc.AddDays(rejectedAfterDays.Value);
        var updatedAtUtc = paid ? createdAtUtc.AddDays(4) : createdAtUtc.AddDays(1);
        var subtotal = Math.Round(totalAmount / 1.15m, 2);
        var assignedQueue = status switch
        {
            BillingRequestStatus.Draft or BillingRequestStatus.Rejected => WorkflowQueue.Sales,
            BillingRequestStatus.AccountsReview => WorkflowQueue.Accounts,
            BillingRequestStatus.ManagerApproval => WorkflowQueue.Manager,
            _ => WorkflowQueue.None
        };

        return new BillingRequest
        {
            Id = BillingRequestId(number),
            RequestNumber = $"BR-2026-{number:0000}",
            CustomerId = customerId,
            Title = title,
            Description = $"{title} for seeded workflow testing.",
            Status = status,
            CreatedByUserId = SalesUserId,
            AssignedToUserId = assignedQueue == WorkflowQueue.None ? null : assignedToUserId,
            AssignedQueue = assignedQueue,
            AssignedAtUtc = assignedQueue == WorkflowQueue.None ? null : rejectedAtUtc ?? submittedAtUtc ?? createdAtUtc,
            SubmittedByUserId = submittedAtUtc is null ? null : SalesUserId,
            AccountsReviewedByUserId = status is BillingRequestStatus.ManagerApproval or BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid
                ? AccountsUserId
                : null,
            ManagerReviewedByUserId = status is BillingRequestStatus.InvoiceGenerated or BillingRequestStatus.Paid && totalAmount > 100000m
                ? ManagerUserId
                : null,
            LastWorkflowActionAtUtc = rejectedAtUtc ?? approvedAtUtc ?? submittedAtUtc ?? updatedAtUtc,
            SubtotalAmount = subtotal,
            VatAmount = totalAmount - subtotal,
            TotalAmount = totalAmount,
            SubmittedAtUtc = submittedAtUtc,
            ApprovedAtUtc = approvedAtUtc,
            RejectedAtUtc = rejectedAtUtc,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private static Invoice Invoice(int number, int billingRequestNumber, Guid customerId, InvoiceStatus status, int? paidAfterDays)
    {
        var request = BillingRequests.Single(x => x.Id == BillingRequestId(billingRequestNumber));
        var issuedAtUtc = request.ApprovedAtUtc ?? request.UpdatedAtUtc;

        return new Invoice
        {
            Id = Guid.Parse($"cccccccc-cccc-cccc-cccc-{number:000000000000}"),
            InvoiceNumber = $"INV-2026-{number:0000}",
            BillingRequestId = request.Id,
            CustomerId = customerId,
            SubtotalAmount = request.SubtotalAmount,
            VatPercentage = 15m,
            VatAmount = request.VatAmount,
            TotalAmount = request.TotalAmount,
            Status = status,
            IssuedAtUtc = issuedAtUtc,
            DueDays = 30,
            DueAtUtc = issuedAtUtc.AddDays(30),
            PaidAtUtc = paidAfterDays is null ? null : issuedAtUtc.AddDays(paidAfterDays.Value)
        };
    }

    private static Comment Comment(int number, int billingRequestNumber, Guid authorUserId, string body)
    {
        return new Comment
        {
            Id = Guid.Parse($"dddddddd-dddd-dddd-dddd-{number:000000000000}"),
            BillingRequestId = BillingRequestId(billingRequestNumber),
            AuthorUserId = authorUserId,
            Body = body,
            CreatedAtUtc = BaseDate.AddDays(billingRequestNumber).AddHours(4)
        };
    }

    private static AuditLog AuditLog(int number, int billingRequestNumber, Guid actorUserId, AuditActionType actionType, string message)
    {
        return new AuditLog
        {
            Id = Guid.Parse($"eeeeeeee-eeee-eeee-eeee-{number:000000000000}"),
            BillingRequestId = BillingRequestId(billingRequestNumber),
            EntityType = "BillingRequest",
            EntityId = BillingRequestId(billingRequestNumber),
            EntityNumber = $"BR-2026-{billingRequestNumber:0000}",
            ActorUserId = actorUserId,
            ActorDisplayName = Users.Single(x => x.Id == actorUserId).FullName,
            ActionType = actionType,
            Message = message,
            CreatedAtUtc = BaseDate.AddDays(billingRequestNumber).AddHours(2)
        };
    }

    private static Notification Notification(int number, Guid userId, string title, string message)
    {
        return new Notification
        {
            Id = Guid.Parse($"ffffffff-ffff-ffff-ffff-{number:000000000000}"),
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAtUtc = BaseDate.AddDays(number)
        };
    }

    private static Guid BillingRequestId(int number)
    {
        return Guid.Parse($"99999999-9999-9999-9999-{number:000000000000}");
    }

    private static string GetSuffix(string requestNumber)
    {
        var number = int.Parse(requestNumber[^4..]);
        return number.ToString("000000000000");
    }
}

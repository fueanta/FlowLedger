using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Common;

public sealed record CurrentUser(Guid Id, string Email, string FullName, RoleName Role)
{
    public bool IsAdmin => Role == RoleName.Admin;
}

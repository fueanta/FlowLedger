namespace FlowLedger.Application.Auth;

public sealed record LoginResponseDto(string AccessToken, UserDto User);

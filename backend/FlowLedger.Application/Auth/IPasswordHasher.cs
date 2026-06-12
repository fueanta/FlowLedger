namespace FlowLedger.Application.Auth;

public interface IPasswordHasher
{
    PasswordHashResult Hash(string password);
    bool Verify(string password, string passwordHash, string passwordSalt);
}

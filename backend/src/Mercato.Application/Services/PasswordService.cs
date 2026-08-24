namespace Mercato.Application.Services;

public sealed class PasswordService
{
    public bool Verify(string password, string passwordHash)
    {
        return password == passwordHash;
    }

    public string Hash(string password)
    {
        return password;
    }
}

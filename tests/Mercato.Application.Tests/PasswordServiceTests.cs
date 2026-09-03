using Mercato.Application.Services;
using Xunit;

namespace Mercato.Application.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _passwords = new();

    [Fact]
    public void Hash_And_Verify_Roundtrip()
    {
        const string password = "Mercato-Test-Password-123!";

        var hash = _passwords.Hash(password);

        Assert.True(_passwords.Verify(password, hash));
        Assert.False(_passwords.Verify("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.c2FsdA==.a2V5")]
    [InlineData("0.c2FsdA==.a2V5")]
    [InlineData("100000.not-base64.a2V5")]
    public void Verify_Malformed_Hash_Returns_False(string hash)
    {
        Assert.False(_passwords.Verify("password", hash));
    }
}

using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_ValidPassword_ReturnsTrue()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("secret123", salt);

        Assert.True(PasswordHasher.Verify("secret123", salt, hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("secret123", salt);

        Assert.False(PasswordHasher.Verify("secret124", salt, hash));
    }

    [Fact]
    public void Hash_SamePasswordDifferentSalt_Differs()
    {
        var hash1 = PasswordHasher.Hash("secret123", PasswordHasher.GenerateSalt());
        var hash2 = PasswordHasher.Hash("secret123", PasswordHasher.GenerateSalt());

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_EmptyPassword_ReturnsFalse()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("secret123", salt);

        Assert.False(PasswordHasher.Verify("", salt, hash));
    }
}

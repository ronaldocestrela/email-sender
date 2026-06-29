using System;
using Xunit;
using Identity.Domain.Aggregates;

namespace Identity.Domain.Tests;

/// <summary>
/// Testes unitários para a raiz do Agregado User.
/// </summary>
public class UserTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    [Fact]
    public void GivenValidData_WhenUserCreated_ShouldSucceedAndBeInitialized()
    {
        // Arrange
        var email = "user@acme.com";
        var passwordHash = "hashed_password";
        var role = "User";

        // Act
        var result = User.Create(email, passwordHash, TestTenantId, role);

        // Assert
        Assert.True(result.IsSuccess);
        var user = result.Value;
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(TestTenantId, user.TenantId);
        Assert.Equal(role, user.Role);
        Assert.False(user.IsMfaEnabled);
        Assert.Empty(user.MfaSecret);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    public void GivenInvalidEmail_WhenUserCreated_ShouldReturnFailure(string invalidEmail)
    {
        // Act
        var result = User.Create(invalidEmail, "hash", TestTenantId, "User");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidEmail", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidPassword_WhenUserCreated_ShouldReturnFailure(string invalidPasswordHash)
    {
        // Act
        var result = User.Create("user@acme.com", invalidPasswordHash, TestTenantId, "User");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidPassword", result.Error.Code);
    }

    [Fact]
    public void GivenEmptyTenantId_WhenUserCreated_ShouldReturnFailure()
    {
        // Act
        var result = User.Create("user@acme.com", "hash", Guid.Empty, "User");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidTenant", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    public void GivenInvalidRole_WhenUserCreated_ShouldReturnFailure(string invalidRole)
    {
        // Act
        var result = User.Create("user@acme.com", "hash", TestTenantId, invalidRole);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidRole", result.Error.Code);
    }

    [Fact]
    public void GivenUser_WhenRoleChangedToValidRole_ShouldUpdateRole()
    {
        // Arrange
        var user = User.Create("user@acme.com", "hash", TestTenantId, "User").Value;

        // Act
        var result = user.ChangeRole("Admin");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public void GivenUser_WhenRoleChangedToInvalidRole_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create("user@acme.com", "hash", TestTenantId, "User").Value;

        // Act
        var result = user.ChangeRole("InvalidRole");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidRole", result.Error.Code);
    }

    [Fact]
    public void GivenUser_WhenEnablingMfaWithValidSecret_ShouldUpdateStatusAndSecret()
    {
        // Arrange
        var user = User.Create("user@acme.com", "hash", TestTenantId, "User").Value;
        var secret = "JBSWY3DPEHPK3PXP"; // Exemplo de segredo Base32 para TOTP

        // Act
        var result = user.EnableMfa(secret);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.IsMfaEnabled);
        Assert.Equal(secret, user.MfaSecret);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenUser_WhenEnablingMfaWithInvalidSecret_ShouldReturnFailure(string invalidSecret)
    {
        // Arrange
        var user = User.Create("user@acme.com", "hash", TestTenantId, "User").Value;

        // Act
        var result = user.EnableMfa(invalidSecret);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidMfaSecret", result.Error.Code);
    }

    [Fact]
    public void GivenUserWithMfaEnabled_WhenDisabled_ShouldClearStatusAndSecret()
    {
        // Arrange
        var user = User.Create("user@acme.com", "hash", TestTenantId, "User").Value;
        user.EnableMfa("JBSWY3DPEHPK3PXP");

        // Act
        var result = user.DisableMfa();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(user.IsMfaEnabled);
        Assert.Empty(user.MfaSecret);
    }
}

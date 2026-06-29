using System;
using Xunit;
using EmailEngine.Domain.Aggregates;
using EmailEngine.Domain.Entities;
using EmailEngine.Domain.Enums;

namespace EmailEngine.Domain.Tests;

public class EmailEngineDomainTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    [Fact]
    public void GivenValidSmtpData_WhenCreatingSmtpSettings_ShouldSucceed()
    {
        // Act
        var result = EmailProviderSettings.CreateSmtp(
            TestTenantId,
            "smtp.mailgun.org",
            587,
            "user",
            "pass",
            true,
            "sender@acme.com",
            "ACME Sender");

        // Assert
        Assert.True(result.IsSuccess);
        var settings = result.Value;
        Assert.Equal(TestTenantId, settings.TenantId);
        Assert.Equal(EmailProviderType.Smtp, settings.Type);
        Assert.Equal("smtp.mailgun.org", settings.SmtpHost);
        Assert.Equal(587, settings.SmtpPort);
        Assert.Equal("user", settings.SmtpUsername);
        Assert.Equal("pass", settings.SmtpPassword);
        Assert.True(settings.SmtpEnableSsl);
        Assert.Equal("sender@acme.com", settings.SenderAddress);
        Assert.Equal("ACME Sender", settings.SenderName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidSmtpHost_WhenCreatingSmtpSettings_ShouldFail(string invalidHost)
    {
        // Act
        var result = EmailProviderSettings.CreateSmtp(
            TestTenantId,
            invalidHost,
            587,
            "user",
            "pass",
            true,
            "sender@acme.com",
            "ACME Sender");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailProviderSettings.InvalidSmtpHost", result.Error.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    public void GivenInvalidSmtpPort_WhenCreatingSmtpSettings_ShouldFail(int invalidPort)
    {
        // Act
        var result = EmailProviderSettings.CreateSmtp(
            TestTenantId,
            "smtp.mailgun.org",
            invalidPort,
            "user",
            "pass",
            true,
            "sender@acme.com",
            "ACME Sender");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailProviderSettings.InvalidSmtpPort", result.Error.Code);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("sender@")]
    [InlineData("@acme.com")]
    public void GivenInvalidSenderAddress_WhenCreatingSmtpSettings_ShouldFail(string invalidSender)
    {
        // Act
        var result = EmailProviderSettings.CreateSmtp(
            TestTenantId,
            "smtp.mailgun.org",
            587,
            "user",
            "pass",
            true,
            invalidSender,
            "ACME Sender");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailProviderSettings.InvalidSenderAddress", result.Error.Code);
    }

    [Fact]
    public void GivenValidSendGridData_WhenCreatingSendGridSettings_ShouldSucceed()
    {
        // Act
        var result = EmailProviderSettings.CreateSendGrid(
            TestTenantId,
            "SG.12345.abcde",
            "sender@acme.com",
            "ACME SendGrid");

        // Assert
        Assert.True(result.IsSuccess);
        var settings = result.Value;
        Assert.Equal(TestTenantId, settings.TenantId);
        Assert.Equal(EmailProviderType.SendGrid, settings.Type);
        Assert.Equal("SG.12345.abcde", settings.ApiKey);
        Assert.Equal("sender@acme.com", settings.SenderAddress);
        Assert.Equal("ACME SendGrid", settings.SenderName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidApiKey_WhenCreatingSendGridSettings_ShouldFail(string invalidApiKey)
    {
        // Act
        var result = EmailProviderSettings.CreateSendGrid(
            TestTenantId,
            invalidApiKey,
            "sender@acme.com",
            "ACME SendGrid");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailProviderSettings.InvalidApiKey", result.Error.Code);
    }

    [Fact]
    public void GivenValidData_WhenCreatingEmailHistory_ShouldSucceed()
    {
        // Act
        var result = EmailHistory.Create(
            TestTenantId,
            "recipient@client.com",
            "Welcome!",
            "<h1>Hello</h1>",
            "acme.com",
            true);

        // Assert
        Assert.True(result.IsSuccess);
        var history = result.Value;
        Assert.NotEqual(Guid.Empty, history.Id);
        Assert.Equal(TestTenantId, history.TenantId);
        Assert.Equal("recipient@client.com", history.To);
        Assert.Equal("Welcome!", history.Subject);
        Assert.Equal("<h1>Hello</h1>", history.Body);
        Assert.Equal("acme.com", history.SenderDomain);
        Assert.True(history.IsSuccess);
        Assert.Null(history.ErrorMessage);
        Assert.True(history.SentAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    public void GivenInvalidRecipient_WhenCreatingEmailHistory_ShouldFail(string invalidRecipient)
    {
        // Act
        var result = EmailHistory.Create(
            TestTenantId,
            invalidRecipient,
            "Subject",
            "Body",
            "acme.com",
            true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailHistory.InvalidRecipient", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidSubject_WhenCreatingEmailHistory_ShouldFail(string invalidSubject)
    {
        // Act
        var result = EmailHistory.Create(
            TestTenantId,
            "recipient@client.com",
            invalidSubject,
            "Body",
            "acme.com",
            true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailHistory.InvalidSubject", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidBody_WhenCreatingEmailHistory_ShouldFail(string invalidBody)
    {
        // Act
        var result = EmailHistory.Create(
            TestTenantId,
            "recipient@client.com",
            "Subject",
            invalidBody,
            "acme.com",
            true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailHistory.InvalidBody", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidSenderDomain_WhenCreatingEmailHistory_ShouldFail(string invalidDomain)
    {
        // Act
        var result = EmailHistory.Create(
            TestTenantId,
            "recipient@client.com",
            "Subject",
            "Body",
            invalidDomain,
            true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EmailHistory.InvalidSenderDomain", result.Error.Code);
    }
}

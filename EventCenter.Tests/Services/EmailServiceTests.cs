using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventCenter.Tests.Services;

/// <summary>
/// Test helper class that captures sent registration confirmations for testing.
/// Used by integration tests to verify email functionality without actually sending emails.
/// </summary>
public class TestEmailSender : IEmailSender
{
    public List<Registration> SentConfirmations { get; } = new();
    public List<EventCompany> SentInvitations { get; } = new();
    public List<(EventCompany Company, Event Event)> SentAdminBookingNotifications { get; } = new();
    public List<(EventCompany Company, Event Event, bool IsNonParticipation)> SentAdminCancellationNotifications { get; } = new();

    public Task SendRegistrationConfirmationAsync(Registration registration)
    {
        SentConfirmations.Add(registration);
        return Task.CompletedTask;
    }

    public Task SendCompanyInvitationAsync(EventCompany invitation, Event evt, string personalMessage, string invitationLink)
    {
        SentInvitations.Add(invitation);
        return Task.CompletedTask;
    }

    public Task SendAdminBookingNotificationAsync(EventCompany company, Event evt, List<ParticipantModel> participants)
    {
        SentAdminBookingNotifications.Add((company, evt));
        return Task.CompletedTask;
    }

    public Task SendAdminCancellationNotificationAsync(EventCompany company, Event evt, string? cancellationComment, bool isNonParticipation)
    {
        SentAdminCancellationNotifications.Add((company, evt, isNonParticipation));
        return Task.CompletedTask;
    }
}

public class EmailServiceTests
{
    [Fact]
    public void MailKitEmailSender_ImplementsIEmailSender()
    {
        // Arrange
        var settings = Options.Create(new SmtpSettings
        {
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            Username = "test@example.com",
            Password = "password",
            SenderName = "Test Sender",
            SenderEmail = "noreply@example.com"
        });
        var logger = new LoggerFactory().CreateLogger<MailKitEmailSender>();

        // Act
        var sender = new MailKitEmailSender(settings, logger);

        // Assert
        Assert.IsAssignableFrom<IEmailSender>(sender);
    }

    [Fact]
    public void TestEmailSender_CapturesRegistrations()
    {
        // Arrange
        var testSender = new TestEmailSender();
        var registration = new Registration
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        // Act
        testSender.SendRegistrationConfirmationAsync(registration).Wait();

        // Assert
        Assert.Single(testSender.SentConfirmations);
        Assert.Equal("john@example.com", testSender.SentConfirmations[0].Email);
    }

    [Fact]
    public void TestEmailSender_CapturesMultipleRegistrations()
    {
        // Arrange
        var testSender = new TestEmailSender();
        var registration1 = new Registration { Id = 1, Email = "test1@example.com" };
        var registration2 = new Registration { Id = 2, Email = "test2@example.com" };

        // Act
        testSender.SendRegistrationConfirmationAsync(registration1).Wait();
        testSender.SendRegistrationConfirmationAsync(registration2).Wait();

        // Assert
        Assert.Equal(2, testSender.SentConfirmations.Count);
        Assert.Contains(testSender.SentConfirmations, r => r.Email == "test1@example.com");
        Assert.Contains(testSender.SentConfirmations, r => r.Email == "test2@example.com");
    }
}

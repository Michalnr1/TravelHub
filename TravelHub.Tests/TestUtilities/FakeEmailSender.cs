using System.Collections.Generic;
using System.Threading.Tasks;
using TravelHub.Domain.Interfaces;

namespace TravelHub.Tests.TestUtilities;

public class FakeEmailSender : IEmailSender
{
    public List<(string Email, string Subject, string Html)> Sent { get; } = new();

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Sent.Add((email, subject, htmlMessage));
        return Task.CompletedTask;
    }
}

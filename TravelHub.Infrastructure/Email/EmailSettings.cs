namespace TravelHub.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "TravelHub";
    public string SenderEmail { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

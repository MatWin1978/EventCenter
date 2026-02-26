namespace EventCenter.Web.Infrastructure.Email;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Veranstaltungscenter";
    public string SenderEmail { get; set; } = string.Empty;
}

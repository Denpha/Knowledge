namespace KMS.Infrastructure.Services.Email;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = false;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = "noreply@kms.rmuti.ac.th";
    public string SenderName { get; set; } = "KMS - ระบบจัดการองค์ความรู้";
    public bool Enabled { get; set; } = true;
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}

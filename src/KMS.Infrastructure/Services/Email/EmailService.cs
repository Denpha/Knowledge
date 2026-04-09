using KMS.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KMS.Infrastructure.Services.Email;

public class EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Email disabled. Would send to {To}: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort,
                _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                cancellationToken);

            if (!string.IsNullOrEmpty(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
    }

    public async Task SendPasswordResetAsync(string to, string username, string resetLink, CancellationToken cancellationToken = default)
    {
        var html = $"""
            <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
              <h2 style="color:#1d4ed8">รีเซ็ตรหัสผ่าน — KMS</h2>
              <p>สวัสดีคุณ <strong>{username}</strong></p>
              <p>คลิกปุ่มด้านล่างเพื่อรีเซ็ตรหัสผ่านของคุณ ลิงก์นี้จะหมดอายุใน 1 ชั่วโมง</p>
              <a href="{resetLink}" style="display:inline-block;background:#1d4ed8;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;margin:16px 0">รีเซ็ตรหัสผ่าน</a>
              <p style="color:#6b7280;font-size:14px">หากคุณไม่ได้ขอรีเซ็ตรหัสผ่าน กรุณาเพิกเฉยต่ออีเมลนี้</p>
            </div>
            """;
        await SendAsync(to, "รีเซ็ตรหัสผ่าน — KMS", html, cancellationToken);
    }

    public async Task SendArticlePublishedAsync(string to, string username, string articleTitle, string articleUrl, CancellationToken cancellationToken = default)
    {
        var html = $"""
            <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
              <h2 style="color:#059669">บทความได้รับการเผยแพร่แล้ว</h2>
              <p>สวัสดีคุณ <strong>{username}</strong></p>
              <p>บทความ <strong>"{articleTitle}"</strong> ของคุณได้รับการเผยแพร่เรียบร้อยแล้ว</p>
              <a href="{articleUrl}" style="display:inline-block;background:#059669;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;margin:16px 0">ดูบทความ</a>
            </div>
            """;
        await SendAsync(to, $"บทความ \"{articleTitle}\" ได้รับการเผยแพร่แล้ว", html, cancellationToken);
    }

    public async Task SendReviewRequestedAsync(string to, string reviewerUsername, string articleTitle, string articleUrl, CancellationToken cancellationToken = default)
    {
        var html = $"""
            <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
              <h2 style="color:#d97706">มีบทความรอการตรวจสอบ</h2>
              <p>สวัสดีคุณ <strong>{reviewerUsername}</strong></p>
              <p>บทความ <strong>"{articleTitle}"</strong> รอการตรวจสอบจากคุณ</p>
              <a href="{articleUrl}" style="display:inline-block;background:#d97706;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;margin:16px 0">ตรวจสอบบทความ</a>
            </div>
            """;
        await SendAsync(to, $"บทความ \"{articleTitle}\" รอการตรวจสอบ", html, cancellationToken);
    }

    public async Task SendWelcomeAsync(string to, string username, CancellationToken cancellationToken = default)
    {
        var html = $"""
            <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
              <h2 style="color:#1d4ed8">ยินดีต้อนรับสู่ KMS</h2>
              <p>สวัสดีคุณ <strong>{username}</strong></p>
              <p>บัญชีของคุณได้รับการสร้างเรียบร้อยแล้ว ยินดีต้อนรับสู่ระบบจัดการองค์ความรู้ มหาวิทยาลัยเทคโนโลยีราชมงคลอีสาน วิทยาเขตสกลนคร</p>
              <a href="{_options.WebBaseUrl}" style="display:inline-block;background:#1d4ed8;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;margin:16px 0">เข้าสู่ระบบ</a>
            </div>
            """;
        await SendAsync(to, "ยินดีต้อนรับสู่ KMS", html, cancellationToken);
    }
}

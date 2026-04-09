namespace KMS.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string to, string username, string resetLink, CancellationToken cancellationToken = default);
    Task SendArticlePublishedAsync(string to, string username, string articleTitle, string articleUrl, CancellationToken cancellationToken = default);
    Task SendReviewRequestedAsync(string to, string reviewerUsername, string articleTitle, string articleUrl, CancellationToken cancellationToken = default);
    Task SendWelcomeAsync(string to, string username, CancellationToken cancellationToken = default);
}

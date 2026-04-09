namespace KMS.Application.Interfaces;

public interface IAlertChannelService
{
    Task SendLineWebhookAlertAsync(
        string metricName,
        int currentCount,
        int threshold,
        int windowSeconds,
        CancellationToken cancellationToken = default);
}

using BluewardSentinel.Core.Models;

namespace BluewardSentinel.Core.Interfaces;

public interface IWebhookService
{
    Task<List<WebhookConfig>> GetWebhooksAsync(CancellationToken cancellationToken = default);
    Task<WebhookConfig> SaveWebhookAsync(WebhookConfig config, CancellationToken cancellationToken = default);
    Task<bool> DeleteWebhookAsync(string id, CancellationToken cancellationToken = default);
    Task DispatchIncidentAlertAsync(IncidentCluster incident, string eventType, CancellationToken cancellationToken = default);
}

namespace BluewardSentinel.Core.Models;

public class WebhookConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Slack / Discord Webhook";
    public string Url { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string TargetSeverity { get; set; } = "Critical"; // Critical, All
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

namespace BluewardSentinel.Core.Models;

public class SentinelProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApiKey { get; set; } = "sec_" + Guid.NewGuid().ToString("N");
    public string Environment { get; set; } = "Production";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

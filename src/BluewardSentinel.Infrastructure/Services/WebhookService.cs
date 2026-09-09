using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Core.Models;

namespace BluewardSentinel.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, WebhookConfig> _webhooks = new();
    private readonly string _storageFilePath;
    private readonly object _lockObj = new();

    public WebhookService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BluewardSentinel");
        Directory.CreateDirectory(appData);
        _storageFilePath = Path.Combine(appData, "webhooks.json");

        LoadFromDisk();
    }

    public Task<List<WebhookConfig>> GetWebhooksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_webhooks.Values.OrderBy(w => w.CreatedAtUtc).ToList());
    }

    public Task<WebhookConfig> SaveWebhookAsync(WebhookConfig config, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            if (string.IsNullOrWhiteSpace(config.Id))
            {
                config.Id = Guid.NewGuid().ToString("N");
            }
            _webhooks[config.Id] = config;
            SaveToDisk();
        }
        return Task.FromResult(config);
    }

    public Task<bool> DeleteWebhookAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            var removed = _webhooks.TryRemove(id, out _);
            if (removed) SaveToDisk();
            return Task.FromResult(removed);
        }
    }

    public async Task DispatchIncidentAlertAsync(IncidentCluster incident, string eventType, CancellationToken cancellationToken = default)
    {
        var targets = _webhooks.Values.Where(w => w.IsEnabled).ToList();
        if (targets.Count == 0) return;

        var payload = new
        {
            text = $"🚨 [Blueward Sentinel 경보] {eventType.ToUpperInvariant()}: {incident.Title}",
            attachments = new[]
            {
                new
                {
                    title = $"{incident.Title} ({incident.Severity})",
                    color = incident.Severity == "Critical" ? "#f43f5e" : "#eab308",
                    fields = new[]
                    {
                        new { title = "프로젝트", value = incident.ProjectName, @short = true },
                        new { title = "심각도", value = incident.Severity, @short = true },
                        new { title = "발생 횟수", value = $"{incident.OccurrenceCount}회", @short = true },
                        new { title = "태그", value = string.Join(", ", incident.Tags.Select(t => $"#{t}")), @short = true },
                        new { title = "오류 요약", value = incident.SampleMessage ?? "상세 메시지 없음", @short = false }
                    },
                    footer = "Blueward Sentinel AI Agent Real-time Alert",
                    ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            }
        };

        foreach (var webhook in targets)
        {
            if (string.IsNullOrWhiteSpace(webhook.Url)) continue;
            if (webhook.TargetSeverity == "Critical" && incident.Severity != "Critical") continue;

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(webhook.Url, payload, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Sentinel Webhook] Alert dispatched to {webhook.Name} ({webhook.Url})");
                }
                else
                {
                    Console.WriteLine($"[Sentinel Webhook] Alert failed ({response.StatusCode}) for {webhook.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sentinel Webhook] Exception for {webhook.Url}: {ex.Message}");
            }
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_webhooks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sentinel Webhook] Failed to persist webhooks to disk: {ex.Message}");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(_storageFilePath))
            {
                var json = File.ReadAllText(_storageFilePath);
                var loaded = JsonSerializer.Deserialize<ConcurrentDictionary<string, WebhookConfig>>(json);
                if (loaded != null)
                {
                    foreach (var kv in loaded) _webhooks[kv.Key] = kv.Value;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sentinel Webhook] Failed to load webhooks from disk: {ex.Message}");
        }
    }
}

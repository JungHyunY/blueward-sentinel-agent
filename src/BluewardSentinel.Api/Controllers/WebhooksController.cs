using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWebhooks(CancellationToken cancellationToken)
    {
        var list = await _webhookService.GetWebhooksAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> SaveWebhook([FromBody] WebhookConfig config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.Url))
        {
            return BadRequest(new { message = "Webhook URL을 입력해주세요." });
        }
        var saved = await _webhookService.SaveWebhookAsync(config, cancellationToken);
        return Ok(saved);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(string id, CancellationToken cancellationToken)
    {
        var success = await _webhookService.DeleteWebhookAsync(id, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestWebhook([FromBody] WebhookConfig config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.Url))
        {
            return BadRequest(new { message = "Webhook URL이 비어있습니다." });
        }

        var sampleIncident = new IncidentCluster
        {
            Id = "test-incident-alert",
            ProjectName = "Yoonikon Sentinel Test",
            Title = "AIOps 모바일 웹훅 연동 테스트 알림",
            Severity = "Critical",
            OccurrenceCount = 1,
            SampleMessage = "모바일 알림 채널(Slack/Discord/Telegram) 정상 수신 검증 성공",
            Tags = new List<string> { "TEST", "AIOPS", "MOBILE_ALERT" }
        };

        await _webhookService.DispatchIncidentAlertAsync(sampleIncident, "test_ping", cancellationToken);
        return Ok(new { success = true, message = "테스트 웹훅 알림이 성공적으로 전송되었습니다." });
    }
}

using Microsoft.AspNetCore.Mvc;
using BluewardSentinel.Infrastructure.Services;
using System.Text.Json;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    [HttpGet("gemini")]
    public IActionResult GetGeminiConfig()
    {
        var apiKey = GeminiAiDiagnosticEngine.RuntimeApiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoCodeRagSolution");
        var keyTxt = Path.Combine(appData, "gemini_key.txt");

        if (string.IsNullOrWhiteSpace(apiKey) && System.IO.File.Exists(keyTxt))
        {
            apiKey = System.IO.File.ReadAllText(keyTxt).Trim();
        }

        var hasKey = !string.IsNullOrWhiteSpace(apiKey);
        var masked = hasKey && apiKey.Length > 8
            ? $"{apiKey[..4]}...{apiKey[^4..]}"
            : (hasKey ? "****" : "");

        return Ok(new
        {
            hasKey,
            maskedKey = masked,
            defaultModel = "gemini-2.5-flash"
        });
    }

    [HttpPost("gemini")]
    public IActionResult UpdateGeminiConfig([FromBody] GeminiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(new { message = "API 키를 입력해주세요." });
        }

        var key = request.ApiKey.Trim();
        GeminiAiDiagnosticEngine.RuntimeApiKey = key;

        // Persist to local user AppData
        try
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoCodeRagSolution");
            Directory.CreateDirectory(appData);
            System.IO.File.WriteAllText(Path.Combine(appData, "gemini_key.txt"), key);
        }
        catch {}

        return Ok(new
        {
            success = true,
            message = "Gemini API 키가 성공적으로 저장되었습니다.",
            maskedKey = key.Length > 8 ? $"{key[..4]}...{key[^4..]}" : "****"
        });
    }
}

public class GeminiKeyRequest
{
    public string ApiKey { get; set; } = string.Empty;
}

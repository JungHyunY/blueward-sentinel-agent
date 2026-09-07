using BluewardSentinel.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BluewardSentinel.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ISentinelRepository _repository;

    public ProjectsController(ISentinelRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var list = await _repository.GetProjectsAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("프로젝트 이름을 입력하세요.");
        var p = await _repository.CreateProjectAsync(req.Name, req.Description ?? "", req.Environment ?? "Production");
        return Ok(p);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id)
    {
        await _repository.DeleteProjectAsync(id);
        return Ok(new { success = true });
    }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Environment { get; set; }
}

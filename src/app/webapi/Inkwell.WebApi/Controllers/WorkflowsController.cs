using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// Workflow 管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class WorkflowsController(WorkflowRegistry workflowRegistry) : ControllerBase
{
    /// <summary>
    /// 获取所有 Workflow 列表
    /// </summary>
    /// <returns>Workflow 列表</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        IReadOnlyList<WorkflowRegistration> workflows = workflowRegistry.GetAll();

        return this.Ok(workflows.Select(w => new
        {
            w.Id,
            w.Name,
            w.Description
        }));
    }

    /// <summary>
    /// 获取 Workflow 详情
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Workflow 详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        WorkflowRegistration? registration = workflowRegistry.GetById(id);

        if (registration is null)
        {
            return this.NotFound();
        }

        return this.Ok(new
        {
            registration.Id,
            registration.Name,
            registration.Description
        });
    }

    /// <summary>
    /// 获取 Workflow 拓扑图（Mermaid 格式）
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Mermaid 格式的拓扑图字符串</returns>
    [HttpGet("{id}/topology")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetTopology(string id)
    {
        WorkflowRegistration? registration = workflowRegistry.GetById(id);

        if (registration is null)
        {
            return this.NotFound();
        }

        string mermaid = registration.Workflow.ToMermaidString();

        return this.Ok(new
        {
            registration.Id,
            registration.Name,
            format = "mermaid",
            topology = mermaid
        });
    }
}

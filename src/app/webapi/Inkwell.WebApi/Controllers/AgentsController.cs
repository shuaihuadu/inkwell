using Inkwell.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// Agent 管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class AgentsController(AgentRegistry agentRegistry) : ControllerBase
{
    /// <summary>
    /// 获取所有已注册的 Agent
    /// </summary>
    /// <returns>Agent 列表</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        IReadOnlyList<AgentRegistration> agents = agentRegistry.GetAll();

        var result = agents.Select(a => new
        {
            a.Id,
            a.Name,
            a.Description,
            a.AguiRoute
        });

        return this.Ok(result);
    }

    /// <summary>
    /// 根据 ID 获取 Agent 信息
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <returns>Agent 信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        AgentRegistration? agent = agentRegistry.GetById(id);

        if (agent is null)
        {
            return this.NotFound();
        }

        return this.Ok(new
        {
            agent.Id,
            agent.Name,
            agent.Description,
            agent.AguiRoute
        });
    }
}

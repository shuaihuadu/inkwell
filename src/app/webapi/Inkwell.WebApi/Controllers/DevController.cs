using Inkwell.Agents;
using Inkwell.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 开发调试控制器（仅开发环境可用）
/// 提供 Agent/Workflow 的调试信息查看
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public sealed class DevController(
    AgentRegistry agentRegistry,
    WorkflowRegistry workflowRegistry,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// 获取系统诊断信息
    /// </summary>
    /// <returns>系统诊断数据</returns>
    [HttpGet("diagnostics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetDiagnostics()
    {
        if (!environment.IsDevelopment())
        {
            return this.Forbid();
        }

        return this.Ok(new
        {
            environment = environment.EnvironmentName,
            agents = agentRegistry.GetAll().Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.AguiRoute,
                agentType = a.Agent.GetType().Name
            }),
            workflows = workflowRegistry.GetAll().Select(w => new
            {
                w.Id,
                w.Name,
                w.Description
            }),
            configuration = new
            {
                azureOpenAI = new
                {
                    primaryDeployment = configuration["AzureOpenAI:Primary:DeploymentName"],
                    secondaryDeployment = configuration["AzureOpenAI:Secondary:DeploymentName"],
                    embeddingDeployment = configuration["AzureOpenAI:Embedding:DeploymentName"],
                    // 不暴露 Endpoint 和 ApiKey
                },
                auth = new
                {
                    enabled = configuration.GetValue<bool>("Auth:Enabled")
                },
                cors = configuration.GetSection("Cors:Origins").Get<string[]>()
            }
        });
    }

    /// <summary>
    /// 获取指定 Agent 的详细调试信息
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <returns>Agent 调试信息</returns>
    [HttpGet("agents/{agentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAgentDebugInfo(string agentId)
    {
        if (!environment.IsDevelopment())
        {
            return this.Forbid();
        }

        AgentRegistration? registration = agentRegistry.GetById(agentId);
        if (registration is null)
        {
            return this.NotFound();
        }

        return this.Ok(new
        {
            registration.Id,
            registration.Name,
            registration.Description,
            registration.AguiRoute,
            agentType = registration.Agent.GetType().FullName,
            // Agent 内部元数据（Type 信息）
            capabilities = new
            {
                hasTools = registration.Description.Contains("工具") || registration.Description.Contains("Tool"),
                hasStructuredOutput = registration.Description.Contains("结构化"),
                hasMiddleware = registration.Description.Contains("护栏") || registration.Description.Contains("审计"),
                isDeclarative = registration.Description.StartsWith("[声明式]"),
                isWorkflowAgent = registration.Id.StartsWith("workflow-")
            }
        });
    }

    /// <summary>
    /// 获取所有 Workflow 的拓扑图汇总
    /// </summary>
    /// <returns>所有 Workflow 的 Mermaid 拓扑</returns>
    [HttpGet("workflows/topologies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllTopologies()
    {
        if (!environment.IsDevelopment())
        {
            return this.Forbid();
        }

        IReadOnlyList<WorkflowRegistration> workflows = workflowRegistry.GetAll();

        return this.Ok(workflows.Select(w =>
        {
            string mermaid;
            try
            {
                mermaid = w.Workflow.ToMermaidString();
            }
            catch
            {
                mermaid = "// Error generating topology";
            }

            return new
            {
                w.Id,
                w.Name,
                mermaid
            };
        }));
    }
}

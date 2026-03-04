using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.Application.Agents.Delegates;
using Library.Infrastructure.MessageBus;
using Library.Domain.Abstractions;

namespace Service.Controllers.Agents;

[ApiController]
[Route("agents/ledger")]
public class LedgerController : ControllerBase
{
    private readonly LedgerAgent _agent;
    private readonly ITaskChannel _channel;

    public LedgerController(LedgerAgent agent, ITaskChannel channel)
    {
        _agent = agent;
        _channel = channel;
    }

    [HttpPost("pipeline-check")]
    public async Task<IActionResult> RunPipelineCheck([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.DailyPipelineCheck();

        if (publish)
        {
            await _channel.PublishAsync("daily_pipeline_check", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }

    [HttpPost("retention-audit")]
    public async Task<IActionResult> RunRetentionAudit([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.WeeklyRetentionAudit();

        if (publish)
        {
            await _channel.PublishAsync("weekly_retention_audit", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }

    [HttpPost("health-brief")]
    public async Task<IActionResult> RunHealthBrief([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.WeeklyHealthBrief();

        if (publish)
        {
            await _channel.PublishAsync("weekly_health_brief", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }
}

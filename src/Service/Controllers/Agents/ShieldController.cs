using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.Application.Agents.Delegates;
using Library.Infrastructure.MessageBus;
using Library.Domain.Abstractions;

namespace Service.Controllers.Agents;

[ApiController]
[Route("agents/shield")]
public class ShieldController : ControllerBase
{
    private readonly ShieldAgent _agent;
    private readonly ITaskChannel _channel;

    public ShieldController(ShieldAgent agent, ITaskChannel channel)
    {
        _agent = agent;
        _channel = channel;
    }

    [HttpPost("perimeter-audit")]
    public async Task<IActionResult> RunPerimeterAudit([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.DailyPerimeterAudit();
        
        if (publish)
        {
            await _channel.PublishAsync("daily_perimeter_audit", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }

    [HttpPost("policy-drift")]
    public async Task<IActionResult> RunPolicyDrift([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.WeeklyPolicyDrift();
        
        if (publish)
        {
            await _channel.PublishAsync("weekly_policy_drift", result, ct);
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

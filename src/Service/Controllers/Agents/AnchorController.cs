using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.Application.Agents.Delegates;
using Library.Infrastructure.MessageBus;
using Library.Domain.Abstractions;

namespace Service.Controllers.Agents;

[ApiController]
[Route("agents/anchor")]
public class AnchorController : ControllerBase
{
    private readonly AnchorAgent _agent;
    private readonly ITaskChannel _channel;

    public AnchorController(AnchorAgent agent, ITaskChannel channel)
    {
        _agent = agent;
        _channel = channel;
    }

    [HttpPost("backup-check")]
    public async Task<IActionResult> RunBackupCheck([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.DailyBackupCheck();

        if (publish)
        {
            await _channel.PublishAsync("daily_backup_check", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }

    [HttpPost("service-stability")]
    public async Task<IActionResult> RunServiceStability([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.DailyServiceStability();

        if (publish)
        {
            await _channel.PublishAsync("daily_service_stability", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }

    [HttpPost("drift-check")]
    public async Task<IActionResult> RunDriftCheck([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.WeeklyDriftCheck();

        if (publish)
        {
            await _channel.PublishAsync("weekly_drift_check", result, ct);
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

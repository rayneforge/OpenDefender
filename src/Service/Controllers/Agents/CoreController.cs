using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.Application.Agents.Delegates;
using Library.Infrastructure.MessageBus;
using Library.Domain.Abstractions;

namespace Service.Controllers.Agents;

[ApiController]
[Route("agents/core")]
public class CoreController : ControllerBase
{
    private readonly CoreAgent _agent;
    private readonly ITaskChannel _channel;

    public CoreController(CoreAgent agent, ITaskChannel channel)
    {
        _agent = agent;
        _channel = channel;
    }

    [HttpPost("resource-check")]
    public async Task<IActionResult> RunResourceCheck([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.DailyResourceCheck();

        if (publish)
        {
            await _channel.PublishAsync("daily_resource_check", result, ct);
        }

        return Ok(new { Result = result, Published = publish });
    }

    [HttpPost("firmware-review")]
    public async Task<IActionResult> RunFirmwareReview([FromQuery] bool publish = false, CancellationToken ct = default)
    {
        var result = await _agent.WeeklyFirmwareReview();

        if (publish)
        {
            await _channel.PublishAsync("weekly_firmware_review", result, ct);
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

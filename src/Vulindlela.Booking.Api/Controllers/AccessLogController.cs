using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Data;

namespace Vulindlela.Booking.Api.Controllers;

[ApiController, Route("api/access-log"), Authorize]
public class AccessLogController : ControllerBase
{
    private readonly IAccessLogRepository _logs;
    public AccessLogController(IAccessLogRepository logs) => _logs = logs;

    private Guid CallerId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok((await _logs.ForSubjectAsync(CallerId, ct)).Select(l => new
        {
            reader = l.ReaderCitizenId,
            role = l.ReaderRole,
            at = l.TimestampUtc,
            reason = l.Reason,
            resource = l.Resource
        }));
}
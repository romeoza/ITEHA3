using Microsoft.AspNetCore.Mvc;
using Vulindlela.Clearance.Api.Business;
using Vulindlela.Clearance.Api.Data;
using Vulindlela.Shared;

namespace Vulindlela.Clearance.Api.Controllers;

[ApiController, Route("api/clearance")]
public class ClearanceController : ControllerBase
{
    private readonly IClearanceService _service;
    private readonly ISubmissionRepository _repo;
    public ClearanceController(IClearanceService service, ISubmissionRepository repo)
    { _service = service; _repo = repo; }

    [HttpPost("notify-appointment")]
    public IActionResult Notify([FromBody] AppointmentCreatedNotification notification) =>
        Accepted(new { notification.AppointmentId, message = "Recorded; awaiting document submission." });

    [HttpPost("submissions")]
    public async Task<IActionResult> Submit([FromBody] ClearanceSubmissionRequest request, CancellationToken ct)
    {
        var dto = await _service.SubmitAsync(request.CitizenId, request, ct);
        return AcceptedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("submissions/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var sub = await _repo.GetAsync(id, ct);
        if (sub is null) return NotFound();
        var missing = string.IsNullOrEmpty(sub.MissingDocumentsCsv)
            ? Array.Empty<string>() : sub.MissingDocumentsCsv.Split(';');
        return Ok(new ClearanceSubmissionDto(sub.Id, sub.AppointmentId, sub.Status, missing, sub.UpdatedAtUtc));
    }

    [HttpGet("status/{appointmentId:guid}")]
    public async Task<IActionResult> Status(Guid appointmentId, CancellationToken ct)
    {
        var dto = await _service.StatusForAppointmentAsync(appointmentId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
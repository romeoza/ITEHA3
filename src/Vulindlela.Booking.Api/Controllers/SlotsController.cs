using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Shared;

namespace Vulindlela.Booking.Api.Controllers;

[ApiController, Route("api")]
public class SlotsController : ControllerBase
{
    private readonly IBranchRepository _branches;
    private readonly ISlotRepository _slots;
    public SlotsController(IBranchRepository branches, ISlotRepository slots)
    { _branches = branches; _slots = slots; }

    [HttpGet("branches")]
    public async Task<IActionResult> Branches(CancellationToken ct) =>
        Ok((await _branches.ListAsync(ct)).Select(b => new BranchDto(b.Id, b.Code, b.Name)));

    [HttpGet("branches/{id:guid}/slots")]
    public async Task<IActionResult> Slots(Guid id, CancellationToken ct) =>
        Ok((await _slots.ListForBranchAsync(id, ct)).Select(s => new SlotDto(s.Id, s.BranchId, s.StartsAtUtc, false)));
}
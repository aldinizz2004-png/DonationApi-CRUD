using DonationApi.DTOs;
using DonationApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DonationApi.Controllers;

[ApiController]
[Route("api/donations")]
[Produces("application/json")]
public class DonationsController : ControllerBase
{
    private readonly IDonationService _service;

    public DonationsController(IDonationService service)
    {
        _service = service;
    }

    /// GET api/donations?pageSize=20&sort=newest&from=2026-01-01&to=2026-12-31&minAmount=50&includeTotal=true
    /// Next page: GET api/donations?pageSize=20&cursor={nextCursor}
    [HttpGet]
    [ProducesResponseType(typeof(CursorPage<DonationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CursorPage<DonationResponse>>> GetAll(
        [FromQuery] DonationPageQuery query, CancellationToken ct)
    {
        // Invalid pageSize / sort / cursor / date range -> 400 automatically ([ApiController])
        return Ok(await _service.GetPageAsync(query, ct));
    }

    /// GET api/donations/5
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonationResponse>> GetById(int id)
    {
        if (id <= 0) return InvalidId();

        var donation = await _service.GetByIdAsync(id);
        return donation is null ? NotFoundDonation(id) : Ok(donation);
    }

    /// POST api/donations
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DonationResponse>> Create(CreateDonationRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// PUT api/donations/5
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonationResponse>> Update(int id, UpdateDonationRequest request)
    {
        if (id <= 0) return InvalidId();

        var updated = await _service.UpdateAsync(id, request);
        return updated is null ? NotFoundDonation(id) : Ok(updated);
    }

    /// DELETE api/donations/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0) return InvalidId();

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFoundDonation(id);
    }

    // ---------- helpers ----------
    private ActionResult InvalidId()
    {
        ModelState.AddModelError("id", "Id must be a positive integer.");
        return ValidationProblem(ModelState);
    }

    private ActionResult NotFoundDonation(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Donation not found",
                detail: $"No donation exists with id {id}.");
}
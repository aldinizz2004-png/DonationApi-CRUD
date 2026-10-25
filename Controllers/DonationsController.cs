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

    /// GET api/donations
    [HttpGet]
    [ProducesResponseType(typeof(List<DonationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DonationResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
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
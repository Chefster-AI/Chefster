using Chefster.Models;
using Chefster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chefster.Controllers;

[Authorize]
[Route("api/consideration")]
[ApiController]
public class ConsiderationController(ConsiderationsService considerationsService) : ControllerBase
{
    private readonly ConsiderationsService _considerationsService = considerationsService;

    [HttpGet("{FamilyId}")]
    public ActionResult<ConsiderationsModel> GetByFamilyId(string FamilyId)
    {
        var consideration = _considerationsService.GetAllFamilyConsiderations(FamilyId);
        if (consideration == null)
        {
            return BadRequest($"no considerations for family with Id {FamilyId}");
        }

        return Ok(consideration.Data);
    }

        [HttpGet("api/id/{ConsiderationId}")]
    public ActionResult<ConsiderationsModel> GetConsiderationById(string ConsiderationId)
    {
        var consideration = _considerationsService.GetConsiderationById(ConsiderationId);
        if (consideration.Success == false)
        {
            return BadRequest($"no considerations for family with Id {ConsiderationId}");
        }
 
        return Ok(consideration.Data);
    }

    /// <summary>
    /// Gets Family Notes from the last 7 days
    /// </summary>
    [HttpGet("/api/considerations/{FamilyId}")]
    public ActionResult<ConsiderationsModel> GetPreviousWeekNotes(string FamilyId)
    {
        var note = _considerationsService.GetWeeklyConsiderations(FamilyId);
        if (note == null)
        {
            return BadRequest(
                $"not considerations for family with Id {FamilyId} for the last 7 days"
            );
        }

        return Ok(note.Data);
    }

    [HttpPost]
    public ActionResult CreateConsiderations([FromBody] ConsiderationsCreateDto consideration)
    {
        var created = _considerationsService.CreateConsideration(consideration);
        if (!created.Success)
        {
            return BadRequest($"Error: {created.Error}");
        }

        return Ok("Created consideration successfully");
    }

    [HttpDelete("{ConsiderationId}")]
    public ActionResult DeleteConsideration(string ConsiderationId)
    {
        var deleted = _considerationsService.DeleteConsideration(ConsiderationId);
        if (!deleted.Success)
        {
            return BadRequest($"Error: {deleted.Error}");
        }

        return Ok("Successfully deleted consideration");
    }

    [HttpPut("{ConsiderationId}")]
    public ActionResult<ConsiderationsModel> UpdateConsideration(ConsiderationsUpdateDto consideration, string ConsiderationId)
    {
        var updated = _considerationsService.UpdateConsideration(ConsiderationId, consideration);

        if (!updated.Success)
        {
            return BadRequest($"Error: {updated.Error}");
        }

        return Ok(updated.Data);
    }
}

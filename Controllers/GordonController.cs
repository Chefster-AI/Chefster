using Chefster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Chefster.Controllers;

[Authorize]
[Route("api/gordon")]
[ApiController]
public class GordonController(GordonService gordonService, JobService jobService) : ControllerBase
{
    private readonly GordonService _gordonService = gordonService;
    private readonly JobService _jobService = jobService;

    /// <summary>
    /// FOR TESTING ONLY. Send consideration in body
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreatedGordonResponse([FromBody] string request)
    {
        if (request.IsNullOrEmpty())
        {
            return BadRequest("Request must not be empty!");
        }
        var response = await _gordonService.GetMessageResponse(request);

        if (response.Success != true)
        {
            return BadRequest($"Failed to get Gordon response. Error: {response.Error}");
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// FOR TESTING ONLY. Create a response based on a families considerations and send an email.
    /// </summary>
    [HttpPost("{familyId}")]
    public async Task CreatedGordonResponseForFamily(string familyId)
    {
        //    var formatted = _jobService.BuildGordonRequest(familyId);

        //     if (formatted == null)
        //     {
        //         return BadRequest("Formatted string was null! Does the family have considerations?");
        //     }
        //     var response = await _gordonService.GetMessageResponse(formatted);

        //     if (response.Success != true)
        //     {
        //         return BadRequest($"Failed to get Gordon response. Error: {response.Error}");
        //     }

        await _jobService.GenerateAndSendRecipes(familyId);

        //return Ok();
    }
}

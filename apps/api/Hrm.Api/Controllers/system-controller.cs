using Hrm.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType<ApiResponse<SystemInfoDto>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SystemInfoDto>> GetInfo()
    {
        var response = ApiResponse.Ok(
            new SystemInfoDto("Saigon Retail Management System API", "v1", timeProvider.GetUtcNow()),
            new ApiMeta(TraceId: HttpContext.TraceIdentifier));

        return Ok(response);
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[ApiVersion(ApiVersions.V1)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected const string BaseRoute = "api/v{version:apiVersion}";

    protected string RequestedApiVersion => HttpContext.RequestedApiVersion?.ToString() ?? ApiVersions.V1;
}

public static class ApiVersions
{
    public const string V1 = "1.0";
}

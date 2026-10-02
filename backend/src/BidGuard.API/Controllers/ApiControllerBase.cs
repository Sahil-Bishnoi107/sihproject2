using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

/// <summary>Controllers only translate HTTP to mediator messages; all behaviour lives in Application handlers.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}

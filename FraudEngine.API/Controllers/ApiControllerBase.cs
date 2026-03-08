using FraudEngine.API.Auth;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Base API Controller that applies common routing and versioning attributes.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
}

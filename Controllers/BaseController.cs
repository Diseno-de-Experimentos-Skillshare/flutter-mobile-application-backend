using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace SkillShareBackend.Controllers;

/// <summary>
/// Base controller providing shared functionality for all API controllers.
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Gets the authenticated user's ID from the current security context.
    /// Returns 0 if the user is not authenticated or the ID cannot be found.
    /// </summary>
    protected int GetUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return 0;
        }

        try
        {
            // Try common claim types for user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("uid")?.Value
                              ?? User.FindFirst("userId")?.Value
                              ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                // Fallback to Name property if it contains the ID
                userIdClaim = User.Identity.Name;
            }

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Helper to return an unauthorized response with a consistent message.
    /// </summary>
    protected ActionResult UserUnauthorized()
    {
        return Unauthorized(new { message = "User is not authenticated or session has expired." });
    }

    /// <summary>
    /// Helper to return a forbidden response when a user lacks permissions.
    /// </summary>
    protected ActionResult UserForbidden()
    {
        return Forbid();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillShareBackend.Services;

namespace SkillShareBackend.Controllers;

/// <summary>
/// Endpoints de recomendación de grupos.
/// EC-02 (onboarding) y EC-05 (home "Recomendado para ti").
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class RecommendationController : BaseController
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationController> _logger;

    public RecommendationController(
        IRecommendationService recommendationService,
        ILogger<RecommendationController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// EC-02 — Grupos sugeridos durante el onboarding para unirse rápidamente.
    /// </summary>
    [HttpGet("groups/recommended")]
    public async Task<IActionResult> GetRecommendedGroups([FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        if (userId == 0) return UserUnauthorized();

        var result = await _recommendationService.GetRecommendationsAsync(userId, limit);
        return Ok(result);
    }

    /// <summary>
    /// EC-05 — Sección "Recomendado para ti" en la pantalla principal.
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetHomeRecommendations([FromQuery] int limit = 5)
    {
        var userId = GetUserId();
        if (userId == 0) return UserUnauthorized();

        var result = await _recommendationService.GetRecommendationsAsync(userId, limit);
        return Ok(result);
    }
}
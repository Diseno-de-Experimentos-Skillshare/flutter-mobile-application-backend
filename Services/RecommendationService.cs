using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;

namespace SkillShareBackend.Services;

public interface IRecommendationService
{
    /// <summary>
    /// Devuelve grupos recomendados para el usuario, priorizando afinidad de
    /// materia (interés) y luego popularidad (número de miembros).
    /// </summary>
    Task<List<RecommendedGroupDto>> GetRecommendationsAsync(int userId, int limit);
}

public class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(AppDbContext context, ILogger<RecommendationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RecommendedGroupDto>> GetRecommendationsAsync(int userId, int limit)
    {
        if (limit <= 0) limit = 5;

        // 1) Grupos donde el usuario ya es miembro (se excluyen de la recomendación).
        var joinedGroupIds = await _context.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync();

        // 2) Materias de interés = materias de los grupos donde ya participa.
        var interestSubjectIds = await _context.StudyGroups
            .Where(g => joinedGroupIds.Contains(g.Id) && g.SubjectId != null)
            .Select(g => g.SubjectId!.Value)
            .Distinct()
            .ToListAsync();

        // 3) Candidatos: grupos que no creó y a los que no pertenece.
        //    Se traen a memoria con su nº de miembros y materia para puntuarlos.
        var candidates = await _context.StudyGroups
            .Where(g => !joinedGroupIds.Contains(g.Id) && g.CreatedBy != userId)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.CoverImage,
                g.SubjectId,
                SubjectName = g.Subject != null ? g.Subject.Name : null,
                MemberCount = g.Members.Count()
            })
            .ToListAsync();

        // 4) Puntuación simple: primero por afinidad de materia, luego por popularidad.
        var recommended = candidates
            .Select(c => new
            {
                Group = c,
                IsInterest = c.SubjectId != null && interestSubjectIds.Contains(c.SubjectId.Value)
            })
            .OrderByDescending(x => x.IsInterest)
            .ThenByDescending(x => x.Group.MemberCount)
            .ThenByDescending(x => x.Group.Id) // desempate: más recientes primero
            .Take(limit)
            .Select(x => new RecommendedGroupDto
            {
                Id = x.Group.Id,
                Name = x.Group.Name,
                Description = x.Group.Description,
                CoverImage = x.Group.CoverImage,
                SubjectName = x.Group.SubjectName,
                MemberCount = x.Group.MemberCount,
                Reason = x.IsInterest && x.Group.SubjectName != null
                    ? $"Por tu interés en {x.Group.SubjectName}"
                    : "Popular en SkillShare"
            })
            .ToList();

        _logger.LogInformation(
            "Recomendaciones generadas para user {UserId}: {Count} grupos (limit {Limit}).",
            userId, recommended.Count, limit);

        return recommended;
    }
}
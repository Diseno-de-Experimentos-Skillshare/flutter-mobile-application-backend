namespace SkillShareBackend.DTOs;

/// <summary>
/// Grupo sugerido al usuario (usado por EC-02 onboarding y EC-05 home).
/// </summary>
public class RecommendedGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public string? SubjectName { get; set; }
    public int MemberCount { get; set; }

    /// <summary>
    /// Motivo de la recomendación, para mostrar en la UI
    /// (p. ej. "Por tu interés en Cálculo" o "Popular en SkillShare").
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
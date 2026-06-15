namespace SkillShareBackend.DTOs;

public class UpdateFcmTokenDto
{
    public string Token { get; set; } = string.Empty;
    public bool SessionRemindersEnabled { get; set; }
}

namespace SkillShareBackend.DTOs;

public class UpdateFcmTokenDto
{
    public string? Token { get; set; }
    public bool SessionRemindersEnabled { get; set; }
}

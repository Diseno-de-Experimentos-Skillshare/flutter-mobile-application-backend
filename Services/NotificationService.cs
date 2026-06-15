using FirebaseAdmin.Messaging;
using SkillShareBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace SkillShareBackend.Services;

public interface INotificationService
{
    Task<bool> SendSessionReminderAsync(int userId, string title, string body);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SendSessionReminderAsync(int userId, string title, string body)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            _logger.LogWarning($"User with ID {userId} not found for sending session reminder.");
            return false;
        }

        // Condición del Experimento: Debe dispararse solo si el usuario tiene session_reminders_enabled activo
        if (!user.SessionRemindersEnabled)
        {
            _logger.LogInformation($"Skipped sending session reminder to user {userId} because they are in the CONTROL cohort (session_reminders_enabled = false).");
            return false;
        }

        if (string.IsNullOrEmpty(user.FcmToken))
        {
            _logger.LogWarning($"User with ID {userId} has no FCM token registered.");
            return false;
        }

        try
        {
            var message = new Message
            {
                Token = user.FcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>
                {
                    { "type", "session_reminder" }
                }
            };

            _logger.LogInformation($"Sending session reminder FCM to user {userId}...");
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation($"FCM successfully sent to user {userId}. Response: {response}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send FCM to user {userId}");
            return false;
        }
    }
}

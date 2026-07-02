using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.Services;

namespace SkillShareBackend.Controllers;

/// <summary>
/// Disparador de notificaciones del experimento EC-01.
/// Envía recordatorios de sesión SOLO a la cohorte de tratamiento
/// (usuarios con session_reminders_enabled = true y token FCM registrado).
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// EC-01 — Envía el recordatorio de sesión a toda la cohorte de tratamiento.
    /// Devuelve un resumen: cuántos se enviaron y cuántos fallaron.
    /// </summary>
    [HttpPost("send-reminders")]
    public async Task<IActionResult> SendReminders(
        [FromQuery] string title = "Tu grupo tiene una sesión próxima",
        [FromQuery] string body = "Repasa tus materiales antes de empezar.")
    {
        var userId = GetUserId();
        if (userId == 0) return UserUnauthorized();

        // Cohorte de tratamiento: opt-in a recordatorios + token FCM válido.
        var treatmentUserIds = await _context.Users
            .Where(u => u.SessionRemindersEnabled
                        && u.FcmToken != null
                        && u.FcmToken != "")
            .Select(u => u.UserId)
            .ToListAsync();

        int sent = 0, failed = 0;
        foreach (var id in treatmentUserIds)
        {
            var ok = await _notificationService.SendSessionReminderAsync(id, title, body);
            if (ok) sent++; else failed++;
        }

        _logger.LogInformation(
            "EC-01 send-reminders: cohorte tratamiento={Total}, enviados={Sent}, fallidos={Failed}.",
            treatmentUserIds.Count, sent, failed);

        return Ok(new
        {
            cohorteTratamiento = treatmentUserIds.Count,
            enviados = sent,
            fallidos = failed,
            mensaje = "Recordatorios procesados. La cohorte de control no recibe notificaciones."
        });
    }

    /// <summary>
    /// Envío de prueba a un solo usuario (para validar el flujo sin recorrer toda la cohorte).
    /// </summary>
    [HttpPost("send-reminder/{targetUserId:int}")]
    public async Task<IActionResult> SendReminderToUser(int targetUserId)
    {
        var userId = GetUserId();
        if (userId == 0) return UserUnauthorized();

        var ok = await _notificationService.SendSessionReminderAsync(
            targetUserId,
            "Recordatorio de prueba",
            "Este es un envío de prueba de SkillShare.");

        return Ok(new
        {
            targetUserId,
            enviado = ok,
            mensaje = ok
                ? "Notificación enviada."
                : "No se envió: el usuario no existe, está en control o no tiene token FCM."
        });
    }
}
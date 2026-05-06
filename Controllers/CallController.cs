using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillShareBackend.Services;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallsController : ControllerBase
{
    private readonly ICallService _callService;

    public CallsController(ICallService callService)
    {
        _callService = callService;
    }

    [HttpPost("create-room")]
    public async Task<IActionResult> CreateCallRoom([FromBody] CreateCallRequest request)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1";
            var result = await _callService.CreateCallRoom(request.GroupId, userIdStr);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in CreateCallRoom: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("join-call")]
    public async Task<IActionResult> JoinCall([FromBody] JoinCallRequest request)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1";
            var result = await _callService.JoinCall(request.GroupId, userIdStr);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in JoinCall: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("active-call/{groupId}")]
    public async Task<IActionResult> GetActiveCall(int groupId)
    {
        try
        {
            var result = await _callService.GetActiveCall(groupId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in GetActiveCall: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("end-call")]
    public async Task<IActionResult> EndCall([FromBody] EndCallRequest request)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1";
            var success = await _callService.EndCall(request.GroupId, userIdStr);
            
            if (success)
            {
                return Ok(new { success = true, message = "Call ended successfully" });
            }
            
            return NotFound(new { success = false, message = "No active call found" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in EndCall: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("call-stats/{groupId}")]
    public async Task<IActionResult> GetCallStatistics(int groupId)
    {
        try
        {
            var result = await _callService.GetCallStats(groupId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in GetCallStatistics: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("user-stats")]
    public async Task<IActionResult> GetUserCallStatistics()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1";
            var result = await _callService.GetUserCallStats(userIdStr);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in GetUserCallStatistics: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

// DTOs para las solicitudes
public class CreateCallRequest
{
    public int GroupId { get; set; }
}

public class JoinCallRequest
{
    public int GroupId { get; set; }
}

public class EndCallRequest
{
    public int GroupId { get; set; }
}
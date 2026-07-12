using HospitalFlow.Data;
using HospitalFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalFlow.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAiAssistantService _aiService;

    public AiController(ApplicationDbContext context, IAiAssistantService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> AskAssistant([FromBody] PromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("Prompt cannot be empty.");
        }

        // Gather localized system state to feed into the prompt context window
        var rooms = await _context.Rooms.Include(r => r.Equipments).ToListAsync();
        var equipment = await _context.Equipments.ToListAsync();

        var roomSnapshot = string.Join("\n", rooms.Select(r =>
            $"- Room: {r.Name}, Dept: {r.Department}, Capacity: {r.Equipments.Count}/{r.MaxCapacity}"));

        var equipmentSnapshot = string.Join("\n", equipment.Select(e =>
            $"- Asset: ID {e.Id}, Type: {e.Type}, Status: {e.Status}, RoomId: {(e.RoomId.HasValue ? e.RoomId.ToString() : "Unassigned")}"));

        var systemContext = $"[LIVE ROOM MATRIX]\n{roomSnapshot}\n\n[LIVE ASSET TELEMETRY]\n{equipmentSnapshot}";

        var response = await _aiService.GetChatResponseAsync(request.Prompt, systemContext);

        return Ok(new { response });
    }
}

public class PromptRequest
{
    public string Prompt { get; set; } = string.Empty;
}
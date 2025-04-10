using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
}

public class UpdateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string NewTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
}

public class CalendarEventsController : ApplicationController
{
    private readonly DataService _dataService;

    public CalendarEventsController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCalendarEvents()
    {
        return Ok(await _dataService.GetCalendarEvents());
    }

    [HttpGet("{title}")]
    public async Task<IActionResult> GetCalendarEventByTitle(string title)
    {
        var entity = (await _dataService.GetCalendarEventByTitle(title));

        if (entity == null)
            return NotFound("Calendar event not found");

        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCalendarEvent(CreateCalendarEventRequest request)
    {
        var calendarEvent = new CalendarEvent(
            request.Title,
            request.Description,
            request.Date,
            request.Category,
            request.MatchId);

        await _dataService.SaveCalendarEvent(calendarEvent);

        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCalendarEvent(UpdateCalendarEventRequest request)
    {
        var entity = await _dataService.GetCalendarEventByTitle(request.Title);
        if (entity == null)
            return BadRequest("Calendar event not found");

        var updatedEvent = entity with
        {
            Title = request.NewTitle,
            Description = request.Description,
            Date = request.Date,
            Category = request.Category,
            MatchId = request.MatchId
        };

        await _dataService.UpdateCalendarEvent(updatedEvent);

        return NoContent();
    }

    [HttpDelete("{title}")]
    public async Task<IActionResult> DeleteCalendarEvent(string title)
    {
        var entity = await _dataService.GetCalendarEventByTitle(title);
        if (entity == null)
            return BadRequest("Calendar event not found");

        await _dataService.DeleteCalendarEvent(entity.Id);

        return NoContent();
    }
}

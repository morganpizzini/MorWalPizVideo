using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CalendarEventsController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly ILogger<CalendarEventsController> _logger;

        public CalendarEventsController(
            DataService dataService,
            ILogger<CalendarEventsController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Get all calendar events
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IList<CalendarEvent>>> GetAll()
        {
            try
            {
                var events = await _dataService.GetCalendarEvents();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar events");
                return StatusCode(500, "An error occurred while fetching calendar events");
            }
        }

        /// <summary>
        /// Get calendar event by title
        /// </summary>
        [HttpGet("by-title/{title}")]
        public async Task<ActionResult<CalendarEvent>> GetByTitle(string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest("Title cannot be empty");
                }

                var calendarEvent = await _dataService.GetCalendarEventByTitle(title);
                if (calendarEvent == null)
                {
                    return NotFound($"Calendar event with title '{title}' not found");
                }

                return Ok(calendarEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar event by title: {Title}", title);
                return StatusCode(500, "An error occurred while fetching the calendar event");
            }
        }

        /// <summary>
        /// Create a new calendar event
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CalendarEvent>> Create([FromBody] CalendarEvent calendarEvent)
        {
            try
            {
                if (calendarEvent == null)
                {
                    return BadRequest("Calendar event data is required");
                }

                if (string.IsNullOrWhiteSpace(calendarEvent.Title))
                {
                    return BadRequest("Title is required");
                }

                if (string.IsNullOrWhiteSpace(calendarEvent.Description))
                {
                    return BadRequest("Description is required");
                }

                if (calendarEvent.StartDate == default)
                {
                    return BadRequest("Start date is required");
                }

                if (calendarEvent.EndDate == default)
                {
                    return BadRequest("End date is required");
                }

                if (calendarEvent.EndDate < calendarEvent.StartDate)
                {
                    return BadRequest("End date must be after start date");
                }

                // Check if event with same title already exists
                var existingEvent = await _dataService.GetCalendarEventByTitle(calendarEvent.Title);
                if (existingEvent != null)
                {
                    return Conflict($"Calendar event with title '{calendarEvent.Title}' already exists");
                }

                await _dataService.SaveCalendarEvent(calendarEvent);
                
                _logger.LogInformation("Calendar event created: {Title}", calendarEvent.Title);
                
                return CreatedAtAction(nameof(GetByTitle), new { title = calendarEvent.Title }, calendarEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event");
                return StatusCode(500, "An error occurred while creating the calendar event");
            }
        }

        /// <summary>
        /// Update an existing calendar event
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] CalendarEvent calendarEvent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("ID is required");
                }

                if (calendarEvent == null)
                {
                    return BadRequest("Calendar event data is required");
                }

                if (string.IsNullOrWhiteSpace(calendarEvent.Title))
                {
                    return BadRequest("Title is required");
                }

                if (string.IsNullOrWhiteSpace(calendarEvent.Description))
                {
                    return BadRequest("Description is required");
                }

                if (calendarEvent.StartDate == default)
                {
                    return BadRequest("Start date is required");
                }

                if (calendarEvent.EndDate == default)
                {
                    return BadRequest("End date is required");
                }

                if (calendarEvent.EndDate < calendarEvent.StartDate)
                {
                    return BadRequest("End date must be after start date");
                }

                

                await _dataService.UpdateCalendarEvent(calendarEvent);
                
                _logger.LogInformation("Calendar event updated: {Id} - {Title}", id, calendarEvent.Title);
                
                return Ok(calendarEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event with ID: {Id}", id);
                return StatusCode(500, "An error occurred while updating the calendar event");
            }
        }

        /// <summary>
        /// Delete a calendar event
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("ID is required");
                }

                await _dataService.DeleteCalendarEvent(id);
                
                _logger.LogInformation("Calendar event deleted: {Id}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event with ID: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the calendar event");
            }
        }
    }
}

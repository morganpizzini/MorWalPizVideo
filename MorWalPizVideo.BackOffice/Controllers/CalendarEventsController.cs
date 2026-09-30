using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [RequireChannelScope]
    public class CalendarEventsController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarEventsController> _logger;

        public CalendarEventsController(
            ICalendarService calendarService,
            ILogger<CalendarEventsController> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        /// <summary>
        /// Get all calendar events
        /// </summary>
        [HttpGet]
        [AllowUser(AuthorizationPermissionKeys.CalendarView, AuthorizationPermissionKeys.CalendarManage)]
        public async Task<ActionResult<IList<CalendarEventContract>>> GetAll()
        {
            try
            {
                var events = await _calendarService.ListAsync(HttpContext.GetChannelContext().ChannelId);
                return Ok(events.Select(ContractUtils.Convert));
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
        [AllowUser(AuthorizationPermissionKeys.CalendarView, AuthorizationPermissionKeys.CalendarManage)]
        public async Task<ActionResult<CalendarEventContract>> GetByTitle(string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest("Title cannot be empty");
                }

                var calendarEvent = await _calendarService.GetByTitleAsync(title, HttpContext.GetChannelContext().ChannelId);
                if (calendarEvent == null)
                {
                    return NotFound($"Calendar event with title '{title}' not found");
                }

                return Ok(ContractUtils.Convert(calendarEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar event by title: {Title}", title);
                return StatusCode(500, "An error occurred while fetching the calendar event");
            }
        }

        [HttpGet("{id}")]
        [AllowUser(AuthorizationPermissionKeys.CalendarView, AuthorizationPermissionKeys.CalendarManage)]
        public async Task<ActionResult<CalendarEventContract>> GetById(string id)
        {
            var entity = await _calendarService.GetAsync(id, HttpContext.GetChannelContext().ChannelId);
            return entity is null ? NotFound("Calendar event not found") : Ok(ContractUtils.Convert(entity));
        }

        /// <summary>
        /// Create a new calendar event
        /// </summary>
        [HttpPost]
        [AllowUser(AuthorizationPermissionKeys.CalendarCreate, AuthorizationPermissionKeys.CalendarManage)]
        public async Task<ActionResult<CalendarEventContract>> Create([FromBody] SaveCalendarEventRequest request)
        {
            try
            {
                var channelId = HttpContext.GetChannelContext().ChannelId;
                var calendarEvent = await _calendarService.CreateAsync(request.ToEntity(channelId), channelId);

                _logger.LogInformation("Calendar event created: {Title}", calendarEvent.Title);

                return CreatedAtAction(nameof(GetByTitle), new { title = calendarEvent.Title }, ContractUtils.Convert(calendarEvent));
            }
            catch (CalendarValidationException ex) { return BadRequest(ex.Message); }
            catch (CalendarConflictException ex) { return Conflict(ex.Message); }
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
        [AllowUser(AuthorizationPermissionKeys.CalendarUpdate, AuthorizationPermissionKeys.CalendarManage)]
        public async Task<ActionResult<CalendarEventContract>> Update(string id, [FromBody] SaveCalendarEventRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("ID is required");
                }

                var channelId = HttpContext.GetChannelContext().ChannelId;
                var calendarEvent = await _calendarService.UpdateAsync(id, request.ToEntity(channelId), channelId, request.Revision);
                if (calendarEvent is null) return NotFound("Calendar event not found");

                _logger.LogInformation("Calendar event updated: {Id} - {Title}", id, calendarEvent.Title);

                return Ok(ContractUtils.Convert(calendarEvent));
            }
            catch (CalendarValidationException ex) { return BadRequest(ex.Message); }
            catch (CalendarConflictException ex) { return Conflict(ex.Message); }
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
        [AllowUser(AuthorizationPermissionKeys.CalendarDelete, AuthorizationPermissionKeys.CalendarManage)]
        public async Task<ActionResult> Delete(string id, [FromQuery] long? revision = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("ID is required");
                }

                if (!await _calendarService.DeleteAsync(id, HttpContext.GetChannelContext().ChannelId, revision))
                {
                    return NotFound("Calendar event not found");
                }

                _logger.LogInformation("Calendar event deleted: {Id}", id);

                return NoContent();
            }
            catch (CalendarValidationException ex) { return BadRequest(ex.Message); }
            catch (CalendarConflictException ex) { return Conflict(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event with ID: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the calendar event");
            }
        }
    }
}

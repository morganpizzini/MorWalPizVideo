using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreatePublishScheduleRequest
{
  public string VideoId { get; set; } = string.Empty;
  public string[] QueryStringIds { get; set; } = Array.Empty<string>();
  public string Message { get; set; } = string.Empty;
  public DateTime Date { get; set; }
}

public class UpdatePublishScheduleRequest
{
  public string VideoId { get; set; } = string.Empty;
  public string[] QueryStringIds { get; set; } = Array.Empty<string>();
  public string Message { get; set; } = string.Empty;
  public DateTime Date { get; set; }
}

public class PublishScheduleController : ApplicationController
{
  private readonly DataService _dataService;

  public PublishScheduleController(DataService dataService)
  {
    _dataService = dataService;
  }

  [HttpGet]
  public async Task<IActionResult> GetPublishSchedules()
  {
    var schedules = await _dataService.GetPublishSchedules();
    return Ok(schedules);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetPublishSchedule(string id)
  {
    var schedule = await _dataService.GetPublishScheduleById(id);
    if (schedule == null)
      return NotFound("Publish schedule not found");

    return Ok(schedule);
  }

  [HttpGet("video/{videoId}")]
  public async Task<IActionResult> GetPublishScheduleByVideoId(string videoId)
  {
    var schedule = await _dataService.GetPublishScheduleByVideoId(videoId);
    if (schedule == null)
      return NotFound("Publish schedule not found for the specified video");

    return Ok(schedule);
  }

  [HttpPost]
  public async Task<IActionResult> CreatePublishSchedule(CreatePublishScheduleRequest request)
  {
    var schedule = new PublishSchedule(
        request.VideoId,
        request.QueryStringIds,
        request.Message,
        request.Date);

    await _dataService.SavePublishSchedule(schedule);
    return NoContent();
  }

  [HttpPut("{id}")]
  public async Task<IActionResult> UpdatePublishSchedule(string id, UpdatePublishScheduleRequest request)
  {
    var entity = await _dataService.GetPublishScheduleById(id);
    if (entity == null)
      return BadRequest("Publish schedule not found");

    entity = entity with
    {
      VideoId = request.VideoId,
      QueryStringIds = request.QueryStringIds,
      Message = request.Message,
      Date = request.Date
    };

    await _dataService.UpdatePublishSchedule(entity);
    return NoContent();
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeletePublishSchedule(string id)
  {
    var entity = await _dataService.GetPublishScheduleById(id);
    if (entity == null)
      return BadRequest("Publish schedule not found");

    await _dataService.DeletePublishSchedule(id);
    return NoContent();
  }
}

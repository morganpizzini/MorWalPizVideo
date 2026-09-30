using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

public sealed record SaveCalendarEventRequest
{
    public string? Id { get; init; }
    [Required]
    public string Title { get; init; } = string.Empty;
    [Required]
    public string Description { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    [Required]
    public CategoryRef[] Categories { get; init; } = [];
    public string MatchId { get; init; } = string.Empty;
    public string? ChannelId { get; init; }
    public DateTime? CreationDateTime { get; init; }
    public long? Revision { get; init; }

    public CalendarEvent ToEntity(string channelId) => new(Title, Description, StartDate, EndDate, Categories, MatchId)
    {
        Id = Id!, ChannelId = channelId, CreationDateTime = CreationDateTime ?? DateTime.Now
    };
}

public sealed record PublicCalendarEventResponse(
    string Id, DateTime CreationDateTime, string Title, string Description,
    DateTime StartDate, DateTime EndDate, CategoryRef[] Categories,
    string MatchId, string ChannelId, string? MatchUrl, bool OldEvent);
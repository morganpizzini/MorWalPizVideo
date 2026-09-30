using MongoDB.Bson;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services;

public interface ICalendarService
{
    Task<IList<CalendarEvent>> ListAsync(string? channelId = null);
    Task<IList<CalendarEvent>> GetRecentAsync(DateTime fromInclusive, int limit);
    Task<CalendarEvent?> GetByTitleAsync(string title, string? channelId = null);
    Task<CalendarEvent?> GetAsync(string id, string? channelId = null);
    Task<CalendarEvent> CreateAsync(CalendarEvent entity, string channelId);
    Task<CalendarEvent?> UpdateAsync(string id, CalendarEvent entity, string channelId, long? expectedRevision = null);
    Task<bool> DeleteAsync(string id, string channelId, long? expectedRevision = null);
}

public sealed class CalendarValidationException(string message) : Exception(message);
public sealed class CalendarConflictException(string message) : Exception(message);

public sealed class CalendarService(ICalendarEventRepository repository) : ICalendarService
{
    public Task<IList<CalendarEvent>> ListAsync(string? channelId = null) => channelId is null
        ? repository.GetItemsAsync() : repository.GetItemsAsync(entity => entity.ChannelId == channelId);

    public Task<IList<CalendarEvent>> GetRecentAsync(DateTime fromInclusive, int limit) => repository.GetRecentAsync(fromInclusive, limit);

    public async Task<CalendarEvent?> GetByTitleAsync(string title, string? channelId = null) =>
        (await repository.GetItemsAsync(entity => entity.Title.ToLower() == title.ToLower() &&
            (channelId == null || entity.ChannelId == channelId))).FirstOrDefault();

    public async Task<CalendarEvent?> GetAsync(string id, string? channelId = null) =>
        (await repository.GetItemsAsync(entity => entity.Id == id &&
            (channelId == null || entity.ChannelId == channelId))).FirstOrDefault();

    public async Task<CalendarEvent> CreateAsync(CalendarEvent entity, string channelId)
    {
        Validate(entity);
        if (await GetByTitleAsync(entity.Title) is not null)
            throw new CalendarConflictException($"Calendar event with title '{entity.Title}' already exists");
        var created = entity with
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? ObjectId.GenerateNewId().ToString() : entity.Id,
            ChannelId = channelId, Revision = 1
        };
        if (!ObjectId.TryParse(created.Id, out _)) throw new CalendarValidationException("Invalid ID");
        return await repository.AddItemAsync(created);
    }

    public async Task<CalendarEvent?> UpdateAsync(string id, CalendarEvent entity, string channelId, long? expectedRevision = null)
    {
        ValidateRevision(expectedRevision);
        if (!string.IsNullOrWhiteSpace(entity.Id) && entity.Id != id)
            throw new CalendarValidationException("Route and body IDs must match");
        Validate(entity);
        var existing = await GetAsync(id, channelId);
        if (existing is null) return null;
        var revision = expectedRevision ?? existing.Revision;
        if (revision != existing.Revision) throw new CalendarConflictException("Calendar event changed. Reload before saving.");
        ValidateRevision(revision);
        var updated = entity with { Id = existing.Id, ChannelId = existing.ChannelId, CreationDateTime = existing.CreationDateTime, Revision = revision + 1 };
        if (!await repository.ReplaceAsync(updated, revision)) throw new CalendarConflictException("Calendar event changed. Reload before saving.");
        return updated;
    }

    public async Task<bool> DeleteAsync(string id, string channelId, long? expectedRevision = null)
    {
        ValidateRevision(expectedRevision);
        var existing = await GetAsync(id, channelId);
        if (existing is null) return false;
        var revision = expectedRevision ?? existing.Revision;
        if (revision != existing.Revision) throw new CalendarConflictException("Calendar event changed. Reload before deleting.");
        if (!await repository.DeleteAsync(channelId, id, revision)) throw new CalendarConflictException("Calendar event changed. Reload before deleting.");
        return true;
    }

    private static void ValidateRevision(long? revision)
    {
        if (revision is < 0 or >= 9_007_199_254_740_991)
            throw new CalendarValidationException("Invalid expected revision");
    }

    private static void Validate(CalendarEvent entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Title) || string.IsNullOrWhiteSpace(entity.Description))
            throw new CalendarValidationException("Title and description are required");
        if (entity.StartDate == default || entity.EndDate == default || entity.EndDate < entity.StartDate)
            throw new CalendarValidationException("Valid start and end dates are required; end date must not precede start date");
        if (entity.Categories is null || entity.Categories.Any(category => category is null ||
            string.IsNullOrWhiteSpace(category.Id) || string.IsNullOrWhiteSpace(category.Title)))
            throw new CalendarValidationException("Categories must contain IDs and titles");
    }
}
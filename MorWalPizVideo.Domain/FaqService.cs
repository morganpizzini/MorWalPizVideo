using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed record FaqPublicAnswer(string ChannelName, string Content, int HelpfulVotes, int NotHelpfulVotes);
public sealed record FaqPublicItem(string Id, string Question, string CategorySlug, string CategoryName, IReadOnlyList<FaqPublicAnswer> Answers);
public sealed record FaqVoteResult(bool Accepted, bool Changed, int HelpfulVotes, int NotHelpfulVotes);

public interface IFaqService
{
    Task<IReadOnlyList<FaqPublicItem>> GetPublicAsync(string? categorySlug = null);
    Task<IReadOnlyList<Faq>> GetAllAsync(string? search = null, string? categoryId = null, FaqLifecycleStatus? status = null);
    Task<Faq?> GetAsync(string id);
    Task<Faq> CreateAsync(Faq faq);
    Task<Faq?> UpdateAsync(Faq faq);
    Task<IReadOnlyList<FaqCategory>> GetCategoriesAsync(bool activeOnly = false);
    Task<FaqCategory> SaveCategoryAsync(FaqCategory category);
    Task<IReadOnlyList<FaqAnswer>> GetAnswersAsync(string faqId, string channelId);
    Task<FaqAnswer> CreateAnswerAsync(FaqAnswer answer);
    Task<FaqAnswer?> UpdateAnswerAsync(FaqAnswer answer, string channelId);
    Task<FaqVoteResult?> VoteAsync(string answerId, string userId, FaqVoteValue value);
    Task<FaqVoteResult?> VoteAsync(string faqId, string channelName, string userId, FaqVoteValue value);
    Task<IReadOnlyList<FaqCandidate>> GetCandidatesAsync(FaqCandidateStatus? status = null);
    Task<FaqCandidate?> ReviewCandidateAsync(string id, FaqCandidateStatus status);
    IReadOnlyList<string> Validate(Faq faq);
    IReadOnlyList<string> Validate(FaqAnswer answer);
}

public sealed class FaqService(
    IFaqRepository faqRepository,
    IFaqCategoryRepository categoryRepository,
    IFaqAnswerRepository answerRepository,
    IFaqCandidateRepository candidateRepository,
    IFaqVoteRepository voteRepository,
    IYTChannelRepository channelRepository) : IFaqService
{
    public async Task<IReadOnlyList<FaqPublicItem>> GetPublicAsync(string? categorySlug = null)
    {
        var categories = await categoryRepository.GetItemsAsync(x => x.IsActive);
        var category = string.IsNullOrWhiteSpace(categorySlug)
            ? null
            : categories.FirstOrDefault(x => NormalizeSlug(x.Slug) == NormalizeSlug(categorySlug));
        if (!string.IsNullOrWhiteSpace(categorySlug) && category is null) return [];

        var activeCategoryIds = categories.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var faqs = (await faqRepository.GetPublicAsync(category?.Id))
            .Where(item => activeCategoryIds.Contains(item.CategoryId))
            .ToArray();
        var answers = (await Task.WhenAll(faqs.Select(async faq => (Faq: faq, Answers: (IReadOnlyList<FaqAnswer>)await answerRepository.GetByFaqIdAsync(faq.Id))))).ToDictionary(x => x.Faq.Id, x => x.Answers);
        var channels = await channelRepository.GetItemsAsync();
        var channelNames = channels.ToDictionary(x => x.ChannelId, x => x.ChannelName, StringComparer.OrdinalIgnoreCase);
        return faqs.OrderBy(x => x.Question, StringComparer.OrdinalIgnoreCase).Select(faq => new FaqPublicItem(
            faq.Id,
            faq.Question,
            categories.FirstOrDefault(x => x.Id == faq.CategoryId)?.Slug ?? string.Empty,
            categories.FirstOrDefault(x => x.Id == faq.CategoryId)?.Name ?? string.Empty,
            answers[faq.Id].Where(x => x.Status == FaqLifecycleStatus.Published && channelNames.ContainsKey(x.ChannelId))
                .OrderBy(x => channelNames[x.ChannelId], StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new FaqPublicAnswer(channelNames[x.ChannelId], x.Content, x.HelpfulVotes, x.NotHelpfulVotes)).ToArray())).ToArray();
    }

    public async Task<IReadOnlyList<Faq>> GetAllAsync(string? search = null, string? categoryId = null, FaqLifecycleStatus? status = null)
    {
        var normalizedSearch = search?.Trim();
        return (await faqRepository.GetItemsAsync(x =>
                (string.IsNullOrWhiteSpace(normalizedSearch) || x.Question.ToLower().Contains(normalizedSearch!.ToLower())) &&
                (string.IsNullOrWhiteSpace(categoryId) || x.CategoryId == categoryId) &&
                (status == null || x.Status == status)))
            .OrderByDescending(x => x.UpdatedAt).ToArray();
    }

    public async Task<Faq?> GetAsync(string id) => await faqRepository.GetItemAsync(id);

    public async Task<Faq> CreateAsync(Faq faq)
    {
        var normalized = Normalize(faq) with { Id = ObjectId.GenerateNewId().ToString(), CreationDateTime = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await faqRepository.AddItemAsync(normalized);
        return normalized;
    }

    public async Task<Faq?> UpdateAsync(Faq faq)
    {
        var existing = await faqRepository.GetItemAsync(faq.Id);
        if (existing is null) return null;
        var normalized = Normalize(faq) with { Id = existing.Id, CreationDateTime = existing.CreationDateTime, UpdatedAt = DateTime.UtcNow, PublishedAt = faq.Status == FaqLifecycleStatus.Published && existing.Status != FaqLifecycleStatus.Published ? DateTime.UtcNow : faq.PublishedAt };
        await faqRepository.UpdateItemAsync(normalized);
        return normalized;
    }

    public async Task<IReadOnlyList<FaqCategory>> GetCategoriesAsync(bool activeOnly = false)
        => (await categoryRepository.GetItemsAsync(x => !activeOnly || x.IsActive)).OrderBy(x => x.SortOrder).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public async Task<FaqCategory> SaveCategoryAsync(FaqCategory category)
    {
        var normalized = category with { Slug = NormalizeSlug(category.Slug.Length == 0 ? category.Name : category.Slug), Name = category.Name.Trim(), Description = category.Description.Trim() };
        if (string.IsNullOrWhiteSpace(normalized.Id))
        {
            normalized = normalized with { Id = ObjectId.GenerateNewId().ToString(), CreationDateTime = DateTime.UtcNow };
            await categoryRepository.AddItemAsync(normalized);
        }
        else await categoryRepository.UpdateItemAsync(normalized);
        return normalized;
    }

    public async Task<IReadOnlyList<FaqAnswer>> GetAnswersAsync(string faqId, string channelId)
        => (await answerRepository.GetByFaqIdAndChannelIdAsync(faqId, channelId)).ToArray();

    public async Task<FaqAnswer> CreateAnswerAsync(FaqAnswer answer)
    {
        var normalized = answer with { Id = ObjectId.GenerateNewId().ToString(), Content = answer.Content.Trim(), CreationDateTime = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await answerRepository.AddItemAsync(normalized);
        return normalized;
    }

    public async Task<FaqAnswer?> UpdateAnswerAsync(FaqAnswer answer, string channelId)
    {
        var existing = (await answerRepository.GetItemsAsync(x => x.Id == answer.Id && x.ChannelId == channelId)).FirstOrDefault();
        if (existing is null) return null;
        var normalized = answer with { Id = existing.Id, FaqId = existing.FaqId, ChannelId = existing.ChannelId, CreationDateTime = existing.CreationDateTime, Content = answer.Content.Trim(), UpdatedAt = DateTime.UtcNow, PublishedAt = answer.Status == FaqLifecycleStatus.Published && existing.Status != FaqLifecycleStatus.Published ? DateTime.UtcNow : answer.PublishedAt };
        await answerRepository.UpdateItemAsync(normalized);
        return normalized;
    }

    public async Task<FaqVoteResult?> VoteAsync(string answerId, string userId, FaqVoteValue value)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Enum.IsDefined(value)) return null;
        var answer = (await answerRepository.GetItemsAsync(x => x.Id == answerId && x.Status == FaqLifecycleStatus.Published)).FirstOrDefault();
        if (answer is null) return null;
        var faq = (await faqRepository.GetItemsAsync(x => x.Id == answer.FaqId && x.Status == FaqLifecycleStatus.Published)).FirstOrDefault();
        if (faq is null) return null;
        var category = (await categoryRepository.GetItemsAsync(x => x.Id == faq.CategoryId && x.IsActive)).FirstOrDefault();
        if (category is null) return null;
        var existing = await voteRepository.GetByAnswerAndUserAsync(answerId, userId);
        if (existing is not null && existing.Value == value) return new(false, false, answer.HelpfulVotes, answer.NotHelpfulVotes);
        if (existing is null)
        {
            var vote = new FaqVote { Id = ObjectId.GenerateNewId().ToString(), AnswerId = answerId, UserId = userId, Value = value };
            try { await voteRepository.AddItemAsync(vote); }
            catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { return new(false, false, answer.HelpfulVotes, answer.NotHelpfulVotes); }
        }
        else
        {
            await voteRepository.UpdateItemAsync(existing with { Value = value, UpdatedAt = DateTime.UtcNow });
            await answerRepository.IncrementVoteAsync(answerId, existing.Value, -1);
        }
        await answerRepository.IncrementVoteAsync(answerId, value, 1);
        var updated = (await answerRepository.GetItemsAsync(x => x.Id == answerId)).First();
        return new(true, existing is not null, updated.HelpfulVotes, updated.NotHelpfulVotes);
    }

    public async Task<FaqVoteResult?> VoteAsync(string faqId, string channelName, string userId, FaqVoteValue value)
    {
        var channel = (await channelRepository.GetItemsAsync(x => x.ChannelName.ToLower() == channelName.Trim().ToLower())).FirstOrDefault();
        var answer = channel is null ? null : (await answerRepository.GetItemsAsync(x => x.FaqId == faqId && x.ChannelId == channel.ChannelId && x.Status == FaqLifecycleStatus.Published)).FirstOrDefault();
        return answer is null ? null : await VoteAsync(answer.Id, userId, value);
    }

    public async Task<IReadOnlyList<FaqCandidate>> GetCandidatesAsync(FaqCandidateStatus? status = null)
        => (await candidateRepository.GetItemsAsync(x => status == null || x.Status == status)).OrderByDescending(x => x.CreationDateTime).ToArray();

    public async Task<FaqCandidate?> ReviewCandidateAsync(string id, FaqCandidateStatus status)
    {
        var candidate = await candidateRepository.GetItemAsync(id);
        if (candidate is null) return null;
        var updated = candidate with { Status = status };
        await candidateRepository.UpdateItemAsync(updated);
        return updated;
    }

    public IReadOnlyList<string> Validate(Faq faq)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(faq.Question) || faq.Question.Trim().Length > 500) errors.Add("Question is required and must be at most 500 characters.");
        if (string.IsNullOrWhiteSpace(faq.CategoryId)) errors.Add("Category is required.");
        if (!Enum.IsDefined(faq.Status)) errors.Add("Status is invalid.");
        return errors;
    }

    public IReadOnlyList<string> Validate(FaqAnswer answer)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(answer.FaqId)) errors.Add("FAQ is required.");
        if (string.IsNullOrWhiteSpace(answer.ChannelId)) errors.Add("Channel owner is required.");
        if (string.IsNullOrWhiteSpace(answer.Content)) errors.Add("Answer content is required.");
        if (!Enum.IsDefined(answer.Status)) errors.Add("Status is invalid.");
        return errors;
    }

    private static Faq Normalize(Faq faq) => faq with { Question = Regex.Replace(faq.Question.Trim(), "\\s+", " ") };
    private static string NormalizeSlug(string value) => Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}
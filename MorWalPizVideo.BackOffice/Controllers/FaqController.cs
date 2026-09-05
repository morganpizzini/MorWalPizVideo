using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class FaqCandidateGenerationRequest
{
    public IReadOnlyList<string> CampaignIds { get; set; } = [];
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class FaqController(IFaqService faqService, IFaqCandidateGenerationService candidateGenerationService, ICrossApiService crossApiService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.FaqView, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<IReadOnlyList<FaqContract>>> List([FromQuery] string? search = null, [FromQuery] string? categoryId = null, [FromQuery] FaqLifecycleStatus? status = null)
        => Ok((await faqService.GetAllAsync(search, categoryId, status)).Select(ContractUtils.Convert).ToArray());

    [HttpGet("categories")]
    [AllowUser(AuthorizationPermissionKeys.FaqView, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<IReadOnlyList<FaqCategoryContract>>> Categories()
        => Ok((await faqService.GetCategoriesAsync()).Select(ContractUtils.Convert).ToArray());

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqContract>> Create(Faq faq)
    {
        var errors = faqService.Validate(faq);
        if (errors.Count > 0) return BadRequest(errors);
        var created = await faqService.CreateAsync(faq);
        await InvalidateAsync();
        return CreatedAtAction(nameof(Get), new { id = created.Id }, ContractUtils.Convert(created));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FaqView, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqContract>> Get(string id)
    {
        var faq = await faqService.GetAsync(id);
        return faq is null ? NotFound() : Ok(ContractUtils.Convert(faq));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqContract>> Update(string id, Faq faq)
    {
        var errors = faqService.Validate(faq);
        if (errors.Count > 0) return BadRequest(errors);
        var updated = await faqService.UpdateAsync(faq with { Id = id });
        if (updated is null) return NotFound();
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(updated));
    }

    [HttpPut("categories/{id?}")]
    [AllowUser(AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqCategoryContract>> SaveCategory(string? id, FaqCategory category)
    {
        var result = await faqService.SaveCategoryAsync(category with { Id = id ?? category.Id });
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(result));
    }

    [HttpGet("{id}/answers")]
    [AllowUser(AuthorizationPermissionKeys.FaqView, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<IReadOnlyList<FaqAnswerContract>>> Answers(string id)
        => Ok((await faqService.GetAnswersAsync(id, HttpContext.GetChannelContext().ChannelId)).Select(ContractUtils.Convert).ToArray());

    [HttpPost("{id}/answers")]
    [AllowUser(AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqAnswerContract>> CreateAnswer(string id, FaqAnswer answer)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var candidate = answer with { FaqId = id, ChannelId = channelId };
        var errors = faqService.Validate(candidate);
        if (errors.Count > 0) return BadRequest(errors);
        var created = await faqService.CreateAnswerAsync(candidate);
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(created));
    }

    [HttpPut("answers/{answerId}")]
    [AllowUser(AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqAnswerContract>> UpdateAnswer(string answerId, FaqAnswer answer)
    {
        var updated = await faqService.UpdateAnswerAsync(answer with { Id = answerId }, HttpContext.GetChannelContext().ChannelId);
        if (updated is null) return NotFound();
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(updated));
    }

    [HttpGet("candidates")]
    [AllowUser(AuthorizationPermissionKeys.FaqCandidates, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<IReadOnlyList<FaqCandidate>>> Candidates([FromQuery] FaqCandidateStatus? status = null)
        => Ok(await faqService.GetCandidatesAsync(status));

    [HttpPost("candidates/generate")]
    [AllowUser(AuthorizationPermissionKeys.FaqCandidates, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<IReadOnlyList<FaqCandidate>>> GenerateCandidates(FaqCandidateGenerationRequest request, CancellationToken cancellationToken)
    {
        var campaignIds = request.CampaignIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        if (campaignIds.Length == 0) return BadRequest("Select at least one Campaign.");

        try
        {
            var candidates = await candidateGenerationService.GenerateAsync(campaignIds, User.Identity?.Name ?? "backoffice", cancellationToken);
            return Ok(candidates);
        }
        catch (FaqCandidateGenerationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }

    [HttpPost("candidates/{id}/review")]
    [AllowUser(AuthorizationPermissionKeys.FaqCandidates, AuthorizationPermissionKeys.FaqManage)]
    public async Task<ActionResult<FaqCandidate>> ReviewCandidate(string id, [FromBody] FaqCandidateStatus status)
        => (await faqService.ReviewCandidateAsync(id, status)) is { } item ? Ok(item) : NotFound();

    private async Task InvalidateAsync()
    {
        await crossApiService.ResetCache(CacheKeys.Faq);
        await crossApiService.PurgeCache(ApiTagCacheKeys.Faq);
    }
}
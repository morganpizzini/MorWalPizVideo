using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.ServerAPI.Controllers;

[Route("api/faq")]
public sealed class FaqController(IFaqService faqService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(Tags = [ApiTagCacheKeys.Faq], VaryByQueryKeys = ["category"])]
    public async Task<ActionResult<IReadOnlyList<FaqPublicContract>>> Get([FromQuery] string? category = null)
    {
        var items = await faqService.GetPublicAsync(category);
        return Ok(items.Select(item => new FaqPublicContract
        {
            Id = item.Id, Question = item.Question, CategorySlug = item.CategorySlug, CategoryName = item.CategoryName,
            Answers = item.Answers.Select(answer => new FaqPublicAnswerContract
            {
                ChannelName = answer.ChannelName, Content = answer.Content,
                HelpfulVotes = answer.HelpfulVotes, NotHelpfulVotes = answer.NotHelpfulVotes
            }).ToArray()
        }).ToArray());
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    [OutputCache(Tags = [ApiTagCacheKeys.Faq])]
    public async Task<ActionResult<IReadOnlyList<FaqCategoryContract>>> Categories()
        => Ok((await faqService.GetCategoriesAsync(true)).Select(ContractUtils.Convert).ToArray());

    [HttpPost("{faqId}/answers/{channelName}/vote")]
    [Authorize]
    [EnableRateLimiting("faq-vote")]
    public async Task<ActionResult<FaqVoteContract>> Vote(string faqId, string channelName, [FromBody] FaqVoteValue value)
    {
        if (!Enum.IsDefined(value)) return BadRequest(new { message = "Vote value is invalid." });
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await faqService.VoteAsync(faqId, channelName, userId ?? string.Empty, value);
        return result is null ? NotFound() : Ok(new FaqVoteContract { Accepted = result.Accepted, Changed = result.Changed, HelpfulVotes = result.HelpfulVotes, NotHelpfulVotes = result.NotHelpfulVotes });
    }
}
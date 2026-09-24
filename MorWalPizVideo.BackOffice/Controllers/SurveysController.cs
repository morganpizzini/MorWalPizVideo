using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class CreateSurveyRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string[] FormIds { get; set; } = [];
    public SurveyLifecycle Lifecycle { get; set; } = SurveyLifecycle.Draft;
}

[RequireChannelScope]
public sealed class SurveysController(
    ISurveyRepository surveyRepository,
    ICustomFormRepository formRepository,
    ICrossApiService crossApiService) : ApplicationControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.FormsView, AuthorizationPermissionKeys.FormsManage)]
    public async Task<ActionResult<IList<SurveyContract>>> Fetch()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var surveys = await surveyRepository.GetItemsAsync(x => x.ChannelId == channelId);
        return Ok(surveys.Select(ContractUtils.Convert).ToList());
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FormsView, AuthorizationPermissionKeys.FormsManage)]
    public async Task<ActionResult<SurveyContract>> Get(string id)
    {
        var survey = await Find(id);
        return survey is null ? NotFound() : Ok(ContractUtils.Convert(survey));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.FormsCreate, AuthorizationPermissionKeys.FormsManage)]
    public async Task<ActionResult<SurveyContract>> Create(BaseRequest<CreateSurveyRequest> request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var validation = await Validate(request.Body, channelId);
        if (validation is not null) return BadRequest(validation);
        var survey = new Survey(request.Body.Title, request.Body.Description, request.Body.Url, channelId, request.Body.FromUtc, request.Body.ToUtc, request.Body.FormIds, request.Body.Lifecycle);
        await surveyRepository.AddItemAsync(survey);
        await InvalidatePublicSurveyCacheAsync();
        return CreatedAtAction(nameof(Get), new { id = survey.Id }, ContractUtils.Convert(survey));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FormsUpdate, AuthorizationPermissionKeys.FormsManage)]
    public async Task<IActionResult> Update(string id, BaseRequest<CreateSurveyRequest> request)
    {
        var existing = await Find(id);
        if (existing is null) return NotFound();
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var validation = await Validate(request.Body, channelId);
        if (validation is not null) return BadRequest(validation);
        await surveyRepository.UpdateItemAsync(existing with { Title = request.Body.Title, Description = request.Body.Description, Url = request.Body.Url, FromUtc = request.Body.FromUtc.ToUniversalTime(), ToUtc = request.Body.ToUtc.ToUniversalTime(), FormIds = request.Body.FormIds, Lifecycle = request.Body.Lifecycle });
        await InvalidatePublicSurveyCacheAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FormsDelete, AuthorizationPermissionKeys.FormsManage)]
    public async Task<IActionResult> Delete(string id)
    {
        var survey = await Find(id);
        if (survey is null) return NotFound();
        await surveyRepository.UpdateItemAsync(survey with { Lifecycle = SurveyLifecycle.Archived });
        await InvalidatePublicSurveyCacheAsync();
        return NoContent();
    }

    private async Task InvalidatePublicSurveyCacheAsync()
    {
        await crossApiService.ResetCache(CacheKeys.Surveys);
        await crossApiService.PurgeCache(CacheKeys.Surveys);
    }

    private async Task<Survey?> Find(string id) => (await surveyRepository.GetItemsAsync(x => x.Id == id && x.ChannelId == HttpContext.GetChannelContext().ChannelId)).FirstOrDefault();

    private async Task<string?> Validate(CreateSurveyRequest request, string channelId)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Url)) return "Title and URL are required.";
        if (request.FromUtc.ToUniversalTime() >= request.ToUtc.ToUniversalTime()) return "FromUtc must be earlier than ToUtc.";
        if (request.FormIds.Length == 0) return "At least one form is required.";
        if (request.FormIds.Distinct(StringComparer.Ordinal).Count() != request.FormIds.Length) return "FormIds must not contain duplicates.";
        var forms = await formRepository.GetItemsAsync(x => x.ChannelId == channelId && request.FormIds.Contains(x.Id));
        if (forms.Count != request.FormIds.Length) return "Every form must exist in the selected channel.";
        if (forms.Any(x => x.EffectiveLifecycle != CustomFormLifecycle.Online || x.EffectiveAccessMode is not (CustomFormAccessMode.Direct or CustomFormAccessMode.SurveyOnly))) return "Every referenced form must be Online and eligible.";
        return null;
    }
}
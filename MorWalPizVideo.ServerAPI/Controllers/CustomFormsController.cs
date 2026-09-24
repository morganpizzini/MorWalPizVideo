using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    public class SubmitFormResponseRequest
    {
        [Required]
        public CustomFormAnswer[] Answers { get; set; } = [];

        public string? SurveyId { get; set; }
    }
    
    [AllowAnonymous] // ADR-002: explicit public read/submit access
    public class CustomFormsController : ApplicationController
    {
        private readonly ILogger<CustomFormsController> _logger;
        private readonly IFormsService _formsService;
        private readonly IConfiguration _configuration;
        private readonly ISurveyService _surveyService;

        public CustomFormsController(
            IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            IFormsService formsService,
            ILogger<CustomFormsController> logger,
            IConfiguration configuration,
            ISurveyService surveyService) : base(_dataService, _memoryCache)
        {
            _formsService = formsService;
            _logger = logger;
            _configuration = configuration;
            _surveyService = surveyService;
        }

        [HttpGet("../surveys/active")]
        [OutputCache(Tags = [CacheKeys.Surveys])]
        public async Task<IActionResult> GetEligibleSurveys()
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
                return Ok(Array.Empty<object>());

            var surveys = await _surveyService.GetEligibleAsync(channelId, DateTime.UtcNow);
            return Ok(surveys.Select(x => new
            {
                x.Survey.Id, x.Survey.Title, x.Survey.Description, x.Survey.Url,
                fromUtc = x.Survey.FromUtc, toUtc = x.Survey.ToUtc,
                forms = x.Forms.Select(form => form with { Responses = Array.Empty<CustomFormResponse>() })
            }));
        }

        [HttpGet("../surveys/url/{url}")]
        [OutputCache(Tags = [CacheKeys.Surveys], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> GetSurveyByUrl(string url)
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
                return NotFound();

            var survey = await _surveyService.GetByUrlAsync(url, channelId, DateTime.UtcNow);
            return survey is null
                ? NotFound($"Survey with URL '{url}' not found")
                : Ok(new
                {
                    survey.Survey.Id, survey.Survey.Title, survey.Survey.Description, survey.Survey.Url,
                    fromUtc = survey.Survey.FromUtc, toUtc = survey.Survey.ToUtc,
                    forms = survey.Forms.Select(form => form with { Responses = Array.Empty<CustomFormResponse>() })
                });
        }

        /// <summary>
        /// Get all active custom forms (questions only, no responses for privacy)
        /// </summary>
        [HttpGet("active")]
        [OutputCache(Tags = [CacheKeys.CustomForms])]
        public async Task<IActionResult> GetActiveForms()
        {
            try
            {
                var channelId = GetYouTubeChannelId();
                if (string.IsNullOrWhiteSpace(channelId))
                    return Ok(Array.Empty<CustomForm>());

                var forms = await _formsService.GetActiveFormsAsync(channelId);
                
                // Return forms without responses for privacy
                var publicForms = forms.Select(f => f with { Responses = Array.Empty<CustomFormResponse>() }).ToList();
                return Ok(publicForms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active custom forms");
                return StatusCode(500, "An error occurred while fetching active custom forms");
            }
        }

        /// <summary>
        /// Get custom form by URL (questions only, no responses for privacy)
        /// </summary>
        [HttpGet("url/{url}")]
        [OutputCache(Tags = [CacheKeys.CustomForms], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> GetByUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return BadRequest("URL cannot be empty");
                }

                var channelId = GetYouTubeChannelId();
                if (string.IsNullOrWhiteSpace(channelId))
                    return NotFound($"Custom form with URL '{url}' not found");

                var form = await _formsService.GetFormByUrlAsync(url, channelId);
                if (form == null)
                {
                    return NotFound($"Custom form with URL '{url}' not found");
                }

                // Check if form is active
                if (form.EffectiveLifecycle != CustomFormLifecycle.Online || form.EffectiveAccessMode != CustomFormAccessMode.Direct)
                {
                    return NotFound($"Custom form with URL '{url}' not found");
                }

                // Return form without responses for privacy
                var publicForm = form with { Responses = Array.Empty<CustomFormResponse>() };
                return Ok(publicForm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom form by URL: {Url}", url);
                return StatusCode(500, "An error occurred while fetching the custom form");
            }
        }

        /// <summary>
        /// Submit a response to a form (anonymous)
        /// </summary>
        [HttpPost("{id}/responses")]
        public async Task<IActionResult> SubmitResponse(BaseRequestId<SubmitFormResponseRequest> request)
        {
            try
            {
                // Check if form exists
                var channelId = GetYouTubeChannelId();
                if (string.IsNullOrWhiteSpace(channelId))
                    return NotFound($"Custom form with ID '{request.Id}' not found");

                var form = await _formsService.GetFormByIdAsync(request.Id, channelId);
                if (form == null)
                {
                    return NotFound($"Custom form with ID '{request.Id}' not found");
                }

                // Check if form is active
                if (form.EffectiveLifecycle != CustomFormLifecycle.Online)
                {
                    return BadRequest("This form is not currently accepting responses");
                }

                if (form.EffectiveAccessMode == CustomFormAccessMode.SurveyOnly
                    && (string.IsNullOrWhiteSpace(request.Body.SurveyId)
                        || !await _surveyService.CanSubmitAsync(request.Body.SurveyId, form.Id, channelId, DateTime.UtcNow)))
                {
                    return BadRequest("This form can only be submitted through an active survey");
                }

                // Validate answers match questions
                if (request.Body.Answers.Length != form.Questions.Length)
                {
                    return BadRequest("Number of answers must match number of questions");
                }

                // Validate each answer matches its question type
                for (int i = 0; i < form.Questions.Length; i++)
                {
                    var question = form.Questions[i];
                    var answer = request.Body.Answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

                    if (answer == null)
                    {
                        return BadRequest($"Missing answer for question '{question.QuestionText}'");
                    }

                    // Validate answer type matches question type
                    if (question is OpenQuestion && answer is not OpenAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects an open text answer");
                    }
                    else if (question is MultipleChoiceQuestion mcq && answer is not MultipleChoiceAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects a multiple choice answer");
                    }
                    else if (question is SingleChoiceQuestion scq && answer is not SingleChoiceAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects a single choice answer");
                    }
                    else if (question is BooleanQuestion && answer is not BooleanAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects a true/false answer");
                    }
                    else if (question is EmailQuestion && answer is not EmailAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects an email answer");
                    }

                    // Validate required questions are answered
                    if (question.IsRequired)
                    {
                        if (answer is OpenAnswer oa && string.IsNullOrWhiteSpace(oa.TextResponse))
                        {
                            return BadRequest($"Question '{question.QuestionText}' is required");
                        }
                        else if (answer is MultipleChoiceAnswer mca && mca.SelectedOptionIds.Length == 0)
                        {
                            return BadRequest($"Question '{question.QuestionText}' is required");
                        }
                        else if (answer is SingleChoiceAnswer sca && string.IsNullOrWhiteSpace(sca.SelectedOptionId))
                        {
                            return BadRequest($"Question '{question.QuestionText}' is required");
                        }
                        else if (answer is EmailAnswer ea && string.IsNullOrWhiteSpace(ea.Email))
                        {
                            return BadRequest($"Question '{question.QuestionText}' is required");
                        }
                    }

                    // Validate selected options exist for choice questions
                    if (question is MultipleChoiceQuestion mcQuestion && answer is MultipleChoiceAnswer mcAnswer)
                    {
                        var validOptionIds = mcQuestion.Options.Select(o => o.OptionId).ToHashSet();
                        var invalidOptions = mcAnswer.SelectedOptionIds.Where(id => !validOptionIds.Contains(id)).ToArray();
                        if (invalidOptions.Length > 0)
                        {
                            return BadRequest($"Invalid option IDs for question '{question.QuestionText}': {string.Join(", ", invalidOptions)}");
                        }
                    }
                    else if (question is SingleChoiceQuestion scQuestion && answer is SingleChoiceAnswer scAnswer)
                    {
                        var validOptionIds = scQuestion.Options.Select(o => o.OptionId).ToHashSet();
                        if (!string.IsNullOrWhiteSpace(scAnswer.SelectedOptionId) && !validOptionIds.Contains(scAnswer.SelectedOptionId))
                        {
                            return BadRequest($"Invalid option ID for question '{question.QuestionText}': {scAnswer.SelectedOptionId}");
                        }
                    }
                }

                var response = new CustomFormResponse(
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow,
                    request.Body.Answers
                );

                await _formsService.AddResponseAsync(request.Id, response, channelId);

                _logger.LogInformation("Form response submitted for form: {FormId}", request.Id);

                return Ok(new { message = "Response submitted successfully", responseId = response.ResponseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting form response for form ID: {Id}", request.Id);
                return StatusCode(500, "An error occurred while submitting the response");
            }
        }

        private string? GetYouTubeChannelId()
        {
            var channelId = _configuration["YouTubeChannelId"]?.Trim();
            if (string.IsNullOrWhiteSpace(channelId))
                _logger.LogError("Public custom forms endpoint is missing the YouTubeChannelId configuration");

            return channelId;
        }
    }
}

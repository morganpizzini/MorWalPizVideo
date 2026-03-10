using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers
{
    public class CreateCustomFormRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        public CustomFormQuestion[] Questions { get; set; } = [];
        
        public bool Active { get; set; } = true;
    }

    public class UpdateCustomFormRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        public CustomFormQuestion[] Questions { get; set; } = [];
        
        public bool Active { get; set; } = true;
    }

    public class SubmitFormResponseRequest
    {
        [Required]
        public CustomFormAnswer[] Answers { get; set; } = [];
    }

    public class CustomFormsController : ApplicationControllerBase
    {
        private readonly DataService _dataService;
        private readonly ILogger<CustomFormsController> _logger;

        public CustomFormsController(
            DataService dataService,
            ILogger<CustomFormsController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Get all custom forms
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IList<CustomForm>>> Fetch()
        {
            try
            {
                var forms = await _dataService.Fetch();
                return Ok(forms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom forms");
                return StatusCode(500, "An error occurred while fetching custom forms");
            }
        }

        /// <summary>
        /// Get custom form by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomForm>> GetById(BaseRequestId request)
        {
            try
            {

                var form = await _dataService.GetCustomFormById(request.Id);
                if (form == null)
                {
                    return NotFound($"Custom form with ID '{request.Id}' not found");
                }

                return Ok(form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom form by ID: {Id}", request.Id);
                return StatusCode(500, "An error occurred while fetching the custom form");
            }
        }

        /// <summary>
        /// Create a new custom form
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CustomForm>> Create(BaseRequest<CreateCustomFormRequest> request)
        {
            try
            {
                // Validate questions
                if (request.Body.Questions.Length == 0)
                {
                    return BadRequest("At least one question is required");
                }

                // Validate each question
                foreach (var question in request.Body.Questions)
                {
                    if (string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        return BadRequest("Question text cannot be empty");
                    }

                    // Validate choice questions have options
                    if (question is MultipleChoiceQuestion mcq)
                    {
                        if (mcq.Options.Length == 0)
                        {
                            return BadRequest($"Multiple choice question '{question.QuestionText}' must have at least one option");
                        }
                    }
                    else if (question is SingleChoiceQuestion scq)
                    {
                        if (scq.Options.Length == 0)
                        {
                            return BadRequest($"Single choice question '{question.QuestionText}' must have at least one option");
                        }
                    }
                }

                var form = new CustomForm(
                    request.Body.Title,
                    request.Body.Description,
                    request.Body.Url,
                    request.Body.Questions,
                    request.Body.Active
                );

                await _dataService.SaveCustomForm(form);
                
                _logger.LogInformation("Custom form created: {Id} - {Title}", form.Id, form.Title);
                
                return CreatedAtAction(nameof(GetById), new { id = form.Id }, form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom form");
                return StatusCode(500, "An error occurred while creating the custom form");
            }
        }

        /// <summary>
        /// Update an existing custom form
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(BaseRequestId<UpdateCustomFormRequest> request)
        {
            try
            {
                // Check if form exists
                var existingForm = await _dataService.GetCustomFormById(request.Id);
                if (existingForm == null)
                {
                    return NotFound($"Custom form with ID '{request.Id}' not found");
                }

                // Validate questions
                if (request.Body.Questions.Length == 0)
                {
                    return BadRequest("At least one question is required");
                }

                // Validate each question
                foreach (var question in request.Body.Questions)
                {
                    if (string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        return BadRequest("Question text cannot be empty");
                    }

                    // Validate choice questions have options
                    if (question is MultipleChoiceQuestion mcq)
                    {
                        if (mcq.Options.Length == 0)
                        {
                            return BadRequest($"Multiple choice question '{question.QuestionText}' must have at least one option");
                        }
                    }
                    else if (question is SingleChoiceQuestion scq)
                    {
                        if (scq.Options.Length == 0)
                        {
                            return BadRequest($"Single choice question '{question.QuestionText}' must have at least one option");
                        }
                    }
                }

                var updatedForm = existingForm with
                {
                    Title = request.Body.Title,
                    Description = request.Body.Description,
                    Url = request.Body.Url,
                    Questions = request.Body.Questions,
                    Active = request.Body.Active
                };

                await _dataService.UpdateCustomForm(updatedForm);
                
                _logger.LogInformation("Custom form updated: {Id} - {Title}", request.Id, updatedForm.Title);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating custom form with ID: {Id}", request.Id);
                return StatusCode(500, "An error occurred while updating the custom form");
            }
        }

        /// <summary>
        /// Delete a custom form
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(BaseRequestId request)
        {
            try
            {
                // Check if form exists
                var existingForm = await _dataService.GetCustomFormById(request.Id);
                if (existingForm == null)
                {
                    return NotFound($"Custom form with ID '{request.Id}' not found");
                }

                await _dataService.DeleteCustomForm(request.Id);
                
                _logger.LogInformation("Custom form deleted: {Id}", request.Id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom form with ID: {Id}", request.Id);
                return StatusCode(500, "An error occurred while deleting the custom form");
            }
        }

        /// <summary>
        /// Submit a response to a form (anonymous)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id}/responses")]
        public async Task<ActionResult> SubmitResponse(string id, [FromBody] SubmitFormResponseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("Form ID cannot be empty");
                }

                // Check if form exists
                var form = await _dataService.GetCustomFormById(id);
                if (form == null)
                {
                    return NotFound($"Custom form with ID '{id}' not found");
                }

                // Check if form is active
                if (!form.Active)
                {
                    return BadRequest("This form is not currently accepting responses");
                }

                // Validate answers match questions
                if (request.Answers.Length != form.Questions.Length)
                {
                    return BadRequest("Number of answers must match number of questions");
                }

                // Validate each answer matches its question type
                for (int i = 0; i < form.Questions.Length; i++)
                {
                    var question = form.Questions[i];
                    var answer = request.Answers[i];

                    if (answer.QuestionId != question.QuestionId)
                    {
                        return BadRequest($"Answer question ID mismatch at position {i}");
                    }

                    // Validate answer type matches question type
                    if (question is OpenQuestion && answer is not OpenAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects an open text answer");
                    }
                    else if (question is MultipleChoiceQuestion && answer is not MultipleChoiceAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects a multiple choice answer");
                    }
                    else if (question is SingleChoiceQuestion && answer is not SingleChoiceAnswer)
                    {
                        return BadRequest($"Question '{question.QuestionText}' expects a single choice answer");
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
                    }
                }

                var response = new CustomFormResponse(
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow,
                    request.Answers
                );

                await _dataService.AddFormResponse(id, response);
                
                _logger.LogInformation("Form response submitted for form: {FormId}", id);
                
                return Ok(new { message = "Response submitted successfully", responseId = response.ResponseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting form response for form ID: {Id}", id);
                return StatusCode(500, "An error occurred while submitting the response");
            }
        }

        /// <summary>
        /// Get all responses for a form
        /// </summary>
        [HttpGet("{id}/responses")]
        public async Task<ActionResult<CustomFormResponse[]>> GetResponses(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("Form ID cannot be empty");
                }

                var form = await _dataService.GetCustomFormById(id);
                if (form == null)
                {
                    return NotFound($"Custom form with ID '{id}' not found");
                }

                return Ok(form.Responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching responses for form ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching form responses");
            }
        }
    }
}

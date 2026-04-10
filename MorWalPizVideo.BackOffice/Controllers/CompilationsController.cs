using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers
{
    public class CreateCompilationRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        public string[] Videos { get; set; } = [];
    }

    public class UpdateCompilationRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        public string[] Videos { get; set; } = [];
    }

    public class CompilationsController : ApplicationControllerBase
    {
        private readonly DataService _dataService;
        private readonly ILogger<CompilationsController> _logger;

        public CompilationsController(
            DataService dataService,
            ILogger<CompilationsController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Get all compilations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var compilations = await _dataService.GetCompilations();
                return Ok(compilations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching compilations");
                return StatusCode(500, "An error occurred while fetching compilations");
            }
        }

        /// <summary>
        /// Get compilation by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("ID cannot be empty");
                }

                var compilation = await _dataService.GetCompilationById(id);
                if (compilation == null)
                {
                    return NotFound($"Compilation with ID '{id}' not found");
                }

                return Ok(compilation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching compilation by ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the compilation");
            }
        }

        /// <summary>
        /// Create a new compilation
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(BaseRequest<CreateCompilationRequest> request)
        {
            try
            {
                // Validate all VideoRefs exist and fetch their data
                VideoRef[] videoRefs = [];
                if (request.Body.Videos.Length > 0)
                {
                    var invalidVideos = new List<string>();
                    var foundVideoRefs = new List<VideoRef>();
                    var allMatches = await _dataService.FetchMatches();

                    foreach (var youtubeId in request.Body.Videos)
                    {
                        // Try to find the video in matches
                        VideoRef? foundVideo = null;
                        
                        foreach (var match in allMatches)
                        {
                            // Check if it's the thumbnail video
                            if (match.ThumbnailVideoId == youtubeId)
                            {
                                // Create a VideoRef from thumbnail data if available
                                foundVideo = match.VideoRefs.FirstOrDefault(vr => vr.YoutubeId == youtubeId);
                                if (foundVideo == null)
                                {
                                    foundVideo = new VideoRef(youtubeId, Array.Empty<CategoryRef>());
                                }
                                break;
                            }
                            
                            // Check in VideoRefs
                            foundVideo = match.VideoRefs.FirstOrDefault(vr => vr.YoutubeId == youtubeId);
                            if (foundVideo != null)
                            {
                                break;
                            }
                        }

                        if (foundVideo != null)
                        {
                            foundVideoRefs.Add(foundVideo);
                        }
                        else
                        {
                            invalidVideos.Add(youtubeId);
                        }
                    }

                    if (invalidVideos.Count > 0)
                    {
                        return BadRequest($"The following videos do not exist: {string.Join(", ", invalidVideos)}");
                    }

                    videoRefs = foundVideoRefs.ToArray();
                }

                var compilation = new Compilation(
                    request.Body.Title,
                    request.Body.Description,
                    request.Body.Url,
                    videoRefs
                );

                compilation = await _dataService.SaveCompilation(compilation);
                
                _logger.LogInformation("Compilation created: {Id} - {Title}", compilation.Id, compilation.Title);
                
                return Created($"/api/Compilations/{compilation.Id}", compilation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compilation");
                return StatusCode(500, "An error occurred while creating the compilation");
            }
        }

        /// <summary>
        /// Update an existing compilation
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(BaseRequestId<UpdateCompilationRequest> request)
        {
            try
            {
                // Check if compilation exists
                var existingCompilation = await _dataService.GetCompilationById(request.Id);
                if (existingCompilation == null)
                {
                    return NotFound($"Compilation with ID '{request.Id}' not found");
                }

                // Validate all VideoRefs exist and fetch their data
                VideoRef[] videoRefs = [];
                if (request.Body.Videos.Length > 0)
                {
                    var invalidVideos = new List<string>();
                    var foundVideoRefs = new List<VideoRef>();
                    var allMatches = await _dataService.FetchMatches();

                    foreach (var youtubeId in request.Body.Videos)
                    {
                        // Try to find the video in matches
                        VideoRef? foundVideo = null;
                        
                        foreach (var match in allMatches)
                        {
                            // Check if it's the thumbnail video
                            if (match.ThumbnailVideoId == youtubeId)
                            {
                                // Create a VideoRef from thumbnail data if available
                                foundVideo = match.VideoRefs.FirstOrDefault(vr => vr.YoutubeId == youtubeId);
                                if (foundVideo == null)
                                {
                                    foundVideo = new VideoRef(youtubeId, Array.Empty<CategoryRef>());
                                }
                                break;
                            }
                            
                            // Check in VideoRefs
                            foundVideo = match.VideoRefs.FirstOrDefault(vr => vr.YoutubeId == youtubeId);
                            if (foundVideo != null)
                            {
                                break;
                            }
                        }

                        if (foundVideo != null)
                        {
                            foundVideoRefs.Add(foundVideo);
                        }
                        else
                        {
                            invalidVideos.Add(youtubeId);
                        }
                    }

                    if (invalidVideos.Count > 0)
                    {
                        return BadRequest($"The following videos do not exist: {string.Join(", ", invalidVideos)}");
                    }

                    videoRefs = foundVideoRefs.ToArray();
                }

                var updatedCompilation = existingCompilation with
                {
                    Title = request.Body.Title,
                    Description = request.Body.Description,
                    Url = request.Body.Url,
                    Videos = videoRefs
                };

                await _dataService.UpdateCompilation(updatedCompilation);
                
                _logger.LogInformation("Compilation updated: {Id} - {Title}", request.Id, updatedCompilation.Title);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating compilation with ID: {Id}", request.Id);
                return StatusCode(500, "An error occurred while updating the compilation");
            }
        }

        /// <summary>
        /// Delete a compilation
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(BaseRequestId request)
        {
            try
            {
                // Check if compilation exists
                var existingCompilation = await _dataService.GetCompilationById(request.Id);
                if (existingCompilation == null)
                {
                    return NotFound($"Compilation with ID '{request.Id}' not found");
                }

                await _dataService.DeleteCompilation(request.Id);
                
                _logger.LogInformation("Compilation deleted: {Id}", request.Id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting compilation with ID: {Id}", request.Id);
                return StatusCode(500, "An error occurred while deleting the compilation");
            }
        }
    }
}

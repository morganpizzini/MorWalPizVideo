using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
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

    [RequireChannelScope]
    public class CompilationsController : ApplicationControllerBase
    {
        private readonly ICatalogService _catalogService;
        private readonly IContentService _contentService;
        private readonly ILogger<CompilationsController> _logger;
        private readonly IVideoAuthorizationService _videoAuthorization;

        public CompilationsController(
            ICatalogService catalogService,
            IContentService contentService,
            ILogger<CompilationsController> logger,
            IVideoAuthorizationService videoAuthorization)
        {
            _catalogService = catalogService;
            _contentService = contentService;
            _logger = logger;
            _videoAuthorization = videoAuthorization;
        }

        /// <summary>
        /// Get all compilations
        /// </summary>
        [HttpGet]
        [AllowUser(AuthorizationPermissionKeys.CompilationsView, AuthorizationPermissionKeys.CompilationsManage)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var channelId = HttpContext.GetChannelContext().ChannelId;
                var compilations = (await _catalogService.GetCompilationsAsync())
                    .Where(compilation => compilation.ChannelId == channelId);
                return Ok(compilations.Select(ContractUtils.Convert));
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
        [AllowUser(AuthorizationPermissionKeys.CompilationsView, AuthorizationPermissionKeys.CompilationsManage)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("ID cannot be empty");
                }

                var compilation = await _catalogService.GetCompilationByIdAsync(id);
                if (compilation == null || compilation.ChannelId != HttpContext.GetChannelContext().ChannelId)
                {
                    return NotFound($"Compilation with ID '{id}' not found");
                }

                return Ok(ContractUtils.Convert(compilation));
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
        [AllowUser(AuthorizationPermissionKeys.CompilationsCreate, AuthorizationPermissionKeys.CompilationsManage)]
        public async Task<IActionResult> Create(BaseRequest<CreateCompilationRequest> request)
        {
            try
            {
                // Validate all VideoRefs exist and fetch their data
                var channelId = HttpContext.GetChannelContext().ChannelId;
                var normalizedUrl = Compilation.NormalizeUrl(request.Body.Url);
                if (await _catalogService.GetCompilationByUrlAsync(normalizedUrl) is not null)
                {
                    return Conflict("A compilation with this URL already exists");
                }
                VideoRef[] videoRefs = [];
                if (request.Body.Videos.Length > 0)
                {
                    var invalidVideos = new List<string>();
                    var foundVideoRefs = new List<VideoRef>();
                    var allMatches = await _contentService.GetAllMatchesAsync();

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

                    foreach (var videoRef in foundVideoRefs)
                    {
                        var source = allMatches.FirstOrDefault(match => match.VideoRefs.Any(video => video.YoutubeId == videoRef.YoutubeId));
                        if (source is null || !await _videoAuthorization.CanAccessAsync(User, source))
                        {
                            invalidVideos.Add(videoRef.YoutubeId);
                        }
                    }

                    if (invalidVideos.Count > 0)
                    {
                        return BadRequest($"The following videos are not readable in the selected channel: {string.Join(", ", invalidVideos.Distinct())}");
                    }

                    videoRefs = foundVideoRefs.ToArray();
                }

                var compilation = new Compilation(
                    request.Body.Title,
                    request.Body.Description,
                    normalizedUrl,
                    videoRefs
                );
                compilation = compilation with { ChannelId = channelId };

                compilation = await _catalogService.SaveCompilationAsync(compilation);
                
                _logger.LogInformation("Compilation created: {Id} - {Title}", compilation.Id, compilation.Title);
                
                return Created($"/api/Compilations/{compilation.Id}", ContractUtils.Convert(compilation));
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
        [AllowUser(AuthorizationPermissionKeys.CompilationsUpdate, AuthorizationPermissionKeys.CompilationsManage)]
        public async Task<IActionResult> Update(BaseRequestId<UpdateCompilationRequest> request)
        {
            try
            {
                // Check if compilation exists
                var existingCompilation = await _catalogService.GetCompilationByIdAsync(request.Id);
                if (existingCompilation == null || existingCompilation.ChannelId != HttpContext.GetChannelContext().ChannelId)
                {
                    return NotFound($"Compilation with ID '{request.Id}' not found");
                }

                // Validate all VideoRefs exist and fetch their data
                var channelId = HttpContext.GetChannelContext().ChannelId;
                var normalizedUrl = Compilation.NormalizeUrl(request.Body.Url);
                var duplicateUrl = await _catalogService.GetCompilationByUrlAsync(normalizedUrl);
                if (duplicateUrl is not null && duplicateUrl.Id != existingCompilation.Id)
                {
                    return Conflict("A compilation with this URL already exists");
                }
                VideoRef[] videoRefs = [];
                if (request.Body.Videos.Length > 0)
                {
                    var invalidVideos = new List<string>();
                    var foundVideoRefs = new List<VideoRef>();
                    var allMatches = await _contentService.GetAllMatchesAsync();

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

                    foreach (var videoRef in foundVideoRefs)
                    {
                        var source = allMatches.FirstOrDefault(match => match.VideoRefs.Any(video => video.YoutubeId == videoRef.YoutubeId));
                        if (source is null || !await _videoAuthorization.CanAccessAsync(User, source))
                        {
                            invalidVideos.Add(videoRef.YoutubeId);
                        }
                    }

                    if (invalidVideos.Count > 0)
                    {
                        return BadRequest($"The following videos are not readable in the selected channel: {string.Join(", ", invalidVideos.Distinct())}");
                    }

                    videoRefs = foundVideoRefs.ToArray();
                }

                var updatedCompilation = existingCompilation with
                {
                    Title = request.Body.Title,
                    Description = request.Body.Description,
                    Url = normalizedUrl,
                    Videos = videoRefs,
                    ChannelId = channelId
                };

                await _catalogService.UpdateCompilationAsync(updatedCompilation);
                
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
        [AllowUser(AuthorizationPermissionKeys.CompilationsDelete, AuthorizationPermissionKeys.CompilationsManage)]
        public async Task<IActionResult> Delete(BaseRequestId request)
        {
            try
            {
                // Check if compilation exists
                var existingCompilation = await _catalogService.GetCompilationByIdAsync(request.Id);
                if (existingCompilation == null || existingCompilation.ChannelId != HttpContext.GetChannelContext().ChannelId)
                {
                    return NotFound($"Compilation with ID '{request.Id}' not found");
                }

                await _catalogService.DeleteCompilationAsync(request.Id);
                
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

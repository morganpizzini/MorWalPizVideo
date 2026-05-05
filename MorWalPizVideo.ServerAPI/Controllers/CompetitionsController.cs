using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompetitionsController : ApplicationController
    {
        private readonly ICompetitionRepository _competitionRepository;

        public CompetitionsController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache,
            ICompetitionRepository competitionRepository) : base(dataService, memoryCache)
        {
            _competitionRepository = competitionRepository;
        }

        /// <summary>
        /// Get all competitions
        /// </summary>
        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Competitions])]
        public async Task<IActionResult> GetAll()
        {
            var competitions = await cache.GetOrCreateAsync(
                CacheKeys.Competitions,
                async () => await _competitionRepository.GetItemsAsync());
            return Ok(competitions);
        }

        /// <summary>
        /// Get competitions by status
        /// </summary>
        [HttpGet("status/{status}")]
        [OutputCache(Tags = [CacheKeys.Competitions], VaryByRouteValueNames = ["status"])]
        public async Task<IActionResult> GetByStatus(CompetitionStatus status)
        {
            var competitions = await _competitionRepository.GetItemsAsync(c => c.Status == status);
            return Ok(competitions);
        }

        /// <summary>
        /// Get competition by ID
        /// </summary>
        [HttpGet("{id}")]
        [OutputCache(Tags = [CacheKeys.Competitions], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetById(string id)
        {
            var competition = await _competitionRepository.GetItemAsync(id);
            if (competition == null)
                return NotFound(new { message = $"Competition with ID {id} not found" });

            return Ok(competition);
        }

        /// <summary>
        /// Create a new competition
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Competition competition)
        {
            if (competition == null)
                return BadRequest(new { message = "Competition data is required" });

            if (string.IsNullOrWhiteSpace(competition.Name))
                return BadRequest(new { message = "Competition name is required" });

            try
            {
                // Check for duplicate name
                var existing = await _competitionRepository.GetItemsAsync(c => c.Name == competition.Name);
                if (existing.Any())
                    return BadRequest(new { message = $"Competition with name '{competition.Name}' already exists" });

                await _competitionRepository.AddItemAsync(competition);
                
                // Invalidate cache
                await cache.RemoveAsync(CacheKeys.Competitions);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = competition.Id },
                    competition);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing competition
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Competition competition)
        {
            if (competition == null)
                return BadRequest(new { message = "Competition data is required" });

            if (id != competition.Id)
                return BadRequest(new { message = "ID mismatch" });

            try
            {
                // Check if competition exists
                var existing = await _competitionRepository.GetItemAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Competition with ID {id} not found" });

                existing.StartDate = competition.StartDate;
                existing.EndDate = competition.EndDate;
                existing.Name = competition.Name;
                existing.Description = competition.Description;
                existing.Location   = competition.Location;
                existing.Status = competition.Status;
                existing.Rules = competition.Rules;
                existing.ImageUrl = competition.ImageUrl;
                existing.MaxParticipants = competition.MaxParticipants;
                existing.OrganizerId = competition.OrganizerId;
                existing.WebsiteUrl = competition.WebsiteUrl;
                existing.RegistrationDeadline = competition.RegistrationDeadline;
                existing.Stages = existing.Stages;


                await _competitionRepository.UpdateItemAsync(existing);
                
                // Invalidate cache
                await cache.RemoveAsync(CacheKeys.Competitions);

                return Ok(competition);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a competition
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                // Check if competition exists
                var existing = await _competitionRepository.GetItemAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Competition with ID {id} not found" });

                await _competitionRepository.DeleteItemAsync(id);
                
                // Invalidate cache
                await cache.RemoveAsync(CacheKeys.Competitions);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get competition statistics
        /// </summary>
        [HttpGet("stats")]
        [OutputCache(Tags = [CacheKeys.Competitions], Duration = 300)] // 5 minutes cache
        public async Task<IActionResult> GetStatistics()
        {
            var competitions = await _competitionRepository.GetItemsAsync();

            var stats = new
            {
                TotalCompetitions = competitions.Count,
                ByStatus = competitions.GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToList(),
                UpcomingCompetitions = competitions
                    .Count(c => c.StartDate > DateTime.UtcNow && 
                               (c.Status == CompetitionStatus.Published || 
                                c.Status == CompetitionStatus.RegistrationOpen)),
                OngoingCompetitions = competitions
                    .Count(c => c.Status == CompetitionStatus.InProgress),
                CompletedCompetitions = competitions
                    .Count(c => c.Status == CompetitionStatus.Completed),
                AverageStagesPerCompetition = competitions
                    .Where(c => c.Stages.Any())
                    .Average(c => (double?)c.Stages.Count) ?? 0,
                CompetitionsWithRegistrationOpen = competitions
                    .Count(c => c.Status == CompetitionStatus.RegistrationOpen)
            };

            return Ok(stats);
        }

        /// <summary>
        /// Get upcoming competitions (published or registration open, start date in future)
        /// </summary>
        [HttpGet("upcoming")]
        [OutputCache(Tags = [CacheKeys.Competitions], Duration = 60)] // 1 minute cache
        public async Task<IActionResult> GetUpcoming()
        {
            var competitions = await _competitionRepository.GetItemsAsync();
            
            var upcoming = competitions
                .Where(c => c.StartDate > DateTime.UtcNow && 
                           (c.Status == CompetitionStatus.Published || 
                            c.Status == CompetitionStatus.RegistrationOpen ||
                            c.Status == CompetitionStatus.RegistrationClosed))
                .OrderBy(c => c.StartDate)
                .ToList();

            return Ok(upcoming);
        }

        /// <summary>
        /// Get active competitions (registration open or in progress)
        /// </summary>
        [HttpGet("active")]
        [OutputCache(Tags = [CacheKeys.Competitions], Duration = 60)] // 1 minute cache
        public async Task<IActionResult> GetActive()
        {
            var competitions = await _competitionRepository.GetItemsAsync();
            
            var active = competitions
                .Where(c => c.Status == CompetitionStatus.RegistrationOpen || 
                           c.Status == CompetitionStatus.InProgress)
                .OrderBy(c => c.StartDate)
                .ToList();

            return Ok(active);
        }

        /// <summary>
        /// Search competitions by name or location
        /// </summary>
        [HttpGet("search")]
        [OutputCache(Tags = [CacheKeys.Competitions], VaryByQueryKeys = ["q"])]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required" });

            var competitions = await _competitionRepository.GetItemsAsync();
            
            var results = competitions
                .Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                           (c.Location != null && c.Location.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.StartDate)
                .ToList();

            return Ok(results);
        }
    }
}
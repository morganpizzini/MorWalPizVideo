using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using MongoDB.Driver;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Utils;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class ConfigTestController : ControllerBase
    {
        private readonly IFeatureManager _featureManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IOptions<MorWalPizDatabaseSettings> _dbSettings;
        private readonly IMongoDbService? _mongoDbService;

        public ConfigTestController(
            IFeatureManager featureManager,
            IOptions<MorWalPizDatabaseSettings> dbSettings,
            IWebHostEnvironment environment,
            IMongoDbService? mongoDbService = null)
        {
            _featureManager = featureManager;
            _environment = environment;
            _dbSettings = dbSettings;
            _mongoDbService = mongoDbService;
        }

        [HttpGet("test-config")]
        public async Task<IActionResult> TestConfiguration()
        {
            // Check if EnableDev feature flag is enabled for security
            if (!_environment.IsDevelopment() || !await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableDev))
            {
                return NotFound();
            }

            try
            {
                var settings = _dbSettings.Value;
                var featureCors = await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableCors);
                var enableCache = await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableCache);
                var enableOutputCache = await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableOutputCache);
                var result = new
                {
                    Success = true,
                    CorsEnabled = featureCors,
                    EnableOutputCache = enableOutputCache,
                    EnableCache = enableCache,
                    ConfigurationLoaded = !string.IsNullOrEmpty(settings?.ConnectionString),
                    HasConnectionString = !string.IsNullOrEmpty(settings?.ConnectionString),
                    HasDatabaseName = !string.IsNullOrEmpty(settings?.DatabaseName)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Error = "Configuration check failed.",
                    Type = ex.GetType().Name
                });
            }
        }

        [HttpGet("test-database")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            // Check if EnableDev feature flag is enabled for security
            if (!_environment.IsDevelopment() || !await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableDev))
            {
                return NotFound();
            }

            try
            {
                if (_mongoDbService is null)
                    return Ok(new { Success = true, Message = "Mock scenario provider is active" });

                var database = _mongoDbService.GetDatabase();

                // Try to ping the database
                await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));

                return Ok(new
                {
                    Success = true,
                    Message = "Database connection successful"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Error = "Database check failed.",
                    Type = ex.GetType().Name
                });
            }
        }
    }
}

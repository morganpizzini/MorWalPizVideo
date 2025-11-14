using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using MongoDB.Driver;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Utils;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigTestController : ControllerBase
    {
        private readonly IFeatureManager _featureManager;
        private readonly IOptions<MorWalPizDatabaseSettings> _dbSettings;
        private readonly IMongoDbService _mongoDbService;

        public ConfigTestController(
            IFeatureManager featureManager,
            IOptions<MorWalPizDatabaseSettings> dbSettings,
            IMongoDbService mongoDbService)
        {
            _featureManager = featureManager;
            _dbSettings = dbSettings;
            _mongoDbService = mongoDbService;
        }

        [HttpGet("test-config")]
        public async Task<IActionResult> TestConfiguration()
        {
            // Check if EnableDev feature flag is enabled for security
            if (!await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableDev))
            {
                return NotFound();
            }

            try
            {
                var settings = _dbSettings.Value;

                var result = new
                {
                    Success = true,
                    ConfigurationLoaded = !string.IsNullOrEmpty(settings?.ConnectionString),
                    HasConnectionString = !string.IsNullOrEmpty(settings?.ConnectionString),
                    HasDatabaseName = !string.IsNullOrEmpty(settings?.DatabaseName),
                    ConnectionStringPrefix = settings?.ConnectionString?.Substring(0, Math.Min(20, settings.ConnectionString?.Length ?? 0)),
                    DatabaseName = settings?.DatabaseName
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name
                });
            }
        }

        [HttpGet("test-database")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            // Check if EnableDev feature flag is enabled for security
            if (!await _featureManager.IsEnabledAsync(MyFeatureFlags.EnableDev))
            {
                return NotFound();
            }

            try
            {
                var database = _mongoDbService.GetDatabase();

                // Try to ping the database
                await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));

                return Ok(new
                {
                    Success = true,
                    DatabaseName = database.DatabaseNamespace.DatabaseName,
                    Message = "Database connection successful"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Error = ex.Message,
                    Type = ex.GetType().Name
                });
            }
        }
    }
}

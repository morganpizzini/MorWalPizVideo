using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class ConfigurationController : ApplicationController
    {
        public ConfigurationController(
                IGenericDataService _dataService, IExternalDataService _extDataService, IMorWalPizCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet("stream")]
        [OutputCache(Tags = [CacheKeys.ConfigurationStream])]
        public async Task<IActionResult> FetchConfiguration()
        {
            try
            {
                var configurationList = await dataService.FetchConfigurationByKeys([
                    ConfigurationKeys.StreamUrl,
                    ConfigurationKeys.StreamVideoPath,
                    ConfigurationKeys.StreamChatPath,
                    ConfigurationKeys.StreamEnable,
                    ConfigurationKeys.StreamImagePlaceholder]);

                var configuration = configurationList.ToDictionary(x => x.Key, x => x.Value);

                // Check if StreamUrl exists before accessing it
                if (configuration.TryGetValue(ConfigurationKeys.StreamUrl, out var streamUrlValue) && 
                    !string.IsNullOrEmpty(streamUrlValue?.ToString()))
                {
                    var streamUri = new Uri(streamUrlValue.ToString()!);
                    
                    // Safely update video path if it exists
                    if (configuration.TryGetValue(ConfigurationKeys.StreamVideoPath, out var videoPathValue) && 
                        !string.IsNullOrEmpty(videoPathValue?.ToString()))
                    {
                        configuration[ConfigurationKeys.StreamVideoPath] = new Uri(streamUri, videoPathValue.ToString()).ToString();
                    }
                    
                    // Safely update chat path if it exists
                    if (configuration.TryGetValue(ConfigurationKeys.StreamChatPath, out var chatPathValue) && 
                        !string.IsNullOrEmpty(chatPathValue?.ToString()))
                    {
                        configuration[ConfigurationKeys.StreamChatPath] = new Uri(streamUri, chatPathValue.ToString()).ToString();
                    }
                }

                return Ok(configuration);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                // You may want to inject ILogger<ConfigurationController> for proper logging
                return StatusCode(500, new { error = "Failed to fetch stream configuration", message = ex.Message });
            }
        }
    }
}

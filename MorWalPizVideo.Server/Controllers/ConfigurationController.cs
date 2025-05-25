using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class ConfigurationController : ApplicationController
    {

        public ConfigurationController(
                DataService _dataService, IExternalDataService _extDataService, IMorWalPizCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet("stream")]
        [OutputCache(Tags = [CacheKeys.ConfigurationStream])]
        public async Task<IActionResult> FetchConfiguration()
        {
            var configuration = (await dataService.FetchConfigurationByKeys([
                ConfigurationKeys.StreamUrl, 
                ConfigurationKeys.StreamVideoPath, 
                ConfigurationKeys.StreamChatPath,
                ConfigurationKeys.StreamEnable,
                ConfigurationKeys.StreamImagePlaceholder])).ToDictionary(x=>x.Key,x=>x.Value);

            var streamUri = new Uri(configuration[ConfigurationKeys.StreamUrl].ToString() ?? string.Empty);
            if (!string.IsNullOrEmpty(streamUri.ToString()))
            {
                configuration[ConfigurationKeys.StreamVideoPath] = new Uri(streamUri, configuration[ConfigurationKeys.StreamVideoPath].ToString()).ToString();
                configuration[ConfigurationKeys.StreamChatPath] = new Uri(streamUri, configuration[ConfigurationKeys.StreamChatPath].ToString()).ToString();
            }


            return Ok(configuration);
        }
    }
}

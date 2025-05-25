using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.Threading.Tasks;

namespace MorWalPizVideo.BackOffice.Controllers
{
  public class ConfigurationController : ApplicationController
  {
    private readonly DataService _dataService;

    public ConfigurationController(DataService dataService)
    {
      _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfigurations()
    {
      var configurations = await _dataService.GetConfigurations();
      return Ok(configurations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetConfigurationById(string id)
    {
      var configuration = await _dataService.GetConfigurationById(id);
      if (configuration == null)
        return NotFound("Configuration not found");

      return Ok(configuration);
    }

    [HttpGet("key/{key}")]
    public async Task<IActionResult> GetConfigurationByKey(string key)
    {
      var configuration = await _dataService.GetConfigurationByKey(key);
      if (configuration == null)
        return NotFound($"Configuration with key '{key}' not found");

      return Ok(configuration);
    }

    [HttpPost]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreateConfigurationRequest request)
    {
      var existing = await _dataService.GetConfigurationByKey(request.Key);
      if (existing != null)
        return BadRequest($"Configuration with key '{request.Key}' already exists.");

      var configuration = new MorWalPizConfiguration(
          Key: request.Key,
          Value: request.Value,
          Type: request.Type,
          Description: request.Description
      );

      await _dataService.SaveConfiguration(configuration);
      // Potrebbe essere utile restituire l'oggetto creato con l'ID
      // var createdConfig = await _dataService.GetConfigurationByKey(request.Key);
      // return CreatedAtAction(nameof(GetConfigurationById), new { id = createdConfig.Id }, createdConfig);
      return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConfiguration(string id, [FromBody] UpdateConfigurationRequest request)
    {
      var entity = await _dataService.GetConfigurationById(id);
      if (entity == null)
        return NotFound("Configuration not found");

      // Verifica se la nuova chiave è già usata da un'altra configurazione
      if (entity.Key != request.Key)
      {
        var existingWithNewKey = await _dataService.GetConfigurationByKey(request.Key);
        if (existingWithNewKey != null && existingWithNewKey.Id != id)
        {
          return BadRequest($"Another configuration with key '{request.Key}' already exists.");
        }
      }


      var updatedConfiguration = entity with
      {
        Key = request.Key,
        Value = request.Value,
        Type = request.Type,
        Description = request.Description
      };

      await _dataService.UpdateConfiguration(updatedConfiguration);

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConfiguration(string id)
    {
      var entity = await _dataService.GetConfigurationById(id);
      if (entity == null)
        return NotFound("Configuration not found");

      await _dataService.DeleteConfiguration(id);

      return NoContent();
    }
  }
}

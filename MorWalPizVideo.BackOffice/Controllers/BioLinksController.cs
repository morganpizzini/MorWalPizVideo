using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;
public class ToggleBioLinkRequest
{
    public string Title { get; set; } = string.Empty;
}

public class CreateBioLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateBioLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string NewTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class BioLinksController : ApplicationControllerBase
{
    private readonly IBioLinkRepository repository;
    private readonly ICrossApiService client;
    public BioLinksController(IBioLinkRepository repository, ICrossApiService client)
    {
        this.repository = repository;
        this.client = client;
    }
    [HttpPost]
    public async Task<IActionResult> CreateBioLink(CreateBioLinkRequest request)
    {
        var entity = new BioLink(request.Title, request.Description, request.Url, request.Icon, request.Order);
        var shiftedLinks = (await repository.GetItemsAsync(link => link.Order >= entity.Order))
            .Select(link => link with { Order = link.Order + 1 });
        foreach (var shiftedLink in shiftedLinks)
            await repository.UpdateItemAsync(shiftedLink);

        await repository.AddItemAsync(entity);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(CacheKeys.BioLinks);
        return NoContent();
    }
    [HttpPut]
    public async Task<IActionResult> UpdateBioLink(UpdateBioLinkRequest request)
    {
        var entity = (await repository.GetItemsAsync(link => link.Title.ToLower() == request.Title.ToLower())).FirstOrDefault();
        if (entity == null)
            return BadRequest("Bio link has not found");

        var oldOrder = entity.Order;
        var newOrder = request.Order;

        entity = entity with { Title = request.NewTitle, Description = request.Description, Order = newOrder };

        if (newOrder < oldOrder)
        {
            var shiftedLinks = await repository.GetItemsAsync(link => link.Order >= newOrder && link.Order < oldOrder);
            foreach (var shiftedLink in shiftedLinks)
                await repository.UpdateItemAsync(shiftedLink with { Order = shiftedLink.Order + 1 });
        }
        else if (newOrder > oldOrder)
        {
            var shiftedLinks = await repository.GetItemsAsync(link => link.Order > oldOrder && link.Order <= newOrder);
            foreach (var shiftedLink in shiftedLinks)
                await repository.UpdateItemAsync(shiftedLink with { Order = shiftedLink.Order - 1 });
        }

        await repository.UpdateItemAsync(entity);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(CacheKeys.BioLinks);
        return NoContent();
    }

    [HttpPut("toggle")]
    public async Task<IActionResult> ToggleBioLink(ToggleBioLinkRequest request)
    {
        var entity = (await repository.GetItemsAsync(link => link.Title.ToLower() == request.Title.ToLower())).FirstOrDefault();
        if (entity == null)
        {
            return BadRequest("Bio link has not found");
        }

        entity = entity with { Enable = !entity.Enable };

        await repository.UpdateItemAsync(entity);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(CacheKeys.BioLinks);
        return NoContent();
    }

    [HttpDelete("{title}")]
    public async Task<IActionResult> DeleteBioLink(string title)
    {
        var entity = (await repository.GetItemsAsync(link => link.Title.ToLower() == title.ToLower())).FirstOrDefault();
        if (entity == null)
        {
            return BadRequest("Bio link has not found");
        }
        await repository.DeleteItemAsync(entity.Id);

        await client.ResetCache(CacheKeys.BioLinks);
        await client.PurgeCache(CacheKeys.BioLinks);
        return NoContent();
    }
}

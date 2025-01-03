﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;
public class VideoImportRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
}

public class RootCreationRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string Url { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
}
public class SubVideoCrationRequest
{
    [Required]
    public string MatchId { get; set; } = string.Empty;
    [Required]
    public string VideoId { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
}
    public class VideoController : ApplicationController
{
    private readonly IMongoDatabase database;
    private readonly IHttpClientFactory client;
    public VideoController(IMongoDatabase _database, IHttpClientFactory _clientFactory)
    {
        database = _database;
        client = _clientFactory;
    }
    [HttpPost("ImportVideo")]
    public async Task<IActionResult> Import(VideoImportRequest request)
    {
        var matchCollection = database.GetCollection<Match>(DbCollections.Matches);

        matchCollection.InsertOne(new Match(request.VideoId, true, request.Category));

        using var client = this.client.CreateClient("MorWalPiz");

        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.Match}");
        json = await client.GetStringAsync($"cache/purge/{ApiTagCacheKeys.Matches}");
        json = await client.GetStringAsync("matches");
        return NoContent();
    }
    [HttpPost("RootCreation")]
    public IActionResult RootCreation(RootCreationRequest request)
    {
        var matchCollection = database.GetCollection<Match>(DbCollections.Matches);
        matchCollection.InsertOne(new Match(request.VideoId, request.Title, request.Description, request.Url, [], request.Category));
        return NoContent();
    }
    [HttpPost("ImportSubCreation")]
    public async Task<IActionResult> SubVideoCreation(SubVideoCrationRequest request)
    {
        var matchCollection = database.GetCollection<Match>(DbCollections.Matches);
        var existingMatch = matchCollection.Find(x => x.ThumbnailUrl == request.MatchId).FirstOrDefault();
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        existingMatch = existingMatch with { Videos = [.. existingMatch.Videos, new Video(request.VideoId, request.Category)] };

        await matchCollection.ReplaceOneAsync(Builders<Match>.Filter.Eq(e => e.Id, existingMatch.Id), existingMatch);

        using var client = this.client.CreateClient("MorWalPiz");

        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.Match}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.Matches}");

        json = await client.GetStringAsync("matches");
        return NoContent();
    }
}

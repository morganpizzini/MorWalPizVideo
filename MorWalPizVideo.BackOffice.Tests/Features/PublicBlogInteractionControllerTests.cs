using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.BlogInteractions;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PublicBlogInteractionControllerTests
{
    [Fact]
    public async Task Anonymous_visitors_can_read_only_approved_comments()
    {
        await using var factory = new ServerApiWebApplicationFactory();
        var blogs = factory.Services.GetRequiredService<BlogService>();
        var post = await blogs.CreateAsync("UCQRMDE-gxoD43w2q-QCVmIA", "interaction-test",
            new BlogSnapshot { Title = "Interactions", Summary = "Published article" });
        var published = await blogs.PublishAsync(post.ChannelId, post.Id, post.Revision, true);
        Assert.NotNull(published);

        var repository = factory.Services.GetRequiredService<IBlogInteractionRepository>();
        await repository.AddCommentAsync(new BlogComment
        {
            Id = "approved-comment",
            ChannelId = post.ChannelId,
            PostId = post.Id,
            Slug = post.Slug,
            AccountId = "account-approved",
            DisplayName = "Approved reader",
            Body = "Public comment",
            Status = BlogCommentStatus.Approved,
            CreationDateTime = DateTime.UtcNow
        });
        await repository.AddCommentAsync(new BlogComment
        {
            Id = "pending-comment",
            ChannelId = post.ChannelId,
            PostId = post.Id,
            Slug = post.Slug,
            AccountId = "account-pending",
            DisplayName = "Pending reader",
            Body = "Private until approved",
            Status = BlogCommentStatus.Pending,
            CreationDateTime = DateTime.UtcNow
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/blog/interaction-test/interactions/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<BlogCommentPageContract>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Public comment", page.Items[0].Body);
    }
}

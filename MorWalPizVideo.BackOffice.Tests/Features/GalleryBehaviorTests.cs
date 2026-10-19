using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class GalleryBehaviorTests
{
    [Fact]
    public void NormalizeSlug_trims_and_lowercases_the_public_slug()
    {
        Assert.Equal("summer-trip", Gallery.NormalizeSlug("  Summer-Trip/ "));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(21)]
    public void IsAllowedExpirationDays_accepts_the_supported_values(int days)
    {
        Assert.True(Gallery.IsAllowedExpirationDays(days));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(30)]
    public void IsAllowedExpirationDays_rejects_other_values(int days)
    {
        Assert.False(Gallery.IsAllowedExpirationDays(days));
    }
}
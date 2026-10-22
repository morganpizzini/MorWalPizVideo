using MorWalPizVideo.Server.Models;
using MorWalPiz.Contracts;

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

    [Fact]
    public void Public_gallery_conversion_uses_preview_and_falls_back_for_legacy_images()
    {
        var gallery = new Gallery
        {
            BlobReference =
            [
                new GalleryImage { PublicUrl = "original", PreviewPublicUrl = "preview" },
                new GalleryImage { PublicUrl = "legacy" }
            ]
        };

        var contract = ContractUtils.ConvertPublic(gallery);

        Assert.Equal(["preview", "legacy"], contract.Images);
    }
}
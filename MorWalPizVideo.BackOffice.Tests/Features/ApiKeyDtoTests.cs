using MorWalPizVideo.BackOffice.Controllers;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class ApiKeyDtoTests
{
    [Fact]
    public void Api_key_dto_exposes_scopes_with_an_empty_default()
    {
        var dto = new ApiKeyDto();

        Assert.NotNull(dto.Scopes);
        Assert.Empty(dto.Scopes);
    }
}

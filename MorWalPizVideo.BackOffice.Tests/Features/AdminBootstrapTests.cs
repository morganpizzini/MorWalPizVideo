using System.Net;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AdminBootstrapTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public AdminBootstrapTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Bootstrap_admin_rejects_missing_secret_before_user_lookup()
  {
    using var client = _factory.CreateClient();

    var response = await client.PostAsync("/api/User/bootstrap-admin/any-user", content: null);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Bootstrap_admin_rejects_invalid_secret()
  {
    using var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Bootstrap-Secret", "wrong-secret");

    var response = await client.PostAsync("/api/User/bootstrap-admin/any-user", content: null);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
}

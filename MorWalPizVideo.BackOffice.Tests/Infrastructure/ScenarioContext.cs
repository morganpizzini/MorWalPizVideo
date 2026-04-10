using System.Net;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

/// <summary>
/// Shared context for scenario data across step definition classes
/// </summary>
public class TestScenarioContext
{
    public HttpResponseMessage? Response { get; set; }
    public string? CreatedCompilationId { get; set; }
    public string? ExistingCompilationId { get; set; }
    public string? CreatedShortLinkId { get; set; }
    public string? EmbeddedShortLinkId { get; set; }
    public string? TestMatchId { get; set; }
    public string? TestChannelId { get; set; }
}

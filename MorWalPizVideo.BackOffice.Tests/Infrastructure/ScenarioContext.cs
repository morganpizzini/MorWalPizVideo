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
    public string? CreatedProductId { get; set; }
    public string? ExistingProductId { get; set; }
    public string? CreatedQueryLinkId { get; set; }
    public string? ExistingQueryLinkId { get; set; }
    public string? ExistingVideoId { get; set; }
    public string? CreatedVideoId { get; set; }
    public string? ExistingRootMatchId { get; set; }
}

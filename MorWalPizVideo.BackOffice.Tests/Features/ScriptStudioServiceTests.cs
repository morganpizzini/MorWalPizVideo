using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Models;
using FluentAssertions;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class ScriptStudioServiceTests
{
    [Fact]
    public void ComposePromptPrependsGlobalPromptAndIncludesStructuredInputs()
    {
        var request = new ScriptStudioGenerationRequest
        {
            Operation = "rewrite",
            Script = "Original script",
            Prompt = "Make it concise",
            Examples = "Example style",
            Style = "Warm",
            GeneralContext = "Channel context",
            Format = "markdown"
        };

        var prompt = ScriptStudioService.ComposePrompt("Global policy", request, ScriptStudioOperation.Rewrite);

        prompt.Should().StartWith("Global policy");
        prompt.Should().Contain("Operation: Rewrite");
        prompt.Should().Contain("Format: markdown");
        prompt.Should().Contain("Original script");
        prompt.Should().Contain("Make it concise");
    }

    [Fact]
    public void ComposePromptForPrettifyRequiresContentPreservation()
    {
        var request = new ScriptStudioGenerationRequest
        {
            Operation = "prettify",
            Script = "Use {{customerName}} at https://example.test and <code>x</code>.",
            Prompt = "Use short paragraphs",
            Format = "markdown"
        };

        var prompt = ScriptStudioService.ComposePrompt(null, request, ScriptStudioOperation.Prettify);

        prompt.Should().Contain("Preserve Markdown, HTML, placeholders, URLs, code-like tokens");
        prompt.Should().Contain(request.Script);
        prompt.Should().Contain(request.Prompt);
        prompt.Should().Contain("Return only the article content");
    }
}
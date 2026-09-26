using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Models;
using FluentAssertions;

namespace MorWalPizVideo.BackOffice.Tests.Features;

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
}
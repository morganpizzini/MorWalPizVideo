using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.Domain.Scenarios;
using Reqnroll;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

[Binding]
public sealed class ScenarioResetHooks(BackOfficeWebApplicationFactory factory)
{
    [BeforeScenario(Order = -1000)]
    public void ResetScenario()
    {
        _ = factory.CreateClient();
        factory.Services.GetRequiredService<IMockScenario>().Reset();
    }
}
using Microsoft.AspNetCore.Mvc;

namespace MorWalPizVideo.BackOffice.Controllers;

// Host-neutral base (ADR-002): no default [Authorize] here. Each host (BackOffice, ServerAPI, ShortLinks)
// establishes its own default/fallback authorization policy and explicit per-controller overrides.
[ApiController]
[Route("api/[controller]")]
public abstract class ApplicationControllerBase : ControllerBase
{
    protected ApplicationControllerBase()
    {
        
    }
}

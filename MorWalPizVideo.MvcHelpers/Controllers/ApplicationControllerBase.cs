using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class ApplicationControllerBase : ControllerBase
{
    protected ApplicationControllerBase()
    {
        
    }
}

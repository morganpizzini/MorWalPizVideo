using Microsoft.AspNetCore.Mvc;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApplicationControllerBase : ControllerBase
{
    protected ApplicationControllerBase()
    {
        
    }
}

using Microsoft.AspNetCore.Authorization;

namespace MorWalPizVideo.BackOffice.Authentication;

public class ApiKeyAuthAttribute : AuthorizeAttribute
{
    public ApiKeyAuthAttribute()
    {
        AuthenticationSchemes = "ApiKey";
    }
}
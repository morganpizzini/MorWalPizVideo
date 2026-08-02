using Microsoft.AspNetCore.Authorization;

namespace MorWalPizVideo.MvcHelpers.Authentication;

public class InternalServiceAuthAttribute : AuthorizeAttribute
{
    public InternalServiceAuthAttribute()
    {
        AuthenticationSchemes = InternalServiceAuthenticationHandler.SchemeName;
    }
}

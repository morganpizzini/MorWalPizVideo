using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

internal static class PublicAccountTestFlow
{
    public static async Task<HttpResponseMessage> ConfirmAndLoginAsync(ServerApiWebApplicationFactory factory,
        HttpClient client, PublicAccountRegistrationRequest registration, PublicAccountCsrfContract csrf, bool rememberMe = false)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await factory.Services.GetRequiredService<AccountMailMockSender>()
            .WaitForAsync(registration.Email, PublicAccountMailPurpose.EmailConfirmation, timeout.Token);
        var token = TokenFromLink(message.Link);
        using var confirmation = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/email-confirmations")
        {
            Content = JsonContent.Create(token)
        };
        confirmation.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(confirmation)).StatusCode);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/sessions")
        {
            Content = JsonContent.Create(new PublicAccountLoginRequest
            {
                Identifier = registration.Username, Password = registration.Password, Site = "workshop", RememberMe = rememberMe
            })
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        var response = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response;
    }

    public static PublicAccountTokenRequest TokenFromLink(string link)
    {
        var parameters = QueryHelpers.ParseQuery(new Uri(link).Fragment[1..]);
        return new PublicAccountTokenRequest { AccountId = parameters["accountId"].ToString(), Token = parameters["token"].ToString() };
    }
}

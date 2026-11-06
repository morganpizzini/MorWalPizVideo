using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/publicaccounts")]
public sealed class PublicAccountsController(IPublicAccountRepository accounts) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.PublicAccountsView)]
    public async Task<ActionResult<PublicAccountDirectoryPageContract>> List(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 50) return BadRequest("Invalid pagination.");
        var items = await accounts.ListAccountsAsync(search, page, pageSize, cancellationToken);
        var total = await accounts.CountAccountsAsync(search, cancellationToken);
        return Ok(new PublicAccountDirectoryPageContract(items.Select(account =>
            new PublicAccountDirectoryItemContract(account.Id, account.Username, account.DisplayName,
                account.IsActive, account.CreationDateTime)).ToArray(), page, pageSize, total));
    }
}

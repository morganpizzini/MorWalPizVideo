using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Controllers;

[ApiController, Route("api/shooting-range")]
public sealed class ShootingRangeController(ShootingRangeService service, IShootingRangeRepositorySet repositories, ShootingRangeSessions localSessions) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery) => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("auth/register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterShootingRangeUserRequest request) { try { var user = await service.RegisterAsync(request); return Ok(new AccountDto(user.Id, user.Username, user.FirstName, user.LastName, user.Status, user.IsAdmin, user.ForcePasswordChange)); } catch (InvalidOperationException error) { return Conflict(new { message = error.Message }); } }

    [HttpPost("auth/login"), AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting("range-login")]
    public async Task<IActionResult> Login(ShootingRangeLoginRequest request, CancellationToken cancellationToken)
    {
        return await repositories.ExecuteGuardedAsync<IActionResult>(async () =>
        {
            var user = await service.AuthenticateAsync(request.Username, request.Password);
            if (user is null) return Unauthorized(new { message = "Credentials invalid or account not approved." });
            await localSessions.RevokeAsync(User, cancellationToken);
            await localSessions.SignInAsync(HttpContext, user, cancellationToken);
            return Ok(ToAccount(user));
        }, cancellationToken);
    }

    [HttpGet("auth/session"), Authorize]
    public async Task<IActionResult> Session(CancellationToken cancellationToken)
    {
        var user = await repositories.Users.GetAsync(UserId(), cancellationToken);
        return user is null ? Unauthorized() : Ok(ToAccount(user));
    }

    [HttpPost("auth/logout"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) { await localSessions.RevokeAsync(User, cancellationToken); await HttpContext.SignOutAsync(); return NoContent(); }

    [HttpPost("auth/password"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request, CancellationToken cancellationToken)
        => await repositories.ExecuteGuardedAsync<IActionResult>(async () =>
    {
        var user = await repositories.Users.GetAsync(UserId(), cancellationToken);
        if (user is null || user.Status != ShootingRangeAccountStatus.Approved || !PasswordMatches(user, request.CurrentPassword)) return Unauthorized();
        var hash = PasswordHashing.HashPassword(request.NewPassword, out var salt);
        var updated = await repositories.Users.ReplaceAsync(user with { PasswordHash = $"{hash}:{salt}", ForcePasswordChange = false, SecurityVersion = Guid.NewGuid().ToString("N") }, cancellationToken);
        await localSessions.RevokeAsync(User, cancellationToken);
        await localSessions.SignInAsync(HttpContext, updated, cancellationToken);
        return NoContent();
    }, cancellationToken);

    [HttpGet("config"), AllowAnonymous]
    public Task<ShootingRangeConfig> Config(CancellationToken cancellationToken) => service.GetConfigAsync(cancellationToken);

    [HttpGet("sessions"), AllowAnonymous]
    public Task<SessionsDto> Sessions([FromQuery] DateOnly date, CancellationToken cancellationToken) => service.GetSessionsAsync(date, cancellationToken);

    [HttpGet("availability"), AllowAnonymous]
    public Task<AvailabilityDto> Availability([FromQuery] DateOnly date, [FromQuery] string period = "morning", CancellationToken cancellationToken = default) => service.GetAvailabilityAsync(date, period, cancellationToken, User.FindFirstValue(ClaimTypes.NameIdentifier));

    [HttpPost("bookings"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookingRequest request, CancellationToken cancellationToken) { try { var booking = await service.CreateBookingAsync(UserId(), request, cancellationToken); return Created($"api/shooting-range/bookings/{booking.Id}", booking); } catch (UnauthorizedAccessException error) { return StatusCode(403, new { message = error.Message }); } catch (InvalidOperationException error) { return Conflict(new { message = error.Message }); } }

    [HttpGet("bookings/mine"), Authorize]
    public async Task<IReadOnlyList<ShootingRangeBooking>> Mine(CancellationToken cancellationToken) => (await repositories.Bookings.GetAllAsync(cancellationToken)).Where(x => x.UserId == UserId()).OrderByDescending(x => x.StartUtc).ToArray();

    [HttpGet("admin/users"), Authorize(Roles = "admin")]
    public async Task<IReadOnlyList<AdminUserDto>> Users(CancellationToken cancellationToken)
    {
        await service.RequireAdminAsync(UserId(), cancellationToken);
        return (await repositories.Users.GetAllAsync(cancellationToken)).Select(user => new AdminUserDto(user.Id, user.CreationDateTime, user.Username, user.FirstName, user.LastName, user.Status, user.IsAdmin, user.ForcePasswordChange)).ToArray();
    }

    [HttpPost("admin/users/{id}/approve"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(string id, CancellationToken cancellationToken) => service.AdminMutationAsync<IActionResult>(UserId(), async () => { var user = await repositories.Users.GetAsync(id, cancellationToken); if (user is null) return NotFound(); await repositories.Users.ReplaceAsync(user with { Status = ShootingRangeAccountStatus.Approved }, cancellationToken); return NoContent(); }, cancellationToken);

    [HttpPost("admin/users/{id}/password"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> ResetPassword(string id, AdminPasswordResetRequest request, CancellationToken cancellationToken) => service.AdminMutationAsync<IActionResult>(UserId(), async () => { var user = await repositories.Users.GetAsync(id, cancellationToken); if (user is null) return NotFound(); var hash = PasswordHashing.HashPassword(request.NewPassword, out var salt); await repositories.Users.ReplaceAsync(user with { PasswordHash = $"{hash}:{salt}", ForcePasswordChange = true, SecurityVersion = Guid.NewGuid().ToString("N") }, cancellationToken); return NoContent(); }, cancellationToken);

    [HttpGet("admin/bays"), Authorize(Roles = "admin")]
    public async Task<IReadOnlyList<ShootingRangeBay>> Bays(CancellationToken cancellationToken) { await service.RequireAdminAsync(UserId(), cancellationToken); return await repositories.Bays.GetAllAsync(cancellationToken); }

    [HttpPut("admin/bays/{id}"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> SaveBay(string id, ShootingRangeBay bay, CancellationToken cancellationToken) => service.AdminMutationAsync<IActionResult>(UserId(), async () => { var saved = await repositories.Bays.ReplaceAsync(bay with { Id = id }, cancellationToken); return Ok(saved); }, cancellationToken);

    [HttpGet("admin/config"), Authorize(Roles = "admin")]
    public async Task<ShootingRangeConfig> AdminConfig(CancellationToken cancellationToken) { await service.RequireAdminAsync(UserId(), cancellationToken); return await service.GetConfigAsync(cancellationToken); }

    [HttpPut("admin/config"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> SaveConfig(ShootingRangeConfig config, CancellationToken cancellationToken) => service.AdminMutationAsync<IActionResult>(UserId(), async () => { var saved = await repositories.Configs.ReplaceAsync(config with { Id = "default" }, cancellationToken); return Ok(saved); }, cancellationToken);

    [HttpGet("admin/exceptions"), Authorize(Roles = "admin")]
    public async Task<IReadOnlyList<ShootingRangeException>> Exceptions(CancellationToken cancellationToken) { await service.RequireAdminAsync(UserId(), cancellationToken); return await repositories.Exceptions.GetAllAsync(cancellationToken); }

    [HttpPost("admin/exceptions"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<ShootingRangeException> AddException(ShootingRangeException exception, CancellationToken cancellationToken) => service.AdminMutationAsync(UserId(), () => repositories.Exceptions.InsertAsync(exception with { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString() }, cancellationToken), cancellationToken);

    [HttpPut("admin/exceptions/{id}"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<ShootingRangeException> SaveException(string id, ShootingRangeException exception, CancellationToken cancellationToken) => service.AdminMutationAsync(UserId(), async () =>
    {
        var previous = await repositories.Exceptions.GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Closure not found.");
        return await repositories.Exceptions.ReplaceAsync(exception with { Id = id, CreationDateTime = previous.CreationDateTime }, cancellationToken);
    }, cancellationToken);

    [HttpGet("admin/bookings"), Authorize(Roles = "admin")]
    public async Task<IReadOnlyList<ShootingRangeBooking>> AllBookings(CancellationToken cancellationToken) { await service.RequireAdminAsync(UserId(), cancellationToken); return await repositories.Bookings.GetAllAsync(cancellationToken); }

    [HttpPost("admin/bookings/{id}/decision"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DecideBooking(string id, BookingDecisionRequest request, CancellationToken cancellationToken) { await service.DecideBookingAsync(UserId(), id, request.Approved, cancellationToken); return NoContent(); }

    [HttpGet("messages"), Authorize]
    public async Task<IReadOnlyList<ShootingRangeThread>> Messages(CancellationToken cancellationToken)
        => (await repositories.Threads.GetAllAsync(cancellationToken)).Where(x => User.IsInRole("admin") || x.UserId == UserId()).OrderByDescending(x => x.CreationDateTime).ToArray();

    [HttpPost("messages"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenMessage(ThreadRequest request, CancellationToken cancellationToken)
    {
        var thread = new ShootingRangeThread { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), UserId = UserId(), Subject = request.Subject.Trim(), Messages = [new(UserId(), false, request.Text.Trim(), DateTime.UtcNow)] };
        await repositories.Threads.InsertAsync(thread, cancellationToken);
        return Created($"api/shooting-range/messages/{thread.Id}", thread);
    }

    [HttpPost("messages/{id}/replies"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string id, ThreadMessageRequest request, CancellationToken cancellationToken)
    {
        var thread = await repositories.Threads.GetAsync(id, cancellationToken);
        if (thread is null || (!User.IsInRole("admin") && thread.UserId != UserId())) return NotFound();
        var updated = thread with { Messages = [.. thread.Messages, new(UserId(), User.IsInRole("admin"), request.Text.Trim(), DateTime.UtcNow)] };
        await repositories.Threads.ReplaceAsync(updated, cancellationToken);
        return Ok(updated);
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    private static AccountDto ToAccount(ShootingRangeUser user) => new(user.Id, user.Username, user.FirstName, user.LastName, user.Status, user.IsAdmin, user.ForcePasswordChange);
    private static bool PasswordMatches(ShootingRangeUser user, string password) { var parts = user.PasswordHash.Split(':', 2); return parts.Length == 2 && PasswordHashing.VerifyPassword(password, parts[0], parts[1]); }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.ShootingRange;
using MorWalPizVideo.Domain.ShootingRange;
using MorWalPizVideo.Models.ShootingRange;
using MorWalPizVideo.ShootingRange.Services;
using MorWalPizVideo.Domain.Security;

namespace MorWalPizVideo.ShootingRange.Controllers;

[ApiController, Route("api/shooting-range")]
public sealed class ShootingRangeController(ShootingRangeService service, IShootingRangeRepositorySet repositories) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery) => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("auth/register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterShootingRangeUserRequest request) { try { var user = await service.RegisterAsync(request); return Ok(new AccountDto(user.Id, user.Username, user.FirstName, user.LastName, user.Status, user.IsAdmin, user.ForcePasswordChange)); } catch (InvalidOperationException error) { return Conflict(new { message = error.Message }); } }

    [HttpPost("auth/login"), AllowAnonymous]
    public async Task<IActionResult> Login(ShootingRangeLoginRequest request)
    {
        var user = await service.AuthenticateAsync(request.Username, request.Password);
        if (user is null) return Unauthorized(new { message = "Credentials invalid or account not approved." });
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.IsAdmin ? "admin" : "user")], "shooting-range")));
        return Ok(new AccountDto(user.Id, user.Username, user.FirstName, user.LastName, user.Status, user.IsAdmin, user.ForcePasswordChange));
    }

    [HttpPost("auth/logout"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return NoContent(); }

    [HttpPost("auth/password"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await repositories.Users.GetAsync(UserId(), cancellationToken);
        if (user is null || !PasswordMatches(user, request.CurrentPassword)) return Unauthorized();
        var hash = PasswordHashing.HashPassword(request.NewPassword, out var salt);
        await repositories.Users.ReplaceAsync(user with { PasswordHash = $"{hash}:{salt}", ForcePasswordChange = false }, cancellationToken);
        return NoContent();
    }

    [HttpGet("availability"), AllowAnonymous]
    public Task<AvailabilityDto> Availability([FromQuery] DateOnly date, [FromQuery] string period = "morning", CancellationToken cancellationToken = default) => service.GetAvailabilityAsync(date, period, cancellationToken);

    [HttpPost("bookings"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookingRequest request, CancellationToken cancellationToken) { try { var booking = await service.CreateBookingAsync(UserId(), request, cancellationToken); return Created($"api/shooting-range/bookings/{booking.Id}", booking); } catch (UnauthorizedAccessException error) { return StatusCode(403, new { message = error.Message }); } catch (InvalidOperationException error) { return Conflict(new { message = error.Message }); } }

    [HttpGet("bookings/mine"), Authorize]
    public async Task<IReadOnlyList<ShootingRangeBooking>> Mine(CancellationToken cancellationToken) => (await repositories.Bookings.GetAllAsync(cancellationToken)).Where(x => x.UserId == UserId()).OrderByDescending(x => x.StartUtc).ToArray();

    [HttpGet("admin/users"), Authorize(Roles = "admin")]
    public async Task<IReadOnlyList<ShootingRangeUser>> Users(CancellationToken cancellationToken) => await repositories.Users.GetAllAsync(cancellationToken);

    [HttpPost("admin/users/{id}/approve"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id, CancellationToken cancellationToken) { var user = await repositories.Users.GetAsync(id, cancellationToken); if (user is null) return NotFound(); await repositories.Users.ReplaceAsync(user with { Status = ShootingRangeAccountStatus.Approved }, cancellationToken); return NoContent(); }

    [HttpPost("admin/users/{id}/password"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, AdminPasswordResetRequest request, CancellationToken cancellationToken) { var user = await repositories.Users.GetAsync(id, cancellationToken); if (user is null) return NotFound(); var hash = PasswordHashing.HashPassword(request.NewPassword, out var salt); await repositories.Users.ReplaceAsync(user with { PasswordHash = $"{hash}:{salt}", ForcePasswordChange = true }, cancellationToken); return NoContent(); }

    [HttpGet("admin/bays"), Authorize(Roles = "admin")]
    public Task<IReadOnlyList<ShootingRangeBay>> Bays(CancellationToken cancellationToken) => repositories.Bays.GetAllAsync(cancellationToken);

    [HttpPut("admin/bays/{id}"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBay(string id, ShootingRangeBay bay, CancellationToken cancellationToken) { await repositories.Bays.ReplaceAsync(bay with { Id = id }, cancellationToken); return Ok(bay with { Id = id }); }

    [HttpPut("admin/config"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveConfig(ShootingRangeConfig config, CancellationToken cancellationToken) { await repositories.Configs.ReplaceAsync(config with { Id = "default" }, cancellationToken); return Ok(config with { Id = "default" }); }

    [HttpPost("admin/exceptions"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public Task<ShootingRangeException> AddException(ShootingRangeException exception, CancellationToken cancellationToken) => repositories.Exceptions.InsertAsync(exception with { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString() }, cancellationToken);

    [HttpGet("admin/bookings"), Authorize(Roles = "admin")]
    public Task<IReadOnlyList<ShootingRangeBooking>> AllBookings(CancellationToken cancellationToken) => repositories.Bookings.GetAllAsync(cancellationToken);

    [HttpPost("admin/bookings/{id}/decision"), Authorize(Roles = "admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DecideBooking(string id, BookingDecisionRequest request, CancellationToken cancellationToken) { var booking = await repositories.Bookings.GetAsync(id, cancellationToken); if (booking is null) return NotFound(); await repositories.Bookings.ReplaceAsync(booking with { Status = request.Approved ? ShootingRangeBookingStatus.Approved : ShootingRangeBookingStatus.Rejected }, cancellationToken); return NoContent(); }

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
    private static bool PasswordMatches(ShootingRangeUser user, string password) { var parts = user.PasswordHash.Split(':', 2); return parts.Length == 2 && PasswordHashing.VerifyPassword(password, parts[0], parts[1]); }
}

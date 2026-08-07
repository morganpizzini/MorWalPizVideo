using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace MorWalPizVideo.BackOffice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowUser("perm:" + AuthorizationPermissionKeys.CanAccessBackoffice)]
    public class UserController : ApplicationControllerBase
    {
        private readonly DataService _dataService;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(
            DataService dataService,
            IUserGroupRepository userGroupRepository,
            IConfiguration configuration,
            ILogger<UserController> logger)
        {
            _dataService = dataService;
            _userGroupRepository = userGroupRepository;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserContract>>> GetUsers()
        {
            var users = await _dataService.FetchUsers();
            return Ok(users.Select(ContractUtils.Convert));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserContract>> GetUser(string id)
        {
            var user = await _dataService.GetUser(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(ContractUtils.Convert(user));
        }

        [AllowAnonymous]
        [HttpPost("bootstrap-admin/{username}")]
        public async Task<IActionResult> BootstrapAdmin(string username)
        {
            var configuredSecret = _configuration["BootstrapSettings:Secret"];
            var suppliedSecret = Request.Headers["X-Bootstrap-Secret"].ToString();
            if (string.IsNullOrWhiteSpace(configuredSecret) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(configuredSecret),
                    Encoding.UTF8.GetBytes(suppliedSecret)))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Username is required." });
            }

            var users = await _dataService.FetchUsers();
            var groups = await _userGroupRepository.GetItemsAsync();
            var groupsById = groups.ToDictionary(group => group.Id, StringComparer.OrdinalIgnoreCase);
            var hasBackofficeUser = users.Any(user =>
                user.IsActive &&
                (user.CanAccessBackoffice ||
                 user.DirectPermissions.Contains(
                     AuthorizationPermissionKeys.CanAccessBackoffice,
                     StringComparer.OrdinalIgnoreCase) ||
                 (user.GroupIds ?? []).Any(groupId =>
                     groupsById.TryGetValue(groupId, out var group) &&
                     group.IsActive &&
                     group.Permissions.Contains(
                         AuthorizationPermissionKeys.CanAccessBackoffice,
                         StringComparer.OrdinalIgnoreCase))));

            if (hasBackofficeUser)
            {
                return Conflict(new { message = "Initial admin bootstrap is already complete." });
            }

            var user = users.FirstOrDefault(candidate =>
                string.Equals(candidate.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return NotFound(new { message = "User was not found." });
            }

            if (!user.IsActive)
            {
                return BadRequest(new { message = "User is inactive." });
            }

            var adminGroup = await _userGroupRepository.GetByCodeAsync(AuthorizationGroupCodes.Admin);
            if (adminGroup is null)
            {
                adminGroup = await _userGroupRepository.AddItemAsync(new UserGroup
                {
                    Code = AuthorizationGroupCodes.Admin,
                    Name = "Administrators",
                    Description = "Initial platform administrators.",
                    IsActive = true,
                    Permissions = [AuthorizationPermissionKeys.CanAccessBackoffice]
                });
            }
            else if (!adminGroup.Permissions.Contains(
                         AuthorizationPermissionKeys.CanAccessBackoffice,
                         StringComparer.OrdinalIgnoreCase))
            {
                adminGroup = adminGroup with
                {
                    Permissions = adminGroup.Permissions
                        .Append(AuthorizationPermissionKeys.CanAccessBackoffice)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };
                await _userGroupRepository.UpdateItemAsync(adminGroup);
            }

            if (!(user.GroupIds ?? []).Contains(adminGroup.Id, StringComparer.OrdinalIgnoreCase))
            {
                await _dataService.UpdateUser(user with
                {
                    GroupIds = (user.GroupIds ?? []).Append(adminGroup.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                });
            }

            _logger.LogInformation("Initial admin group assigned to user {Username}", user.Username);
            return Ok(new { username = user.Username, group = AuthorizationGroupCodes.Admin });
        }

        [HttpPost]
        [AllowUser("group:" + AuthorizationGroupCodes.Admin)]
        public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username, email, and password are required.");
            }

            // Check if user already exists
            var existingUsers = await _dataService.FetchUsers();
            if (existingUsers.Any(u => u.Username == request.Username || u.Email == request.Email))
            {
                return BadRequest("User with this username or email already exists.");
            }

            var passwordHash = PasswordHashing.HashPassword(request.Password, out var salt);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                IsActive = request.IsActive ?? true
            };

            await _dataService.SaveUser(user);
            var createdUser = await _dataService.GetUserByUsername(request.Username);
            if (createdUser is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "User creation failed.");
            }

            return Created($"/api/User/{createdUser.Id}", ContractUtils.Convert(createdUser));
        }

        [HttpPut("{id}")]
        [AllowUser("group:" + AuthorizationGroupCodes.Admin)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var existingUser = await _dataService.GetUser(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(request.Username) || !string.IsNullOrWhiteSpace(request.Email))
            {
                var users = await _dataService.FetchUsers();
                if (!string.IsNullOrWhiteSpace(request.Username) &&
                    users.Any(user => user.Id != existingUser.Id &&
                                      string.Equals(user.Username, request.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("User with this username already exists.");
                }

                if (!string.IsNullOrWhiteSpace(request.Email) &&
                    users.Any(user => user.Id != existingUser.Id &&
                                      string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("User with this email already exists.");
                }
            }

            var passwordHash = existingUser.PasswordHash;
            var salt = existingUser.Salt;
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                passwordHash = PasswordHashing.HashPassword(request.NewPassword, out salt);
            }

            // Since User is a record, create a new instance with updated values
            var updatedUser = existingUser with
            {
                Username = !string.IsNullOrWhiteSpace(request.Username) ? request.Username : existingUser.Username,
                Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : existingUser.Email,
                IsActive = request.IsActive ?? existingUser.IsActive,
                PasswordHash = passwordHash,
                Salt = salt
            };

            await _dataService.UpdateUser(updatedUser);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [AllowUser("group:" + AuthorizationGroupCodes.Admin)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _dataService.GetUser(id);
            if (user == null)
            {
                return NotFound();
            }

            await _dataService.DeleteUser(id);
            return NoContent();
        }

        [HttpPut("{id}/status")]
        [AllowUser("group:" + AuthorizationGroupCodes.Admin)]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateUserStatusRequest request)
        {
            var user = await _dataService.GetUser(id);
            if (user == null)
            {
                return NotFound();
            }

            var updatedUser = user with { IsActive = request.IsActive };
            await _dataService.UpdateUser(updatedUser);

            return NoContent();
        }

        [HttpPut("{id}/password/reset")]
        [AllowUser("group:" + AuthorizationGroupCodes.Admin)]
        public async Task<IActionResult> ResetUserPassword(string id, [FromBody] ResetUserPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("New password is required.");
            }

            var user = await _dataService.GetUser(id);
            if (user == null)
            {
                return NotFound();
            }

            var passwordHash = PasswordHashing.HashPassword(request.NewPassword, out var salt);
            await _dataService.UpdateUser(user with
            {
                PasswordHash = passwordHash,
                Salt = salt
            });

            return NoContent();
        }

        [HttpPut("{id}/password/set")]
        [AllowUser("group:" + AuthorizationGroupCodes.Admin)]
        public Task<IActionResult> SetUserPassword(string id, [FromBody] ResetUserPasswordRequest request)
        {
            return ResetUserPassword(id, request);
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserContract>> GetProfile()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var user = await _dataService.GetUser(currentUserId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(ContractUtils.Convert(user));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateOwnProfileRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var user = await _dataService.GetUser(currentUserId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Username) && string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("At least one field is required.");
            }

            var users = await _dataService.FetchUsers();
            if (!string.IsNullOrWhiteSpace(request.Username) &&
                users.Any(existing => existing.Id != user.Id &&
                                      string.Equals(existing.Username, request.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("User with this username already exists.");
            }

            if (!string.IsNullOrWhiteSpace(request.Email) &&
                users.Any(existing => existing.Id != user.Id &&
                                      string.Equals(existing.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("User with this email already exists.");
            }

            var updatedUser = user with
            {
                Username = !string.IsNullOrWhiteSpace(request.Username) ? request.Username.Trim() : user.Username,
                Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : user.Email,
            };

            await _dataService.UpdateUser(updatedUser);
            return NoContent();
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangeOwnPassword([FromBody] ChangeOwnPasswordRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var user = await _dataService.GetUser(currentUserId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Current password and new password are required.");
            }

            if (!PasswordHashing.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt))
            {
                return BadRequest("Current password is invalid.");
            }

            var newHash = PasswordHashing.HashPassword(request.NewPassword, out var newSalt);
            await _dataService.UpdateUser(user with
            {
                PasswordHash = newHash,
                Salt = newSalt
            });

            return NoContent();
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public string? NewPassword { get; set; }
    }

    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class ResetUserPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateOwnProfileRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
    }

    public class ChangeOwnPasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}

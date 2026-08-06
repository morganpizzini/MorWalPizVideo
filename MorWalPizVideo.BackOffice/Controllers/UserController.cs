using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowUser("admin", "contributor")]
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

            // Generate salt and hash password
            var salt = GenerateSalt();
            var passwordHash = HashPassword(request.Password, salt);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role,
                IsActive = request.IsActive ?? true
            };

            await _dataService.SaveUser(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ContractUtils.Convert(user));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var existingUser = await _dataService.GetUser(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Since User is a record, create a new instance with updated values
            var updatedUser = existingUser with
            {
                Username = !string.IsNullOrWhiteSpace(request.Username) ? request.Username : existingUser.Username,
                Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : existingUser.Email,
                Role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role : existingUser.Role,
                IsActive = request.IsActive ?? existingUser.IsActive,
                PasswordHash = !string.IsNullOrWhiteSpace(request.NewPassword) ? HashPassword(request.NewPassword, GenerateSalt()) : existingUser.PasswordHash,
                Salt = !string.IsNullOrWhiteSpace(request.NewPassword) ? GenerateSalt() : existingUser.Salt
            };

            await _dataService.UpdateUser(updatedUser);

            return NoContent();
        }

        [HttpDelete("{id}")]
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

        private static string GenerateSalt()
        {
            var saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 10000, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(hashBytes);
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public string? NewPassword { get; set; }
    }

    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }
}

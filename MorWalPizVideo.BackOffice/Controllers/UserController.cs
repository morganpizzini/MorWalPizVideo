using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    //[Authorize]
    public class UserController : ApplicationControllerBase
    {
        private readonly DataService _dataService;

        public UserController(DataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _dataService.FetchUsers();
            
            // Return users without sensitive data
            var userList = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.IsActive,
                u.LastLogin
            });

            return Ok(userList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(string id)
        {
            var user = await _dataService.GetUser(id);
            
            if (user == null)
            {
                return NotFound();
            }

            // Return user without sensitive data
            var userInfo = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsActive,
                user.LastLogin
            };

            return Ok(userInfo);
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

            var userInfo = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsActive,
                user.LastLogin
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userInfo);
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
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000))
            {
                var hashBytes = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hashBytes);
            }
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

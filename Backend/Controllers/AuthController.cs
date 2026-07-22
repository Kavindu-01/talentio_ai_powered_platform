using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Backend.Data;
using Backend.Models;
using BCrypt.Net;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Hiring Managers and Recruiters require Administrator verification before they can log in
            bool requiresApproval = request.Role == UserRole.Recruiter || request.Role == UserRole.HiringManager;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = request.Role,
                IsApproved = !requiresApproval
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // If Candidate, initialize profile automatically
            if (user.Role == UserRole.Candidate)
            {
                var profile = new CandidateProfile
                {
                    UserId = user.Id,
                    Bio = "No bio provided yet.",
                    Skills = "",
                    Experience = "",
                    ResumePath = ""
                };
                _context.CandidateProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            // Log event
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "User Registration",
                Details = $"User registered with role {user.Role} (Approved: {user.IsApproved})."
            });
            await _context.SaveChangesAsync();

            if (requiresApproval)
            {
                return Ok(new { message = "Registration submitted successfully! Your account request is currently pending verification & approval by a System Administrator." });
            }

            return Ok(new { message = "Registration successful! You may now sign in to your account." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.IsApproved)
            {
                return BadRequest(new { message = "Account Pending Approval: Your account request is currently being reviewed by a System Administrator. You will receive an email notification once approved." });
            }

            var token = GenerateJwtToken(user);

            // Log event
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "User Login",
                Details = $"User logged in."
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_FOR_RECRUITMENT_PLATFORM_2026";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"] ?? "RecruitmentAPI",
                Audience = _configuration["Jwt:Audience"] ?? "RecruitmentClient",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Candidate;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

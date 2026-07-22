using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public UserController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyUserProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                Role = user.Role.ToString(),
                user.AvatarUrl
            });
        }

        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyUserProfile([FromBody] UpdateUserProfileRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }
            user.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "User profile updated", user = new { user.Id, user.Name, user.Email, Role = user.Role.ToString(), user.AvatarUrl } });
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No image file provided." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid image format. Please upload JPG, PNG, or WEBP." });
            }

            var avatarsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Avatars");
            if (!Directory.Exists(avatarsFolder))
            {
                Directory.CreateDirectory(avatarsFolder);
            }

            var fileName = $"avatar_{userId}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(avatarsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/Uploads/Avatars/{fileName}";

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.AvatarUrl = relativeUrl;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Profile picture uploaded successfully", avatarUrl = relativeUrl });
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.IsApproved,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsApproved = true;

            var adminUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(adminUserIdString, out int adminUserId);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = "User Account Approved",
                Details = $"Approved account for {user.Name} ({user.Email}, Role: {user.Role})."
            });

            await _context.SaveChangesAsync();

            // Send real email notification to the approved Hiring Manager / Recruiter
            try
            {
                await _notificationService.SendEmailAsync(
                    user.Email,
                    "Account Approved - Welcome to Talentio!",
                    $"Hello {user.Name},\n\nYour account request for the role of '{user.Role}' has been verified and approved by a System Administrator.\n\nYou may now log in to the Talentio platform.\n\nBest regards,\nTalentio System Administration"
                );
            }
            catch (Exception)
            {
                // Silently ignore if SMTP delivery encounters network issue
            }

            return Ok(new { message = $"User {user.Name} ({user.Email}) has been approved successfully." });
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var adminUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminUserIdString, out int adminUserId) && id == adminUserId)
            {
                return BadRequest(new { message = "You cannot change your own role." });
            }

            var oldRole = user.Role;
            user.Role = request.Role;

            // If changing to Candidate and profile doesn't exist, create it
            if (user.Role == UserRole.Candidate)
            {
                var profileExists = await _context.CandidateProfiles.AnyAsync(p => p.UserId == user.Id);
                if (!profileExists)
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
                }
            }

            // Log activity
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = "User Role Updated",
                Details = $"Changed role of user {user.Email} (ID: {user.Id}) from '{oldRole}' to '{user.Role}'."
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "User role updated successfully", role = user.Role });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var adminUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminUserIdString, out int adminUserId) && id == adminUserId)
            {
                return BadRequest(new { message = "You cannot delete your own account." });
            }

            _context.Users.Remove(user);

            // Log activity
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUserId,
                Action = "User Account Deleted",
                Details = $"Deleted user account of {user.Email} (ID: {user.Id})."
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "User deleted successfully" });
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _context.AuditLogs
                .Include(l => l.User)
                .Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.Details,
                    l.Timestamp,
                    UserEmail = l.User != null ? l.User.Email : "System"
                })
                .OrderByDescending(l => l.Timestamp)
                .Take(100) // Return last 100 entries
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("privacy/export")]
        [Authorize]
        public async Task<IActionResult> ExportMyData()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            
            object? applications = null;
            object? interviews = null;
            object? assessments = null;

            if (profile != null)
            {
                applications = await _context.JobApplications
                    .Where(a => a.CandidateProfileId == profile.Id)
                    .Select(a => new { a.Id, a.JobPostingId, a.Status, a.AIScore, a.AppliedDate })
                    .ToListAsync();

                interviews = await _context.InterviewSchedules
                    .Where(i => i.JobApplication!.CandidateProfileId == profile.Id)
                    .Select(i => new { i.Id, i.InterviewDate, i.LocationOrLink, i.Status, i.Score, i.Feedback })
                    .ToListAsync();

                assessments = await _context.CandidateAssessmentResults
                    .Where(r => r.CandidateProfileId == profile.Id)
                    .Select(r => new { r.Id, r.SkillAssessmentId, r.Score, r.Passed, r.TakenAt })
                    .ToListAsync();
            }

            var dataExport = new
            {
                ExportedAt = DateTime.UtcNow,
                CompliancePolicy = "GDPR Article 20 - Right to Data Portability",
                UserProfile = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.CreatedAt
                },
                CandidateProfile = profile != null ? new
                {
                    profile.Id,
                    profile.Bio,
                    profile.Skills,
                    profile.Experience,
                    profile.UpdatedAt
                } : null,
                JobApplications = applications,
                ScheduledInterviews = interviews,
                SkillAssessments = assessments
            };

            return Ok(dataExport);
        }

        [HttpDelete("privacy/forget")]
        [Authorize]
        public async Task<IActionResult> ErasureRequest()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null)
            {
                var apps = await _context.JobApplications.Where(a => a.CandidateProfileId == profile.Id).ToListAsync();
                _context.JobApplications.RemoveRange(apps);

                var results = await _context.CandidateAssessmentResults.Where(r => r.CandidateProfileId == profile.Id).ToListAsync();
                _context.CandidateAssessmentResults.RemoveRange(results);

                _context.CandidateProfiles.Remove(profile);
            }

            _context.Users.Remove(user);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "GDPR Erasure Triggered",
                Details = $"Right to be forgotten request processed. Anonymized/Deleted all records associated with User ID {userId}."
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "GDPR Right to Erasure processed. All personal profiles and applications have been permanently erased from the platform databases." });
        }
    }

    public class UpdateRoleRequest
    {
        public UserRole Role { get; set; }
    }

    public class UpdateUserProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}

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
    public class ApplicationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly INotificationService _notificationService;

        public ApplicationController(ApplicationDbContext context, IAIService aiService, INotificationService notificationService)
        {
            _context = context;
            _aiService = aiService;
            _notificationService = notificationService;
        }

        [HttpPost("apply")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Apply([FromForm] ApplyRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return BadRequest(new { message = "Candidate profile not found. Please log in as a candidate." });
            }

            var job = await _context.JobPostings.FindAsync(request.JobPostingId);
            if (job == null)
            {
                return NotFound(new { message = "Job posting not found" });
            }

            // Check if already applied
            var existingApp = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobPostingId == request.JobPostingId && a.CandidateProfileId == profile.Id);
            if (existingApp != null)
            {
                return BadRequest(new { message = "You have already applied for this job." });
            }

            string relativeResumePath = "";
            string resumeText = "";

            if (request.ResumeFile != null && request.ResumeFile.Length > 0)
            {
                var extension = Path.GetExtension(request.ResumeFile.FileName).ToLower();
                if (extension != ".pdf")
                {
                    return BadRequest(new { message = "Please upload non-scanned resume PDF!" });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.ResumeFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ResumeFile.CopyToAsync(stream);
                }

                relativeResumePath = Path.Combine("Uploads", "Resumes", uniqueFileName).Replace("\\", "/");

                try
                {
                    using (var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath))
                    {
                        var textBuilder = new System.Text.StringBuilder();
                        foreach (var page in pdf.GetPages())
                        {
                            var words = page.GetWords();
                            if (words != null && words.Any())
                            {
                                textBuilder.AppendLine(string.Join(" ", words.Select(w => w.Text)));
                            }
                            else
                            {
                                textBuilder.AppendLine(page.Text);
                            }
                        }
                        resumeText = textBuilder.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(resumeText))
                    {
                        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                        return BadRequest(new { message = "Please upload non-scanned resume PDF!" });
                    }
                }
                catch
                {
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    return BadRequest(new { message = "Please upload non-scanned resume PDF!" });
                }
            }

            // If resumeText was extracted, parse it using AI to update candidate profile
            if (!string.IsNullOrWhiteSpace(resumeText))
            {
                var parsedData = await _aiService.ParseResumeAsync(resumeText);
                profile.Skills = string.IsNullOrWhiteSpace(parsedData.Skills) ? profile.Skills : parsedData.Skills;
                profile.Experience = string.IsNullOrWhiteSpace(parsedData.Experience) ? profile.Experience : parsedData.Experience;
                profile.Bio = string.IsNullOrWhiteSpace(parsedData.Bio) ? profile.Bio : parsedData.Bio;
                profile.ResumePath = relativeResumePath;
                profile.CVContent = resumeText;
                profile.UpdatedAt = DateTime.UtcNow;

                _context.CandidateProfiles.Update(profile);
            }

            // Run AI Candidate-Job Match
            var (score, feedback) = await _aiService.ScoreCandidateAsync(
                string.IsNullOrWhiteSpace(resumeText) ? profile.CVContent : resumeText,
                job.Description,
                job.RequiredSkills
            );

            var application = new JobApplication
            {
                JobPostingId = job.Id,
                CandidateProfileId = profile.Id,
                Status = "Applied",
                ResumePath = relativeResumePath,
                AppliedDate = DateTime.UtcNow,
                AIScore = score,
                AIFeedback = feedback
            };

            _context.JobApplications.Add(application);

            // Log event
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Job Application Submitted",
                Details = $"Applied to '{job.Title}' (ID: {job.Id}) with AI Match Score: {score}%."
            });

            await _context.SaveChangesAsync();

            // Send simulated notification
            var emailBody = $"Hello {User.FindFirst(ClaimTypes.Name)?.Value},\n\n" +
                             $"Thank you for applying to the {job.Title} role. We have received your application and resume. " +
                             $"Our hiring team will review it shortly.\n\nBest regards,\nRecruitment Team";
            await _notificationService.SendEmailAsync(User.FindFirst(ClaimTypes.Email)?.Value ?? "candidate@example.com", "Application Received - " + job.Title, emailBody);

            return Ok(new
            {
                message = "Application submitted successfully",
                applicationId = application.Id,
                aiScore = score,
                aiFeedback = feedback
            });
        }

        [HttpGet("my-applications")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return Ok(Array.Empty<object>());
            }

            var apps = await _context.JobApplications
                .Where(a => a.CandidateProfileId == profile.Id)
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Select(a => new
                {
                    a.Id,
                    a.Status,
                    a.AppliedDate,
                    a.ResumePath,
                    a.AIScore,
                    a.AIFeedback,
                    Job = new
                    {
                        a.JobPosting!.Id,
                        a.JobPosting.Title,
                        a.JobPosting.Department,
                        RecruiterName = a.JobPosting.Recruiter != null ? a.JobPosting.Recruiter.Name : "System"
                    }
                })
                .ToListAsync();

            return Ok(apps);
        }

        [HttpGet("job/{jobId}")]
        [Authorize(Roles = "Recruiter,HiringManager,Administrator")]
        public async Task<IActionResult> GetJobApplications(int jobId)
        {
            var apps = await _context.JobApplications
                .Where(a => a.JobPostingId == jobId)
                .Include(a => a.CandidateProfile)
                .ThenInclude(p => p!.User)
                .Select(a => new
                {
                    a.Id,
                    a.Status,
                    a.AppliedDate,
                    a.ResumePath,
                    a.AIScore,
                    a.AIFeedback,
                    Candidate = new
                    {
                        a.CandidateProfile!.Id,
                        a.CandidateProfile.User!.Name,
                        a.CandidateProfile.User.Email,
                        a.CandidateProfile.Bio,
                        a.CandidateProfile.Skills,
                        a.CandidateProfile.Experience
                    }
                })
                .OrderByDescending(a => a.AIScore) // Sort by ranking
                .ToListAsync();

            return Ok(apps);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Recruiter,HiringManager,Administrator")]
        public async Task<IActionResult> GetApplicationById(int id)
        {
            var app = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.CandidateProfile)
                .ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            return Ok(new
            {
                app.Id,
                app.Status,
                app.AppliedDate,
                app.ResumePath,
                app.AIScore,
                app.AIFeedback,
                Job = new
                {
                    app.JobPosting!.Id,
                    app.JobPosting.Title,
                    app.JobPosting.Department,
                },
                Candidate = new
                {
                    app.CandidateProfile!.Id,
                    app.CandidateProfile.User!.Name,
                    app.CandidateProfile.User.Email,
                    app.CandidateProfile.Bio,
                    app.CandidateProfile.Skills,
                    app.CandidateProfile.Experience
                }
            });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Recruiter,HiringManager,Administrator")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var app = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.CandidateProfile)
                .ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var oldStatus = app.Status;
            app.Status = request.Status;

            // Log event
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Application Status Updated",
                Details = $"Updated application ID: {app.Id} from status '{oldStatus}' to '{app.Status}'."
            });

            await _context.SaveChangesAsync();

            // Notify candidate
            var subject = $"Application Status Update - {app.JobPosting!.Title}";
            var emailBody = $"Hello {app.CandidateProfile!.User!.Name},\n\n" +
                             $"The status of your application for the {app.JobPosting.Title} role has been updated to: **{app.Status}**.\n\n" +
                             $"We will contact you with further details if needed.\n\nBest regards,\nRecruitment Team";
            await _notificationService.SendEmailAsync(app.CandidateProfile.User.Email, subject, emailBody);

            return Ok(new { message = "Status updated successfully", status = app.Status });
        }

        [HttpPost("{id}/rescore")]
        [Authorize(Roles = "Recruiter,Administrator")]
        public async Task<IActionResult> ReScoreApplication(int id)
        {
            var app = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.CandidateProfile)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var resumeText = string.IsNullOrWhiteSpace(app.CandidateProfile!.CVContent)
                ? $"Skills: {app.CandidateProfile.Skills}. Experience: {app.CandidateProfile.Experience}. Bio: {app.CandidateProfile.Bio}."
                : app.CandidateProfile.CVContent;

            var (score, feedback) = await _aiService.ScoreCandidateAsync(
                resumeText,
                app.JobPosting!.Description,
                app.JobPosting.RequiredSkills
            );

            app.AIScore = score;
            app.AIFeedback = feedback;

            // Log event
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Application Recalculated AI Score",
                Details = $"Manually triggered AI score calculation for app ID: {app.Id}. New score: {score}%."
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "AI evaluation completed", score, feedback });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var app = await _context.JobApplications
                .Include(a => a.CandidateProfile)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
            {
                return NotFound(new { message = "Application not found" });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Authorization validation
            if (role == "Candidate")
            {
                if (app.CandidateProfile == null || app.CandidateProfile.UserId != userId)
                {
                    return Forbid();
                }

                if (app.Status != "Rejected")
                {
                    return BadRequest(new { message = "Candidates can only delete applications that have been rejected." });
                }
            }
            else if (role == "Recruiter" || role == "Administrator")
            {
                if (app.Status != "Rejected")
                {
                    return BadRequest(new { message = "Recruiters can only delete applications that have been rejected." });
                }
            }
            else
            {
                return Forbid();
            }

            _context.JobApplications.Remove(app);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Application Record Removed",
                Details = $"Permanently deleted rejected application ID: {app.Id} for Job ID: {app.JobPostingId}."
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Application deleted successfully." });
        }
    }

    public class ApplyRequest
    {
        public int JobPostingId { get; set; }
        public IFormFile? ResumeFile { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}

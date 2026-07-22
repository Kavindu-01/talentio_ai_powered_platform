using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ApplicationDbContext _context;

        public AiController(IAIService aiService, ApplicationDbContext context)
        {
            _aiService = aiService;
            _context = context;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message content is required." });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var dbUser = await _context.Users.FindAsync(userId);
            if (dbUser == null)
            {
                return Unauthorized();
            }

            // Construct real-time database context for this user session
            var userContext = $"Logged-in User: ID {dbUser.Id}, Name '{dbUser.Name}', Email '{dbUser.Email}', Role '{dbUser.Role}'.\n";

            if (dbUser.Role == Models.UserRole.Candidate)
            {
                var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    userContext += $"Profile Skills: '{profile.Skills}', Experience details: '{profile.Experience}', Bio: '{profile.Bio}'.\n";

                    // Retrieve applications using CandidateProfileId
                    var apps = await _context.JobApplications
                        .Include(a => a.JobPosting)
                        .Where(a => a.CandidateProfileId == profile.Id)
                        .ToListAsync();

                    if (apps.Count > 0)
                    {
                        userContext += "Active Job Applications in Database:\n";
                        foreach (var app in apps)
                        {
                            var scoreText = app.AIScore >= 0 ? $"{app.AIScore}%" : "Pending Evaluation";
                            userContext += $"- Job Title: '{app.JobPosting!.Title}' (Department: {app.JobPosting.Department}), Application Status: '{app.Status}', AI Suitability score: {scoreText}, Applied Date: {app.AppliedDate.ToString("yyyy-MM-dd")}.\n";
                        }
                    }
                    else
                    {
                        userContext += "Candidate Applications: None found in database.\n";
                    }
                }
                else
                {
                    userContext += "Candidate Profile: None found in database.\n";
                }
            }
            else if (dbUser.Role == Models.UserRole.Recruiter)
            {
                var recruiterJobs = await _context.JobPostings
                    .Where(j => j.RecruiterId == userId)
                    .ToListAsync();

                if (recruiterJobs.Count > 0)
                {
                    userContext += "Recruiter Active Job Postings:\n";
                    foreach (var job in recruiterJobs)
                    {
                        userContext += $"- Job ID {job.Id}: Title '{job.Title}', Department '{job.Department}', Active Status: '{job.IsActive}'.\n";
                    }
                }
            }

            var responseText = await _aiService.GetChatResponseAsync(request.Message, request.History ?? new List<ChatMessage>(), userContext);
            return Ok(new { response = responseText });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage> History { get; set; } = new();
    }
}

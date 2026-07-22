using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Recruiter,HiringManager,Administrator")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var totalJobs = await _context.JobPostings.CountAsync(j => j.IsActive);
            var totalCandidates = await _context.Users.CountAsync(u => u.Role == UserRole.Candidate);
            var totalInterviews = await _context.InterviewSchedules.CountAsync();
            var totalApplications = await _context.JobApplications.CountAsync();

            var statusBreakdown = await _context.JobApplications
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var avgAIScore = await _context.JobApplications
                .Where(a => a.AIScore >= 0)
                .AverageAsync(a => (double?)a.AIScore) ?? 0.0;

            // Application trend over recent days/months (simulated grouping by day)
            var recentApplications = await _context.JobApplications
                .OrderBy(a => a.AppliedDate)
                .Take(50)
                .Select(a => new
                {
                    Date = a.AppliedDate.ToString("yyyy-MM-dd"),
                    JobTitle = a.JobPosting != null ? a.JobPosting.Title : "Unknown Job"
                })
                .ToListAsync();

            var trendGroup = recentApplications
                .GroupBy(a => a.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            return Ok(new
            {
                TotalJobs = totalJobs,
                TotalCandidates = totalCandidates,
                TotalInterviews = totalInterviews,
                TotalApplications = totalApplications,
                AverageAIScore = Math.Round(avgAIScore, 1),
                StatusBreakdown = statusBreakdown,
                Trend = trendGroup
            });
        }

        [HttpGet("job-analytics/{jobId}")]
        public async Task<IActionResult> GetJobPostingAnalytics(int jobId)
        {
            var job = await _context.JobPostings.FindAsync(jobId);
            if (job == null) return NotFound(new { message = "Job not found" });

            var totalApplications = await _context.JobApplications.CountAsync(a => a.JobPostingId == jobId);
            
            var statusBreakdown = await _context.JobApplications
                .Where(a => a.JobPostingId == jobId)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var avgScore = await _context.JobApplications
                .Where(a => a.JobPostingId == jobId && a.AIScore >= 0)
                .AverageAsync(a => (double?)a.AIScore) ?? 0.0;

            var highestScore = await _context.JobApplications
                .Where(a => a.JobPostingId == jobId && a.AIScore >= 0)
                .MaxAsync(a => (int?)a.AIScore) ?? 0;

            return Ok(new
            {
                JobTitle = job.Title,
                TotalApplications = totalApplications,
                StatusBreakdown = statusBreakdown,
                AverageAIScore = Math.Round(avgScore, 1),
                HighestAIScore = highestScore
            });
        }
    }
}

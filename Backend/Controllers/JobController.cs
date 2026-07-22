using System.Linq;
using System.Security.Claims;
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
    [Authorize]
    public class JobController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _context.JobPostings
                .Where(j => j.IsActive)
                .Include(j => j.Recruiter)
                .Select(j => new
                {
                    j.Id,
                    j.Title,
                    j.Description,
                    j.RequiredSkills,
                    j.Department,
                    j.CreatedAt,
                    RecruiterName = j.Recruiter != null ? j.Recruiter.Name : "System"
                })
                .ToListAsync();

            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var job = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new { message = "Job posting not found" });
            }

            return Ok(new
            {
                job.Id,
                job.Title,
                job.Description,
                job.RequiredSkills,
                job.Department,
                job.CreatedAt,
                job.IsActive,
                RecruiterName = job.Recruiter != null ? job.Recruiter.Name : "System"
            });
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter,Administrator")]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var job = new JobPosting
            {
                Title = request.Title,
                Description = request.Description,
                RequiredSkills = request.RequiredSkills,
                Department = request.Department,
                RecruiterId = userId,
                IsActive = true
            };

            _context.JobPostings.Add(job);

            // Log activity
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Job Creation",
                Details = $"Created job '{job.Title}' (ID: {job.Id}) under department '{job.Department}'."
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, job);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Recruiter,Administrator")]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] CreateJobRequest request)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            // Optional: check if the recruiter owns the post (Admins can bypass)
            if (job.RecruiterId != userId && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            job.Title = request.Title;
            job.Description = request.Description;
            job.RequiredSkills = request.RequiredSkills;
            job.Department = request.Department;

            // Log activity
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Job Update",
                Details = $"Updated job '{job.Title}' (ID: {job.Id})."
            });

            await _context.SaveChangesAsync();
            return Ok(job);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Recruiter,Administrator")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            if (job.RecruiterId != userId && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            job.IsActive = false; // Soft delete

            // Log activity
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Job Deactivation",
                Details = $"Deactivated job '{job.Title}' (ID: {job.Id})."
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Job deactivated successfully" });
        }
    }

    public class CreateJobRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}

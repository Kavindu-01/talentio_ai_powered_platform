using System;

namespace Backend.Models
{
    public class JobPosting
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty; // Comma separated list of skills
        public string Department { get; set; } = string.Empty;
        public int RecruiterId { get; set; }
        public User? Recruiter { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}

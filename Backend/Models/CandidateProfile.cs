using System;

namespace Backend.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string LinkedIn { get; set; } = string.Empty;
        public string GitHub { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty; // Comma separated list of skills
        public string Experience { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Projects { get; set; } = string.Empty;
        public string ResumePath { get; set; } = string.Empty;
        public string CVContent { get; set; } = string.Empty; // Extracted/Parsed content
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

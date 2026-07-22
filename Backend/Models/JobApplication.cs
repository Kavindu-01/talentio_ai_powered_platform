using System;

namespace Backend.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobPostingId { get; set; }
        public JobPosting? JobPosting { get; set; }
        public int CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
        public string Status { get; set; } = "Applied"; // Applied, Shortlisted, Interviewing, Offered, Rejected
        public string ResumePath { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        public int AIScore { get; set; } = -1; // -1 indicates not processed yet, 0-100 score
        public string AIFeedback { get; set; } = string.Empty;
    }
}

using System;

namespace Backend.Models
{
    public class CandidateAssessmentResult
    {
        public int Id { get; set; }
        public int CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
        public int SkillAssessmentId { get; set; }
        public SkillAssessment? SkillAssessment { get; set; }
        public int Score { get; set; } // out of 100
        public bool Passed { get; set; }
        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    }
}

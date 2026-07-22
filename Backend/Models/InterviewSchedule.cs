using System;

namespace Backend.Models
{
    public class InterviewSchedule
    {
        public int Id { get; set; }
        public int JobApplicationId { get; set; }
        public JobApplication? JobApplication { get; set; }
        public DateTime InterviewDate { get; set; }
        public string LocationOrLink { get; set; } = string.Empty;
        public int InterviewerId { get; set; }
        public User? Interviewer { get; set; } // HiringManager or Recruiter
        public string Feedback { get; set; } = string.Empty;
        public int Score { get; set; } = -1; // -1 if not scored yet
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled
    }
}

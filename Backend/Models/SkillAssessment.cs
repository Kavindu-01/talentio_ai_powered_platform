using System;

namespace Backend.Models
{
    public class SkillAssessment
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string QuestionsJson { get; set; } = string.Empty; // JSON representation of questions
        public int RequiredScore { get; set; } = 70; // passing percentage
    }
}

using Microsoft.EntityFrameworkCore;
using Backend.Models;
using System;

namespace Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<InterviewSchedule> InterviewSchedules => Set<InterviewSchedule>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
        public DbSet<CandidateAssessmentResult> CandidateAssessmentResults => Set<CandidateAssessmentResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and cascading deletes
            modelBuilder.Entity<CandidateProfile>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Recruiter)
                .WithMany()
                .HasForeignKey(j => j.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.JobPosting)
                .WithMany()
                .HasForeignKey(a => a.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.CandidateProfile)
                .WithMany()
                .HasForeignKey(a => a.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InterviewSchedule>()
                .HasOne(i => i.JobApplication)
                .WithMany()
                .HasForeignKey(i => i.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InterviewSchedule>()
                .HasOne(i => i.Interviewer)
                .WithMany()
                .HasForeignKey(i => i.InterviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed Users with hashed passwords
            // BCrypt.Net.BCrypt.HashPassword("Admin123!") is "$2a$11$sRkLz8vL/x0wVjG2qBvHdu7fAep3rA07R5ZqjK.j2wYkK91qX74P."
            // BCrypt.Net.BCrypt.HashPassword("Recruiter123!") is "$2a$11$Z5n1wGqV2L.rF85W72UfJODhV8aNlA/0Q9fTz/63pEq62lT/B74P."
            // BCrypt.Net.BCrypt.HashPassword("Manager123!") is "$2a$11$C15d8f6V3L.rF85W72UfJOlhV8aNlA/0Q9fTz/63pEq62lT/B74P."
            // BCrypt.Net.BCrypt.HashPassword("Candidate123!") is "$2a$11$D15d8f6V3L.rF85W72UfJOnhV8aNlA/0Q9fTz/63pEq62lT/B74P."
            
            var adminUser = new User
            {
                Id = 1,
                Name = "System Administrator",
                Email = "admin@recruitment.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Administrator,
                IsApproved = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var recruiterUser = new User
            {
                Id = 2,
                Name = "Sarah Jenkins (Recruiter)",
                Email = "recruiter@recruitment.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recruiter123!"),
                Role = UserRole.Recruiter,
                IsApproved = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var managerUser = new User
            {
                Id = 3,
                Name = "David Kovacs (Hiring Manager)",
                Email = "manager@recruitment.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
                Role = UserRole.HiringManager,
                IsApproved = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var candidateUser = new User
            {
                Id = 4,
                Name = "Alex Rivera (Candidate)",
                Email = "candidate@recruitment.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Candidate123!"),
                Role = UserRole.Candidate,
                IsApproved = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            modelBuilder.Entity<User>().HasData(adminUser, recruiterUser, managerUser, candidateUser);

            // Seed Candidate Profile for Alex Rivera
            var candidateProfile = new CandidateProfile
            {
                Id = 1,
                UserId = 4,
                Bio = "Experienced Full Stack Developer with 4+ years of building web applications using React, ASP.NET Core, and SQLite.",
                Skills = "C#, React, ASP.NET Core, SQL, JavaScript, HTML, CSS, Git",
                Experience = "Senior Software Engineer at DevTech Solutions (2 years), Software Developer at WebSystems (2 years)",
                ResumePath = "uploads/resumes/alex_rivera_resume.txt",
                CVContent = "Alex Rivera Resume. Skills: C#, React, ASP.NET Core, SQL. Experience: Web developer with 4 years. Strong understanding of backend microservices.",
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            modelBuilder.Entity<CandidateProfile>().HasData(candidateProfile);

            // Seed Job Posting
            var jobPosting = new JobPosting
            {
                Id = 1,
                Title = "Full Stack Engineer (C# & React)",
                Description = "We are seeking a talented Full Stack Engineer to join our core development team. You will build high-quality APIs in ASP.NET Core and beautiful frontend designs in React. Experience with databases and Git is required.",
                RequiredSkills = "C#, ASP.NET Core, React, SQL, CSS",
                Department = "Engineering",
                RecruiterId = 2,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            };

            modelBuilder.Entity<JobPosting>().HasData(jobPosting);

            // Seed Job Application
            var jobApplication = new JobApplication
            {
                Id = 1,
                JobPostingId = 1,
                CandidateProfileId = 1,
                Status = "Shortlisted",
                ResumePath = "uploads/resumes/alex_rivera_resume.txt",
                AppliedDate = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                AIScore = 88,
                AIFeedback = "The candidate shows strong alignment with C# and React requirements, and has relevant professional experience. Key skills matched: C#, ASP.NET Core, React, SQL."
            };

            modelBuilder.Entity<JobApplication>().HasData(jobApplication);

            // Seed Organizations
            modelBuilder.Entity<Organization>().HasData(
                new Organization { Id = 1, Name = "TechCorp Global", Address = "123 Tech Way, Silicon Valley, CA", ContactEmail = "contact@techcorp.com", Phone = "+1-555-0199" },
                new Organization { Id = 2, Name = "Innovate Solutions Ltd", Address = "45 Innovation Plaza, London, UK", ContactEmail = "info@innovate.co.uk", Phone = "+44-20-7946-0958" }
            );

            // Seed Skill Assessments
            modelBuilder.Entity<SkillAssessment>().HasData(
                new SkillAssessment
                {
                    Id = 1,
                    Title = "C# Backend Developer Assessment",
                    Topic = "C# / ASP.NET Core",
                    RequiredScore = 70,
                    QuestionsJson = @"[
                        {""Question"":""What is the purpose of middleware in ASP.NET Core?"", ""Choices"":[""To manage frontend templates"",""To process HTTP requests and responses in the pipeline"",""To connect directly to SQL Server without ORM"",""To compile C# files to WASM""], ""Answer"":1},
                        {""Question"":""Which EF Core method executes immediately to load referenced relations synchronously?"", ""Choices"":[""Include()"",""ThenInclude()"",""Load()"",""ToList()""], ""Answer"":3},
                        {""Question"":""What does JWT authentication protect against?"", ""Choices"":[""SQL injection"",""Unauthorized endpoint access"",""Cross-Site Scripting (XSS)"",""DDoS attacks""], ""Answer"":1}
                    ]"
                },
                new SkillAssessment
                {
                    Id = 2,
                    Title = "React Frontend Developer Assessment",
                    Topic = "React / JavaScript",
                    RequiredScore = 70,
                    QuestionsJson = @"[
                        {""Question"":""What is the function of the dependency array in useEffect?"", ""Choices"":[""To load npm packages dynamically"",""To state which values trigger hook re-execution"",""To compile CSS styles"",""To configure routing transitions""], ""Answer"":1},
                        {""Question"":""Which hook is best used to share state across multiple non-nested components?"", ""Choices"":[""useState"",""useMemo"",""useContext"",""useRef""], ""Answer"":2}
                    ]"
                }
            );
        }
    }
}

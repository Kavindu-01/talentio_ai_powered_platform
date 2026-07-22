using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public class ChatMessage
    {
        public string Sender { get; set; } = string.Empty; // "user" or "bot"
        public string Text { get; set; } = string.Empty;
    }

    public interface IAIService
    {
        Task<(string Skills, string Experience, string Bio)> ParseResumeAsync(string resumeText);
        Task<(int Score, string Feedback)> ScoreCandidateAsync(string resumeText, string jobDescription, string requiredSkills);
        Task<string> GenerateAssessmentQuestionsAsync(string topic, string difficulty = "Intermediate", int questionCount = 3);
        Task<string> GetChatResponseAsync(string message, List<ChatMessage> history, string userContext);
    }

    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly ILogger<AIService> _logger;

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        }

        public async Task<(string Skills, string Experience, string Bio)> ParseResumeAsync(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Gemini API key is missing. Using local rule-based parsing fallback.");
                return ParseResumeLocalFallback(resumeText);
            }

            try
            {
                var prompt = $@"
Analyze the following candidate resume text and extract the information in clean JSON format.
The JSON must have exactly these keys: 'skills', 'experience', and 'bio'.

- 'skills': a comma-separated list of technical skills (languages, frameworks, tools, and platforms).
- 'experience': a professional executive summary of their work history, key roles, and major accomplishments (3-4 sentences), written as a cohesive narrative. Do not include raw markdown bullet points.
- 'bio': a high-quality, engaging professional biography. It should be a polished career narrative (2-3 sentences) summarizing their expertise, career aspirations, and primary focus areas, written in a compelling third-person voice. If the resume lacks a clear summary, synthesize one based on their skills and achievements.

Do not include any markdown code blocks, backticks, or comments outside the JSON. Return only raw JSON.

Resume Text:
{resumeText}
";

                var resultJson = await CallAiApiAsync(prompt);
                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;
                
                string skills = root.TryGetProperty("skills", out var sProp) ? sProp.GetString() ?? "" : "";
                string experience = root.TryGetProperty("experience", out var eProp) ? eProp.GetString() ?? "" : "";
                string bio = root.TryGetProperty("bio", out var bProp) ? bProp.GetString() ?? "" : "";

                return (skills, experience, bio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API for resume parsing. Falling back to local parser.");
                return ParseResumeLocalFallback(resumeText);
            }
        }

        public async Task<(int Score, string Feedback)> ScoreCandidateAsync(string resumeText, string jobDescription, string requiredSkills)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Gemini API key is missing. Using local rule-based scoring fallback.");
                return ScoreCandidateLocalFallback(resumeText, jobDescription, requiredSkills);
            }

            try
            {
                var prompt = $@"
Compare this candidate's resume against the job description and required skills.
Return your evaluation in JSON format with exactly these keys: 'score' and 'feedback'.
- 'score' must be an integer between 0 and 100 reflecting how well the candidate's skills and experience match the job.
- 'feedback' must be a concise (3-4 sentences) assessment of their strengths, gaps, and why they received this score.

Do not include any markdown formatting or backticks outside the JSON. Return only raw JSON.

Job Description:
{jobDescription}

Required Skills:
{requiredSkills}

Resume:
{resumeText}
";

                var resultJson = await CallAiApiAsync(prompt);
                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;

                int score = root.TryGetProperty("score", out var sProp) ? sProp.GetInt32() : 50;
                string feedback = root.TryGetProperty("feedback", out var fProp) ? fProp.GetString() ?? "" : "";

                return (score, feedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API for candidate scoring. Falling back to local scoring.");
                return ScoreCandidateLocalFallback(resumeText, jobDescription, requiredSkills);
            }
        }

        public async Task<string> GenerateAssessmentQuestionsAsync(string topic, string difficulty = "Intermediate", int questionCount = 3)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("AI API key is missing. Using fallback questions.");
                return GenerateQuestionsFallback(topic);
            }

            try
            {
                var prompt = $@"
Generate {questionCount} {difficulty} difficulty level multiple-choice test questions for evaluating candidate proficiency in the skill topic '{topic}'.
Difficulty target level: '{difficulty}'. Make sure the questions reflect true {difficulty} level complexity (Beginner = fundamental syntax & concepts, Intermediate = practical application & design patterns, Expert = deep internal mechanics & edge cases, Super-Expert = complex architecture, performance optimization & deep technical internals).

Return ONLY a valid JSON array of objects without markdown formatting or code fences.

Each object in the array must have:
- 'Question': string text of the question
- 'Choices': array of exactly 4 plausible option strings
- 'CorrectIndex': 0-based integer index (0, 1, 2, or 3) indicating the correct answer choice in the 'Choices' array.

Example structure:
[
  {{
    ""Question"": ""What is ...?"",
    ""Choices"": [""Option A"", ""Option B"", ""Option C"", ""Option D""],
    ""CorrectIndex"": 0
  }}
]
";

                var resultJson = await CallAiApiAsync(prompt);
                return resultJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI questions for topic {Topic}. Using fallback.", topic);
                return GenerateQuestionsFallback(topic);
            }
        }

        private string GenerateQuestionsFallback(string topic)
        {
            var fallback = new[]
            {
                new { Question = $"What is a fundamental principle when developing applications in {topic}?", Choices = new[] { "Modularity & Clean Architecture", "Hardcoding secret credentials", "Bypassing automated testing", "Ignoring error handling" }, CorrectIndex = 0 },
                new { Question = $"Which tool or method is standard when managing code in {topic} projects?", Choices = new[] { "Version Control (Git)", "Emailing zip archives", "FTP file editing directly on server", "Disabling audit tracking" }, CorrectIndex = 0 },
                new { Question = $"What is considered a best practice for production {topic} deployments?", Choices = new[] { "Input validation & security checks", "Plaintext password storage", "Disabling application logs", "Skipping build verification" }, CorrectIndex = 0 }
            };

            return JsonSerializer.Serialize(fallback);
        }

        public async Task<string> GetChatResponseAsync(string message, List<ChatMessage> history, string userContext)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "I am the Talentio Talent Intelligence Assistant. Currently, my AI brain key is not configured, but I am ready to help you once the connection is established!";
            }

            try
            {
                var historyPrompt = "";
                if (history != null && history.Count > 0)
                {
                    historyPrompt = "Here is the previous conversation history for context:\n";
                    foreach (var msg in history.TakeLast(6)) // Include last 6 messages
                    {
                        historyPrompt += $"{(msg.Sender == "user" ? "User" : "Assistant")}: {msg.Text}\n";
                    }
                    historyPrompt += "\n";
                }

                var prompt = $@"
You are the Talentio Talent Intelligence Assistant, a professional, friendly, and helpful AI assistant embedded inside the 'Talentio' Recruitment & Talent Management platform.
Your purpose is to assist candidates with their profiles, job search, recommendations, and assessments, and to assist recruiters and hiring managers with candidate suitability, evaluations, job posts, and platform navigation.

Never mention specific AI models like Gemini or Groq, and do not use raw AI developer jargon. Focus on corporate talent intelligence. Keep your answers concise, structured, and helpful. Use markdown formatting sparingly for bold text or clean lists if needed.

--------------------------------------------------
USER & SYSTEM DATABASE CONTEXT:
{userContext}
--------------------------------------------------

{historyPrompt}
New user message: '{message}'
Response:";

                return await CallAiApiAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response from AI.");
                return "I apologize, but I encountered an error while processing your request. Please try again in a moment!";
            }
        }

        private async Task<string> CallAiApiAsync(string prompt)
        {
            if (!string.IsNullOrWhiteSpace(_apiKey) && _apiKey.StartsWith("gsk_"))
            {
                return await CallGroqApiAsync(prompt);
            }
            return await CallGeminiApiAsync(prompt);
        }

        private async Task<string> CallGroqApiAsync(string prompt)
        {
            var url = "https://api.groq.com/openai/v1/chat/completions";
            
            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            
            var candidateText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(candidateText))
            {
                throw new Exception("Empty response received from Groq API");
            }

            var cleanText = candidateText.Trim();
            if (cleanText.StartsWith("```json"))
            {
                cleanText = cleanText.Substring(7).Trim();
            }
            else if (cleanText.StartsWith("```"))
            {
                cleanText = cleanText.Substring(3).Trim();
            }
            if (cleanText.EndsWith("```"))
            {
                cleanText = cleanText.Substring(0, cleanText.Length - 3).Trim();
            }

            return cleanText;
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            
            var candidateText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(candidateText))
            {
                throw new Exception("Empty response received from Gemini API");
            }

            // Sanitization: Remove markdown fence block markers if Gemini returned them
            var cleanText = candidateText.Trim();
            if (cleanText.StartsWith("```json"))
            {
                cleanText = cleanText.Substring(7).Trim();
            }
            else if (cleanText.StartsWith("```"))
            {
                cleanText = cleanText.Substring(3).Trim();
            }
            if (cleanText.EndsWith("```"))
            {
                cleanText = cleanText.Substring(0, cleanText.Length - 3).Trim();
            }

            return cleanText;
        }

        private (string Skills, string Experience, string Bio) ParseResumeLocalFallback(string text)
        {
            var skillsList = new StringBuilder();
            var keywords = new[] { "c#", "net", "react", "sql", "javascript", "angular", "vue", "html", "css", "python", "java", "git", "cloud", "aws", "azure", "docker" };
            
            var lowerText = text.ToLower();
            foreach (var kw in keywords)
            {
                if (lowerText.Contains(kw))
                {
                    if (skillsList.Length > 0) skillsList.Append(", ");
                    if (kw == "net") skillsList.Append(".NET");
                    else if (kw == "c#") skillsList.Append("C#");
                    else if (kw == "sql") skillsList.Append("SQL");
                    else if (kw == "html") skillsList.Append("HTML");
                    else if (kw == "css") skillsList.Append("CSS");
                    else if (kw == "aws") skillsList.Append("AWS");
                    else skillsList.Append(char.ToUpper(kw[0]) + kw.Substring(1));
                }
            }

            // Extract Bio / Summary locally
            string bio = "";
            var summaryKeywords = new[] { "summary", "profile", "objective", "about me", "professional biography", "professional summary" };
            foreach (var kw in summaryKeywords)
            {
                int idx = lowerText.IndexOf(kw);
                if (idx != -1)
                {
                    var start = idx + kw.Length;
                    var length = Math.Min(250, text.Length - start);
                    bio = text.Substring(start, length).Trim(':').Trim();

                    // Boundary checks
                    int expBreak = bio.ToLower().IndexOf("experience");
                    if (expBreak != -1) bio = bio.Substring(0, expBreak);
                    int eduBreak = bio.ToLower().IndexOf("education");
                    if (eduBreak != -1) bio = bio.Substring(0, eduBreak);
                    break;
                }
            }

            bio = StripContactInfo(bio);

            if (string.IsNullOrWhiteSpace(bio))
            {
                var skillsStr = skillsList.Length > 0 ? skillsList.ToString() : "C#, React, and SQL";
                bio = $"A skilled software engineering professional specializing in {skillsStr}. Experienced in developing modern applications, designing database solutions, and writing high-quality code across the full development lifecycle.";
            }

            // Extract Experience locally
            string experience = "";
            var expKeywords = new[] { "experience", "work history", "employment", "professional history" };
            string rawExp = "";
            foreach (var kw in expKeywords)
            {
                int idx = lowerText.IndexOf(kw);
                if (idx != -1)
                {
                    var start = idx + kw.Length;
                    var length = Math.Min(800, text.Length - start);
                    rawExp = text.Substring(start, length).Trim(':').Trim();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(rawExp))
            {
                rawExp = text;
            }

            // Summarize the extracted text intelligently
            var companies = new System.Collections.Generic.List<string>();
            var commonCompanies = new[] { "Google", "Microsoft", "Amazon", "Meta", "Apple", "Netflix", "Twitter", "Intel", "IBM", "Oracle", "Cisco", "Salesforce", "Georgia Institute of Technology" };
            foreach (var comp in commonCompanies)
            {
                if (rawExp.Contains(comp) || text.Contains(comp))
                {
                    companies.Add(comp);
                }
            }

            var titles = new System.Collections.Generic.List<string>();
            var commonTitles = new[] { "Software Engineer", "Developer", "Systems Analyst", "Technical Lead", "Data Scientist", "Full Stack Developer", "Backend Engineer", "Research Assistant" };
            foreach (var title in commonTitles)
            {
                if (rawExp.ToLower().Contains(title.ToLower()) || text.ToLower().Contains(title.ToLower()))
                {
                    titles.Add(title);
                }
            }

            var summaryBuilder = new StringBuilder();
            if (titles.Count > 0)
            {
                summaryBuilder.Append($"Experienced {string.Join(" and ", titles.GetRange(0, Math.Min(2, titles.Count)))}");
            }
            else
            {
                summaryBuilder.Append("Technical professional with strong engineering experience");
            }

            if (companies.Count > 0)
            {
                summaryBuilder.Append($" with a demonstrated history of working at {string.Join(" and ", companies.GetRange(0, Math.Min(2, companies.Count)))}");
            }

            summaryBuilder.Append(". Skilled in software design, application development, and systems scalability. ");
            summaryBuilder.Append("Contributed to key projects, API development, and technical problem-solving to deliver robust product solutions.");

            experience = summaryBuilder.ToString();

            // Format strings
            bio = CleanTextFormatting(bio);
            experience = CleanTextFormatting(experience);

            return (skillsList.Length > 0 ? skillsList.ToString() : "C#, React, SQL", experience, bio);
        }

        private string StripContactInfo(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var parts = input.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var cleanParts = new System.Collections.Generic.List<string>();
            foreach (var part in parts)
            {
                var lower = part.ToLower();
                if (lower.Contains("@") || lower.Contains("http") || lower.Contains("www.") || 
                    lower.Contains("+1") || lower.Contains("phone") || lower.Contains("mobile") ||
                    lower.Contains("email") || lower.Contains("github.com") || lower.Contains("linkedin.com") ||
                    lower.Contains("gpa:") || lower.Contains("aug2012") || lower.Contains("dec201"))
                {
                    continue;
                }
                cleanParts.Add(part);
            }
            return string.Join(" ", cleanParts).Trim();
        }

        private string CleanTextFormatting(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            
            var cleaned = input
                .Replace("\r", "")
                .Replace("\t", " ");
                
            var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var cleanLines = new System.Collections.Generic.List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                
                while (trimmed.Contains("  "))
                {
                    trimmed = trimmed.Replace("  ", " ");
                }
                
                if (trimmed.StartsWith("◦") || trimmed.StartsWith("•") || trimmed.StartsWith("▪") || trimmed.StartsWith("-"))
                {
                    trimmed = "• " + trimmed.TrimStart('◦', '•', '▪', '-', ' ').Trim();
                }
                
                cleanLines.Add(trimmed);
            }
            
            return string.Join("\n", cleanLines);
        }

        private (int Score, string Feedback) ScoreCandidateLocalFallback(string resumeText, string jobDescription, string requiredSkills)
        {
            var lowerResume = resumeText.ToLower();
            var skills = requiredSkills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            if (skills.Length == 0)
            {
                return (60, "Job requires general skills. Resume matches base criteria.");
            }

            int matches = 0;
            var matchedSkills = new StringBuilder();
            
            foreach (var skill in skills)
            {
                if (lowerResume.Contains(skill.ToLower()))
                {
                    matches++;
                    if (matchedSkills.Length > 0) matchedSkills.Append(", ");
                    matchedSkills.Append(skill);
                }
            }

            double percentage = (double)matches / skills.Length;
            int score = (int)Math.Round(50 + (percentage * 50)); // Base score of 50, scale up to 100

            string feedback = $"Candidate matched {matches} of {skills.Length} required skills. " +
                               $"Matched: [{matchedSkills}]. Gaps: [{string.Join(", ", Array.FindAll(skills, s => !lowerResume.Contains(s.ToLower())))}].";

            return (score, feedback);
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public interface ICalendarService
    {
        Task ScheduleMeetingAsync(string title, DateTime startTime, DateTime endTime, string attendeeEmail, string locationOrLink);
        string GenerateICalendarInvite(string title, DateTime startTime, DateTime endTime, string attendeeEmail, string locationOrLink);
    }

    public class CalendarService : ICalendarService
    {
        private readonly ILogger<CalendarService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _logFilePath;

        public CalendarService(ILogger<CalendarService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _logFilePath = Path.Combine(AppContext.BaseDirectory, "Logs", "SimulatedCalendar.log");

            // Ensure folder exists
            var directory = Path.GetDirectoryName(_logFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task ScheduleMeetingAsync(string title, DateTime startTime, DateTime endTime, string attendeeEmail, string locationOrLink)
        {
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] CALENDAR EVENT SCHEDULED\n" +
                           $"TITLE: {title}\n" +
                           $"ATTENDEE: {attendeeEmail}\n" +
                           $"START: {startTime:yyyy-MM-dd HH:mm:ss}\n" +
                           $"END: {endTime:yyyy-MM-dd HH:mm:ss}\n" +
                           $"LINK/LOCATION: {locationOrLink}\n" +
                           $"--------------------------------------------------\n";
            await File.AppendAllTextAsync(_logFilePath, logEntry);

            // Generate iCalendar (.ics) format string for Google / Outlook auto-add
            var icsContent = GenerateICalendarInvite(title, startTime, endTime, attendeeEmail, locationOrLink);
            
            var provider = _configuration["Calendar:Provider"] ?? "iCalEmail";
            if (provider.Equals("GoogleCalendarAPI", StringComparison.OrdinalIgnoreCase))
            {
                // Direct Google Calendar API OAuth2 Integration (Google.Apis.Calendar.v3)
                // var service = new CalendarService(...);
                // Event newEvent = new Event { Summary = title, Location = locationOrLink, Start = ..., End = ... };
                // await service.Events.Insert(newEvent, "primary").ExecuteAsync();
                _logger.LogInformation("Google Calendar API event creation invoked for {Attendee}", attendeeEmail);
            }
            else if (provider.Equals("MicrosoftGraphAPI", StringComparison.OrdinalIgnoreCase))
            {
                // Direct Microsoft Outlook / Teams Graph API Integration (Microsoft.Graph)
                // var graphClient = new GraphServiceClient(...);
                // var event = new Event { Subject = title, Start = ..., End = ..., IsOnlineMeeting = true };
                // await graphClient.Me.Events.Request().AddAsync(event);
                _logger.LogInformation("Microsoft Graph Outlook API event creation invoked for {Attendee}", attendeeEmail);
            }

            _logger.LogInformation("Calendar event '{Title}' scheduled for {Attendee}", title, attendeeEmail);
        }

        public string GenerateICalendarInvite(string title, DateTime startTime, DateTime endTime, string attendeeEmail, string locationOrLink)
        {
            var sb = new StringBuilder();
            var now = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var start = startTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var end = endTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var uid = Guid.NewGuid().ToString();

            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Talentio//Recruitment Management System//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:REQUEST");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{now}");
            sb.AppendLine($"DTSTART:{start}");
            sb.AppendLine($"DTEND:{end}");
            sb.AppendLine($"SUMMARY:{title}");
            sb.AppendLine($"DESCRIPTION:Interview Scheduled via Talentio Recruitment Platform. Meeting Link: {locationOrLink}");
            sb.AppendLine($"LOCATION:{locationOrLink}");
            sb.AppendLine($"STATUS:CONFIRMED");
            sb.AppendLine($"SEQUENCE:0");
            sb.AppendLine($"ATTENDEE;ROLE=REQ-PARTICIPANT;PARTSTAT=NEEDS-ACTION;RSVP=TRUE:mailto:{attendeeEmail}");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            return sb.ToString();
        }
    }
}

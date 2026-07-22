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
    public class OrganizationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrganizationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            var orgs = await _context.Organizations.ToListAsync();
            return Ok(orgs);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrgRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var org = new Organization
            {
                Name = request.Name,
                Address = request.Address,
                ContactEmail = request.ContactEmail,
                Phone = request.Phone
            };

            _context.Organizations.Add(org);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Organization Created",
                Details = $"Created organization record '{org.Name}'."
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Organization created successfully", org });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateOrganization(int id, [FromBody] CreateOrgRequest request)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                return NotFound(new { message = "Organization not found" });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            org.Name = request.Name;
            org.Address = request.Address;
            org.ContactEmail = request.ContactEmail;
            org.Phone = request.Phone;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Organization Updated",
                Details = $"Updated organization record '{org.Name}' (ID: {org.Id})."
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Organization updated successfully", org });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
            {
                return NotFound(new { message = "Organization not found" });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            _context.Organizations.Remove(org);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Organization Deleted",
                Details = $"Deleted organization record '{org.Name}' (ID: {org.Id})."
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Organization deleted successfully" });
        }
    }

    public class CreateOrgRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}

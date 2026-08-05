using AssignmentSystem.Api.Data;
using AssignmentSystem.Api.DTOs;
using AssignmentSystem.Api.Middleware;
using AssignmentSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Only Admin manages users
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext db, ILogger<UsersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll([FromQuery] string? role)
    {
        var query = _db.Users.Include(u => u.SchoolClass).AsQueryable();

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        var users = await query
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new UserResponse(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive,
                u.SchoolClassId, u.SchoolClass != null ? u.SchoolClass.Name : null))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var u = await _db.Users.Include(x => x.SchoolClass).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"User {id} not found.");

        return Ok(new UserResponse(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive,
            u.SchoolClassId, u.SchoolClass?.Name));
    }

    /// <summary>Admin creates a new user account (Admin, Teacher, or Student).</summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(RegisterUserRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new BusinessRuleException("Role must be one of: Admin, Teacher, Student.");

        var emailLower = request.Email.ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == emailLower))
            throw new BusinessRuleException("A user with this email already exists.");

        if (role == UserRole.Student)
        {
            if (request.SchoolClassId is null or <= 0)
                throw new BusinessRuleException("Please select a class for the student account.");

            var classExists = await _db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId);
            if (!classExists)
                throw new NotFoundException($"Class {request.SchoolClassId} not found.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = emailLower,
            Role = role,
            SchoolClassId = role == UserRole.Student ? request.SchoolClassId : null,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin created user {Email} with role {Role}", user.Email, user.Role);

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new UserResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive, user.SchoolClassId, null));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.Include(u => u.SchoolClass).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException($"User {id} not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        if (request.SchoolClassId.HasValue && user.Role == UserRole.Student)
        {
            if (request.SchoolClassId.Value <= 0)
                throw new BusinessRuleException("Please select a valid class.");

            var classExists = await _db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId);
            if (!classExists)
                throw new NotFoundException($"Class {request.SchoolClassId} not found.");

            user.SchoolClassId = request.SchoolClassId;
        }

        await _db.SaveChangesAsync();

        return Ok(new UserResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive,
            user.SchoolClassId, user.SchoolClass?.Name));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException($"User {id} not found.");

        // Soft-delete pattern: deactivate rather than hard-delete, to preserve
        // referential integrity with assignments/submissions already created.
        user.IsActive = false;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin deactivated user {UserId}", id);
        return NoContent();
    }
}

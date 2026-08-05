using System.Security.Claims;
using AssignmentSystem.Api.Data;
using AssignmentSystem.Api.DTOs;
using AssignmentSystem.Api.Middleware;
using AssignmentSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(ApplicationDbContext db, ILogger<AssignmentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    private static AssignmentResponse ToResponse(Assignment a) => new(
        a.Id, a.Title, a.Description, a.Deadline, a.MaxMarks, a.Status.ToString(), a.AllowResubmission,
        a.SchoolClassId, a.SchoolClass.Name, a.SubjectId, a.Subject.Name,
        a.CreatedByTeacherId, a.CreatedByTeacher.FullName, a.CreatedAt,
        DateTime.UtcNow > a.Deadline, a.Submissions.Count);

    /// <summary>
    /// Role-aware listing:
    /// - Admin sees everything.
    /// - Teacher sees assignments they created.
    /// - Student sees only PUBLISHED assignments for their own class.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssignmentResponse>>> GetAll()
    {
        var query = _db.Assignments
            .Include(a => a.SchoolClass)
            .Include(a => a.Subject)
            .Include(a => a.CreatedByTeacher)
            .Include(a => a.Submissions)
            .AsQueryable();

        switch (CurrentRole)
        {
            case "Teacher":
                query = query.Where(a => a.CreatedByTeacherId == CurrentUserId);
                break;
            case "Student":
                var student = await _db.Users.FindAsync(CurrentUserId)
                    ?? throw new NotFoundException("Student not found.");
                if (student.SchoolClassId is null) return Ok(Array.Empty<AssignmentResponse>());

                query = query.Where(a => a.SchoolClassId == student.SchoolClassId
                                          && a.Status == AssignmentStatus.Published);
                break;
                // Admin: no filter
        }

        var results = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(results.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssignmentResponse>> GetById(int id)
    {
        var a = await _db.Assignments
            .Include(x => x.SchoolClass).Include(x => x.Subject)
            .Include(x => x.CreatedByTeacher).Include(x => x.Submissions)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Assignment {id} not found.");

        if (CurrentRole == "Student")
        {
            var student = await _db.Users.FindAsync(CurrentUserId);
            var visible = a.Status == AssignmentStatus.Published && a.SchoolClassId == student?.SchoolClassId;
            if (!visible) throw new ForbiddenException("You do not have access to this assignment.");
        }
        else if (CurrentRole == "Teacher" && a.CreatedByTeacherId != CurrentUserId)
        {
            throw new ForbiddenException("You can only view assignments you created.");
        }

        return Ok(ToResponse(a));
    }

    /// <summary>Teacher creates an assignment. Must be assigned to teach that subject.</summary>
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<AssignmentResponse>> Create(CreateAssignmentRequest request)
    {
        if (request.SchoolClassId <= 0)
            throw new BusinessRuleException("Please select a class.");
        if (request.SubjectId <= 0)
            throw new BusinessRuleException("Please select a subject.");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId)
            ?? throw new NotFoundException("Subject not found.");
        if (subject.SchoolClassId != request.SchoolClassId)
            throw new BusinessRuleException("The selected subject does not belong to the selected class.");

        var isAssigned = await _db.TeacherSubjectAssignments
            .AnyAsync(t => t.TeacherId == CurrentUserId && t.SubjectId == request.SubjectId);
        if (!isAssigned)
            throw new ForbiddenException("You are not assigned to teach this subject.");

        if (request.Deadline <= DateTime.UtcNow)
            throw new BusinessRuleException("Deadline must be in the future.");

        var entity = new Assignment
        {
            Title = request.Title,
            Description = request.Description,
            Deadline = request.Deadline,
            MaxMarks = request.MaxMarks,
            SchoolClassId = request.SchoolClassId,
            SubjectId = request.SubjectId,
            AllowResubmission = request.AllowResubmission,
            CreatedByTeacherId = CurrentUserId,
            Status = request.PublishNow ? AssignmentStatus.Published : AssignmentStatus.Draft
        };

        _db.Assignments.Add(entity);
        await _db.SaveChangesAsync();

        await _db.Entry(entity).Reference(a => a.SchoolClass).LoadAsync();
        await _db.Entry(entity).Reference(a => a.Subject).LoadAsync();
        await _db.Entry(entity).Reference(a => a.CreatedByTeacher).LoadAsync();

        _logger.LogInformation("Teacher {TeacherId} created assignment {AssignmentId} ({Status})",
            CurrentUserId, entity.Id, entity.Status);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<AssignmentResponse>> Update(int id, UpdateAssignmentRequest request)
    {
        var a = await _db.Assignments
            .Include(x => x.SchoolClass).Include(x => x.Subject).Include(x => x.CreatedByTeacher)
            .Include(x => x.Submissions)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Assignment {id} not found.");

        if (a.CreatedByTeacherId != CurrentUserId)
            throw new ForbiddenException("You can only edit assignments you created.");

        if (a.Submissions.Any())
            throw new BusinessRuleException("Cannot edit an assignment that already has submissions.");

        if (!string.IsNullOrWhiteSpace(request.Title)) a.Title = request.Title;
        if (request.Description is not null) a.Description = request.Description;
        if (request.Deadline.HasValue)
        {
            if (request.Deadline.Value <= DateTime.UtcNow)
                throw new BusinessRuleException("Deadline must be in the future.");
            a.Deadline = request.Deadline.Value;
        }
        if (request.MaxMarks.HasValue) a.MaxMarks = request.MaxMarks.Value;
        if (request.AllowResubmission.HasValue) a.AllowResubmission = request.AllowResubmission.Value;

        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(a));
    }

    /// <summary>Publish a draft assignment, or revert a published one back to draft.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<AssignmentResponse>> SetStatus(int id, [FromBody] string status)
    {
        var a = await _db.Assignments
            .Include(x => x.SchoolClass).Include(x => x.Subject).Include(x => x.CreatedByTeacher)
            .Include(x => x.Submissions)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Assignment {id} not found.");

        if (a.CreatedByTeacherId != CurrentUserId)
            throw new ForbiddenException("You can only manage assignments you created.");

        if (!Enum.TryParse<AssignmentStatus>(status, true, out var newStatus))
            throw new BusinessRuleException("Status must be 'Draft' or 'Published'.");

        if (newStatus == AssignmentStatus.Draft && a.Submissions.Any())
            throw new BusinessRuleException("Cannot revert to draft: students have already submitted.");

        a.Status = newStatus;
        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(a));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.Assignments.Include(x => x.Submissions).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Assignment {id} not found.");

        if (a.CreatedByTeacherId != CurrentUserId)
            throw new ForbiddenException("You can only delete assignments you created.");

        if (a.Submissions.Any())
            throw new BusinessRuleException("Cannot delete an assignment that already has submissions.");

        _db.Assignments.Remove(a);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

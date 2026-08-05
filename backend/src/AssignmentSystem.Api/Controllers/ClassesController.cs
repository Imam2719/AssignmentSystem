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
public class ClassesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ClassesController(ApplicationDbContext db) => _db = db;

    /// <summary>Any authenticated user can list classes (needed for dropdowns, filters).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolClassResponse>>> GetAll()
    {
        var classes = await _db.SchoolClasses
            .OrderBy(c => c.Name)
            .Select(c => new SchoolClassResponse(c.Id, c.Name, c.Description,
                c.Students.Count, c.Subjects.Count))
            .ToListAsync();

        return Ok(classes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SchoolClassResponse>> GetById(int id)
    {
        var c = await _db.SchoolClasses
            .Include(x => x.Students)
            .Include(x => x.Subjects)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Class {id} not found.");

        return Ok(new SchoolClassResponse(c.Id, c.Name, c.Description, c.Students.Count, c.Subjects.Count));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolClassResponse>> Create(SchoolClassRequest request)
    {
        var entity = new SchoolClass { Name = request.Name, Description = request.Description };
        _db.SchoolClasses.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new SchoolClassResponse(entity.Id, entity.Name, entity.Description, 0, 0));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolClassResponse>> Update(int id, SchoolClassRequest request)
    {
        var entity = await _db.SchoolClasses
            .Include(x => x.Students)
            .Include(x => x.Subjects)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Class {id} not found.");

        entity.Name = request.Name;
        entity.Description = request.Description;
        await _db.SaveChangesAsync();

        return Ok(new SchoolClassResponse(entity.Id, entity.Name, entity.Description,
            entity.Students.Count, entity.Subjects.Count));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.SchoolClasses.Include(c => c.Subjects).Include(c => c.Students)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException($"Class {id} not found.");

        if (entity.Subjects.Any() || entity.Students.Any())
            throw new BusinessRuleException("Cannot delete a class that still has subjects or enrolled students.");

        _db.SchoolClasses.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Subjects (nested under classes for convenience) ----------

    [HttpGet("subjects")]
    public async Task<ActionResult<IEnumerable<SubjectResponse>>> GetAllSubjects([FromQuery] int? schoolClassId)
    {
        var query = _db.Subjects.Include(s => s.SchoolClass).AsQueryable();
        if (schoolClassId.HasValue) query = query.Where(s => s.SchoolClassId == schoolClassId);

        var subjects = await query
            .OrderBy(s => s.SchoolClass.Name).ThenBy(s => s.Name)
            .Select(s => new SubjectResponse(s.Id, s.Name, s.Code, s.SchoolClassId, s.SchoolClass.Name))
            .ToListAsync();

        return Ok(subjects);
    }

    /// <summary>Teacher: list only the subjects THEY are assigned to teach (for the assignment-creation dropdown).</summary>
    [HttpGet("subjects/mine")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<IEnumerable<SubjectResponse>>> GetMySubjects()
    {
        var teacherId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var subjects = await _db.TeacherSubjectAssignments
            .Where(t => t.TeacherId == teacherId)
            .Include(t => t.Subject).ThenInclude(s => s.SchoolClass)
            .OrderBy(t => t.Subject.SchoolClass.Name).ThenBy(t => t.Subject.Name)
            .Select(t => new SubjectResponse(t.Subject.Id, t.Subject.Name, t.Subject.Code,
                t.Subject.SchoolClassId, t.Subject.SchoolClass.Name))
            .ToListAsync();

        return Ok(subjects);
    }

    [HttpPost("subjects")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectResponse>> CreateSubject(SubjectRequest request)
    {
        if (request.SchoolClassId <= 0)
            throw new BusinessRuleException("Please select a class.");

        var classExists = await _db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId);
        if (!classExists) throw new NotFoundException($"Class {request.SchoolClassId} not found.");

        var entity = new Subject { Name = request.Name, Code = request.Code, SchoolClassId = request.SchoolClassId };
        _db.Subjects.Add(entity);
        await _db.SaveChangesAsync();

        var className = (await _db.SchoolClasses.FindAsync(request.SchoolClassId))!.Name;
        return Ok(new SubjectResponse(entity.Id, entity.Name, entity.Code, entity.SchoolClassId, className));
    }

    [HttpDelete("subjects/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var entity = await _db.Subjects.Include(s => s.Assignments).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException($"Subject {id} not found.");

        if (entity.Assignments.Any())
            throw new BusinessRuleException("Cannot delete a subject that has assignments.");

        _db.Subjects.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Teacher <-> Subject assignment ----------

    [HttpPost("assign-teacher")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignTeacher(AssignTeacherRequest request)
    {
        if (request.TeacherId <= 0)
            throw new BusinessRuleException("Please select a teacher.");
        if (request.SubjectId <= 0)
            throw new BusinessRuleException("Please select a subject.");

        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TeacherId && u.Role == UserRole.Teacher)
            ?? throw new NotFoundException("Teacher not found.");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId)
            ?? throw new NotFoundException("Subject not found.");

        var alreadyAssigned = await _db.TeacherSubjectAssignments
            .AnyAsync(t => t.TeacherId == request.TeacherId && t.SubjectId == request.SubjectId);
        if (alreadyAssigned) throw new BusinessRuleException("This teacher is already assigned to this subject.");

        _db.TeacherSubjectAssignments.Add(new TeacherSubjectAssignment
        {
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = $"{teacher.FullName} assigned to {subject.Name}." });
    }

    [HttpDelete("assign-teacher/{assignmentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTeacherAssignment(int assignmentId)
    {
        var entity = await _db.TeacherSubjectAssignments.FirstOrDefaultAsync(t => t.Id == assignmentId)
            ?? throw new NotFoundException("Assignment mapping not found.");

        _db.TeacherSubjectAssignments.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

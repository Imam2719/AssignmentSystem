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
public class SubmissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(ApplicationDbContext db, ILogger<SubmissionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    private static SubmissionResponse ToResponse(Submission s) => new(
        s.Id, s.AssignmentId, s.Assignment.Title, s.Assignment.MaxMarks,
        s.StudentId, s.Student.FullName, s.AnswerText, s.AttachmentUrl,
        s.Status.ToString(), s.SubmittedAt, s.UpdatedAt, s.MarksObtained, s.Feedback, s.GradedAt);

    /// <summary>Student submits an answer for a published assignment (before deadline).</summary>
    [HttpPost("assignments/{assignmentId:int}")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<SubmissionResponse>> Submit(int assignmentId, CreateSubmissionRequest request)
    {
        var assignment = await _db.Assignments.Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == assignmentId)
            ?? throw new NotFoundException("Assignment not found.");

        var student = await _db.Users.FindAsync(CurrentUserId)!;

        if (assignment.Status != AssignmentStatus.Published)
            throw new ForbiddenException("This assignment is not published yet.");

        if (student!.SchoolClassId != assignment.SchoolClassId)
            throw new ForbiddenException("This assignment is not for your class.");

        if (DateTime.UtcNow > assignment.Deadline)
            throw new BusinessRuleException("The deadline has passed; submission is no longer allowed.");

        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == CurrentUserId);
        if (existing is not null)
            throw new BusinessRuleException("You already submitted this assignment. Use update instead.");

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = CurrentUserId,
            AnswerText = request.AnswerText,
            AttachmentUrl = request.AttachmentUrl,
            Status = SubmissionStatus.Submitted
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        await _db.Entry(submission).Reference(s => s.Assignment).LoadAsync();
        await _db.Entry(submission).Reference(s => s.Student).LoadAsync();

        _logger.LogInformation("Student {StudentId} submitted assignment {AssignmentId}", CurrentUserId, assignmentId);

        return CreatedAtAction(nameof(GetById), new { id = submission.Id }, ToResponse(submission));
    }

    /// <summary>Student updates their own submission, only before the deadline and only if allowed.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<SubmissionResponse>> Update(int id, UpdateSubmissionRequest request)
    {
        var submission = await _db.Submissions
            .Include(s => s.Assignment).Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Submission not found.");

        if (submission.StudentId != CurrentUserId)
            throw new ForbiddenException("You can only update your own submission.");

        if (!submission.Assignment.AllowResubmission)
            throw new BusinessRuleException("This assignment does not allow resubmission.");

        if (DateTime.UtcNow > submission.Assignment.Deadline)
            throw new BusinessRuleException("The deadline has passed; you can no longer update your submission.");

        if (submission.Status == SubmissionStatus.Graded)
            throw new BusinessRuleException("This submission has already been graded and cannot be changed.");

        submission.AnswerText = request.AnswerText;
        submission.AttachmentUrl = request.AttachmentUrl;
        submission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(submission));
    }

    /// <summary>List submissions. Teacher: submissions for their assignments. Student: their own. Admin: all.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubmissionResponse>>> GetAll([FromQuery] int? assignmentId)
    {
        var query = _db.Submissions.Include(s => s.Assignment).Include(s => s.Student).AsQueryable();

        if (assignmentId.HasValue) query = query.Where(s => s.AssignmentId == assignmentId);

        query = CurrentRole switch
        {
            "Student" => query.Where(s => s.StudentId == CurrentUserId),
            "Teacher" => query.Where(s => s.Assignment.CreatedByTeacherId == CurrentUserId),
            _ => query // Admin sees all
        };

        var results = await query.OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return Ok(results.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubmissionResponse>> GetById(int id)
    {
        var s = await _db.Submissions.Include(x => x.Assignment).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Submission not found.");

        var allowed = CurrentRole switch
        {
            "Admin" => true,
            "Teacher" => s.Assignment.CreatedByTeacherId == CurrentUserId,
            "Student" => s.StudentId == CurrentUserId,
            _ => false
        };
        if (!allowed) throw new ForbiddenException("You do not have access to this submission.");

        return Ok(ToResponse(s));
    }

    /// <summary>Teacher grades a submission: marks (<= assignment MaxMarks) + feedback.</summary>
    [HttpPost("{id:int}/grade")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<SubmissionResponse>> Grade(int id, GradeSubmissionRequest request)
    {
        var s = await _db.Submissions.Include(x => x.Assignment).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Submission not found.");

        if (s.Assignment.CreatedByTeacherId != CurrentUserId)
            throw new ForbiddenException("You can only grade submissions for your own assignments.");

        if (request.MarksObtained > s.Assignment.MaxMarks)
            throw new BusinessRuleException($"Marks cannot exceed the maximum of {s.Assignment.MaxMarks}.");

        if (request.MarksObtained < 0)
            throw new BusinessRuleException("Marks cannot be negative.");

        s.MarksObtained = request.MarksObtained;
        s.Feedback = request.Feedback;
        s.Status = SubmissionStatus.Graded;
        s.GradedByTeacherId = CurrentUserId;
        s.GradedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Teacher {TeacherId} graded submission {SubmissionId}: {Marks}/{Max}",
            CurrentUserId, id, request.MarksObtained, s.Assignment.MaxMarks);

        return Ok(ToResponse(s));
    }

    /// <summary>Teacher changes submission status (e.g. ask for revision) when necessary.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<SubmissionResponse>> SetStatus(int id, UpdateSubmissionStatusRequest request)
    {
        var s = await _db.Submissions.Include(x => x.Assignment).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Submission not found.");

        if (s.Assignment.CreatedByTeacherId != CurrentUserId)
            throw new ForbiddenException("You can only manage submissions for your own assignments.");

        if (!Enum.TryParse<SubmissionStatus>(request.Status, true, out var newStatus))
            throw new BusinessRuleException("Invalid status value.");

        s.Status = newStatus;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(s));
    }
}

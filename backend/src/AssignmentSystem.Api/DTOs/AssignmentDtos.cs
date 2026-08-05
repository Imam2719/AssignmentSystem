using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.DTOs;

public record CreateAssignmentRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Description,
    [Required] DateTime Deadline,
    [Required, Range(1, 1000)] int MaxMarks,
    [Required] int SchoolClassId,
    [Required] int SubjectId,
    bool AllowResubmission = true,
    bool PublishNow = false
);

public record UpdateAssignmentRequest(
    [MaxLength(200)] string? Title,
    string? Description,
    DateTime? Deadline,
    [Range(1, 1000)] int? MaxMarks,
    bool? AllowResubmission
);

public record AssignmentResponse(
    int Id,
    string Title,
    string Description,
    DateTime Deadline,
    int MaxMarks,
    string Status,
    bool AllowResubmission,
    int SchoolClassId,
    string SchoolClassName,
    int SubjectId,
    string SubjectName,
    int CreatedByTeacherId,
    string CreatedByTeacherName,
    DateTime CreatedAt,
    bool IsPastDeadline,
    int SubmissionCount
);

using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.DTOs;

public record CreateSubmissionRequest(
    [Required] string AnswerText,
    string? AttachmentUrl
);

public record UpdateSubmissionRequest(
    [Required] string AnswerText,
    string? AttachmentUrl
);

public record GradeSubmissionRequest(
    [Required, Range(0, 1000)] int MarksObtained,
    string? Feedback
);

public record UpdateSubmissionStatusRequest(
    [Required] string Status // e.g. "ReturnedForRevision"
);

public record SubmissionResponse(
    int Id,
    int AssignmentId,
    string AssignmentTitle,
    int MaxMarks,
    int StudentId,
    string StudentName,
    string AnswerText,
    string? AttachmentUrl,
    string Status,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    int? MarksObtained,
    string? Feedback,
    DateTime? GradedAt
);

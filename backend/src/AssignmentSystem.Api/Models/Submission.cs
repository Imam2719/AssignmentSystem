using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.Models;

public class Submission
{
    public int Id { get; set; }

    [Required]
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    [Required]
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    // The student's answer text (a real system might also store file attachments;
    // kept as text + optional URL to keep scope reasonable — see README assumptions)
    [Required]
    public string AnswerText { get; set; } = string.Empty;

    public string? AttachmentUrl { get; set; }

    [Required]
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Grading fields, set by teacher
    public int? MarksObtained { get; set; }
    public string? Feedback { get; set; }
    public int? GradedByTeacherId { get; set; }
    public User? GradedByTeacher { get; set; }
    public DateTime? GradedAt { get; set; }
}

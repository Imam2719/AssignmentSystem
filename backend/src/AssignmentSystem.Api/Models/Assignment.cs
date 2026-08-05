using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.Models;

public class Assignment
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Deadline { get; set; }

    [Required]
    [Range(1, 1000)]
    public int MaxMarks { get; set; }

    [Required]
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

    // Whether students may update/resubmit before the deadline
    public bool AllowResubmission { get; set; } = true;

    [Required]
    public int SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;

    [Required]
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    [Required]
    public int CreatedByTeacherId { get; set; }
    public User CreatedByTeacher { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}

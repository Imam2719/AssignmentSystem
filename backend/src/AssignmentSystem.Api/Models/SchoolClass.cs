using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.Models;

// Represents a Class (e.g., "Class 9", "Grade 10 - Section A") or a Course in a college context
public class SchoolClass
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<User> Students { get; set; } = new List<User>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

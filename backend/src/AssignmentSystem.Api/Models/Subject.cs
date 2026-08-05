using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.Models;

public class Subject
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    // A subject belongs to one class/course in this simplified model.
    // (A "Math" subject for Class 9 is a distinct row from "Math" for Class 10.)
    [Required]
    public int SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;

    public ICollection<TeacherSubjectAssignment> TeacherAssignments { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

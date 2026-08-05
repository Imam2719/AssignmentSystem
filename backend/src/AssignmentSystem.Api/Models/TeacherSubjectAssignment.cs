namespace AssignmentSystem.Api.Models;

// Join entity: which teacher is assigned to which subject (subject already implies a class/course)
public class TeacherSubjectAssignment
{
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

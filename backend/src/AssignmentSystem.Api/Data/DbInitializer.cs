using AssignmentSystem.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSystem.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync(); // for first-run convenience; prefer migrations in real deployments

        if (db.Users.Any()) return; // already seeded

        var hasher = new PasswordHasher<User>();

        // ---- Classes ----
        var class9 = new SchoolClass { Name = "Class 9", Description = "Grade 9" };
        var class10 = new SchoolClass { Name = "Class 10", Description = "Grade 10" };
        db.SchoolClasses.AddRange(class9, class10);
        await db.SaveChangesAsync();

        // ---- Subjects ----
        var math9 = new Subject { Name = "Mathematics", Code = "MTH9", SchoolClassId = class9.Id };
        var eng9 = new Subject { Name = "English", Code = "ENG9", SchoolClassId = class9.Id };
        var phy10 = new Subject { Name = "Physics", Code = "PHY10", SchoolClassId = class10.Id };
        db.Subjects.AddRange(math9, eng9, phy10);
        await db.SaveChangesAsync();

        // ---- Users ----
        var admin = new User { FullName = "System Admin", Email = "admin@school.test", Role = UserRole.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        var teacher = new User { FullName = "Rahim Uddin", Email = "teacher@school.test", Role = UserRole.Teacher };
        teacher.PasswordHash = hasher.HashPassword(teacher, "Teacher@123");

        var student = new User
        {
            FullName = "Karim Hossain",
            Email = "student@school.test",
            Role = UserRole.Student,
            SchoolClassId = class9.Id
        };
        student.PasswordHash = hasher.HashPassword(student, "Student@123");

        db.Users.AddRange(admin, teacher, student);
        await db.SaveChangesAsync();

        // ---- Teacher assigned to subjects ----
        db.TeacherSubjectAssignments.AddRange(
            new TeacherSubjectAssignment { TeacherId = teacher.Id, SubjectId = math9.Id },
            new TeacherSubjectAssignment { TeacherId = teacher.Id, SubjectId = eng9.Id }
        );
        await db.SaveChangesAsync();

        // ---- Sample assignment ----
        var assignment = new Assignment
        {
            Title = "Algebra Basics - Worksheet 1",
            Description = "Solve the 10 algebra problems attached and show your work.",
            Deadline = DateTime.UtcNow.AddDays(7),
            MaxMarks = 100,
            Status = AssignmentStatus.Published,
            SchoolClassId = class9.Id,
            SubjectId = math9.Id,
            CreatedByTeacherId = teacher.Id
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();
    }
}

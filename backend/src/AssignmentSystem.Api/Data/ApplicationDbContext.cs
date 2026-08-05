using AssignmentSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubjectAssignment> TeacherSubjectAssignments => Set<TeacherSubjectAssignment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- User ----
        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();

            e.HasOne(u => u.SchoolClass)
                .WithMany(c => c.Students)
                .HasForeignKey(u => u.SchoolClassId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ---- Subject belongs to a SchoolClass ----
        builder.Entity<Subject>(e =>
        {
            e.HasOne(s => s.SchoolClass)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // A subject name should be unique within the same class
            e.HasIndex(s => new { s.SchoolClassId, s.Name }).IsUnique();
        });

        // ---- TeacherSubjectAssignment (many-to-many Teacher <-> Subject) ----
        builder.Entity<TeacherSubjectAssignment>(e =>
        {
            e.HasOne(t => t.Teacher)
                .WithMany(u => u.TeacherAssignments)
                .HasForeignKey(t => t.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Subject)
                .WithMany(s => s.TeacherAssignments)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // A teacher can only be assigned once to the same subject
            e.HasIndex(t => new { t.TeacherId, t.SubjectId }).IsUnique();
        });

        // ---- Assignment ----
        builder.Entity<Assignment>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>();

            e.HasOne(a => a.SchoolClass)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Subject)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.CreatedByTeacher)
                .WithMany(u => u.CreatedAssignments)
                .HasForeignKey(a => a.CreatedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Submission ----
        builder.Entity<Submission>(e =>
        {
            e.Property(s => s.Status).HasConversion<string>();

            e.HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.GradedByTeacher)
                .WithMany()
                .HasForeignKey(s => s.GradedByTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // A student can only have ONE submission per assignment
            // (updates go through PUT, not a new row) — business rule
            e.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
        });
    }
}

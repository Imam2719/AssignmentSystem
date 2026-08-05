using AssignmentSystem.Api.Controllers;
using AssignmentSystem.Api.DTOs;
using AssignmentSystem.Api.Middleware;
using AssignmentSystem.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AssignmentSystem.Tests;

public class SubmissionWorkflowTests
{
    private static async Task<(AssignmentSystem.Api.Data.ApplicationDbContext db, SchoolClass cls, Subject subject,
        User teacher, User student, Assignment assignment)> SeedBasicScenarioAsync(string dbName, DateTime? deadline = null)
    {
        var db = TestHelpers.CreateInMemoryDb(dbName);

        var cls = new SchoolClass { Name = "Class 9" };
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();

        var subject = new Subject { Name = "Math", SchoolClassId = cls.Id };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        var teacher = new User { FullName = "Teacher A", Email = "t@test.com", Role = UserRole.Teacher, PasswordHash = "x" };
        var student = new User { FullName = "Student A", Email = "s@test.com", Role = UserRole.Student, PasswordHash = "x", SchoolClassId = cls.Id };
        db.Users.AddRange(teacher, student);
        await db.SaveChangesAsync();

        db.TeacherSubjectAssignments.Add(new TeacherSubjectAssignment { TeacherId = teacher.Id, SubjectId = subject.Id });
        await db.SaveChangesAsync();

        var assignment = new Assignment
        {
            Title = "Homework 1",
            Description = "Solve problems",
            Deadline = deadline ?? DateTime.UtcNow.AddDays(1),
            MaxMarks = 100,
            SchoolClassId = cls.Id,
            SubjectId = subject.Id,
            CreatedByTeacherId = teacher.Id,
            Status = AssignmentStatus.Published,
            AllowResubmission = true
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        return (db, cls, subject, teacher, student, assignment);
    }

    [Fact]
    public async Task Student_Cannot_Submit_After_Deadline()
    {
        var (db, _, _, _, student, assignment) = await SeedBasicScenarioAsync(
            nameof(Student_Cannot_Submit_After_Deadline), DateTime.UtcNow.AddMinutes(-5));

        var controller = new SubmissionsController(db, NullLogger<SubmissionsController>.Instance);
        TestHelpers.SetUser(controller, student.Id, UserRole.Student);

        var act = async () => await controller.Submit(assignment.Id, new CreateSubmissionRequest("My answer", null));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*deadline*");
    }

    [Fact]
    public async Task Student_Cannot_Submit_Twice_To_Same_Assignment()
    {
        var (db, _, _, _, student, assignment) = await SeedBasicScenarioAsync(nameof(Student_Cannot_Submit_Twice_To_Same_Assignment));

        var controller = new SubmissionsController(db, NullLogger<SubmissionsController>.Instance);
        TestHelpers.SetUser(controller, student.Id, UserRole.Student);

        await controller.Submit(assignment.Id, new CreateSubmissionRequest("First answer", null));

        var act = async () => await controller.Submit(assignment.Id, new CreateSubmissionRequest("Second answer", null));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already submitted*");
    }

    [Fact]
    public async Task Teacher_Cannot_Grade_Marks_Above_MaxMarks()
    {
        var (db, _, _, teacher, student, assignment) = await SeedBasicScenarioAsync(nameof(Teacher_Cannot_Grade_Marks_Above_MaxMarks));

        var studentController = new SubmissionsController(db, NullLogger<SubmissionsController>.Instance);
        TestHelpers.SetUser(studentController, student.Id, UserRole.Student);
        var created = await studentController.Submit(assignment.Id, new CreateSubmissionRequest("My answer", null));
        var submissionId = ((SubmissionResponse)((Microsoft.AspNetCore.Mvc.CreatedAtActionResult)created.Result!).Value!).Id;

        var teacherController = new SubmissionsController(db, NullLogger<SubmissionsController>.Instance);
        TestHelpers.SetUser(teacherController, teacher.Id, UserRole.Teacher);

        var act = async () => await teacherController.Grade(submissionId, new GradeSubmissionRequest(150, "Great job"));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*cannot exceed*");
    }

    [Fact]
    public async Task Teacher_Cannot_Grade_Another_Teachers_Submission()
    {
        var (db, cls, subject, _, student, assignment) = await SeedBasicScenarioAsync(nameof(Teacher_Cannot_Grade_Another_Teachers_Submission));

        var otherTeacher = new User { FullName = "Teacher B", Email = "b@test.com", Role = UserRole.Teacher, PasswordHash = "x" };
        db.Users.Add(otherTeacher);
        await db.SaveChangesAsync();

        var studentController = new SubmissionsController(db, NullLogger<SubmissionsController>.Instance);
        TestHelpers.SetUser(studentController, student.Id, UserRole.Student);
        var created = await studentController.Submit(assignment.Id, new CreateSubmissionRequest("My answer", null));
        var submissionId = ((SubmissionResponse)((Microsoft.AspNetCore.Mvc.CreatedAtActionResult)created.Result!).Value!).Id;

        var otherTeacherController = new SubmissionsController(db, NullLogger<SubmissionsController>.Instance);
        TestHelpers.SetUser(otherTeacherController, otherTeacher.Id, UserRole.Teacher);

        var act = async () => await otherTeacherController.Grade(submissionId, new GradeSubmissionRequest(80, "Nice"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Student_From_Different_Class_Cannot_See_Assignment()
    {
        var (db, _, _, _, _, assignment) = await SeedBasicScenarioAsync(nameof(Student_From_Different_Class_Cannot_See_Assignment));

        var otherClass = new SchoolClass { Name = "Class 10" };
        db.SchoolClasses.Add(otherClass);
        await db.SaveChangesAsync();

        var otherStudent = new User
        {
            FullName = "Student B", Email = "b2@test.com", Role = UserRole.Student,
            PasswordHash = "x", SchoolClassId = otherClass.Id
        };
        db.Users.Add(otherStudent);
        await db.SaveChangesAsync();

        var controller = new AssignmentsController(db, NullLogger<AssignmentsController>.Instance);
        TestHelpers.SetUser(controller, otherStudent.Id, UserRole.Student);

        var act = async () => await controller.GetById(assignment.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}

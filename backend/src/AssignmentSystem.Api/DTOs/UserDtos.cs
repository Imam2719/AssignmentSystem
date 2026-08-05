using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.DTOs;

public record UserResponse(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    int? SchoolClassId,
    string? SchoolClassName
);

public record UpdateUserRequest(
    [MaxLength(150)] string? FullName,
    bool? IsActive,
    int? SchoolClassId
);

public record SchoolClassRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(300)] string? Description
);

public record SchoolClassResponse(int Id, string Name, string? Description, int StudentCount, int SubjectCount);

public record SubjectRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(20)] string? Code,
    [Required] int SchoolClassId
);

public record SubjectResponse(int Id, string Name, string? Code, int SchoolClassId, string SchoolClassName);

public record AssignTeacherRequest(
    [Required] int TeacherId,
    [Required] int SubjectId
);

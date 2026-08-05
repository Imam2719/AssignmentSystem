using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Api.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    int UserId,
    string FullName,
    string Email,
    string Role
);

public record RegisterUserRequest(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Role, // "Admin" | "Teacher" | "Student"
    int? SchoolClassId
);

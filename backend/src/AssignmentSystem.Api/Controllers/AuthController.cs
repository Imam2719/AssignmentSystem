using AssignmentSystem.Api.Data;
using AssignmentSystem.Api.DTOs;
using AssignmentSystem.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<Models.User> _passwordHasher = new();
    private readonly ILogger<AuthController> _logger;

    public AuthController(ApplicationDbContext db, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Authenticate a user and receive a JWT access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.SchoolClass)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var (token, expiresAt) = _tokenService.CreateToken(user);

        _logger.LogInformation("User {Email} ({Role}) logged in", user.Email, user.Role);

        return Ok(new LoginResponse(token, expiresAt, user.Id, user.FullName, user.Email, user.Role.ToString()));
    }
}

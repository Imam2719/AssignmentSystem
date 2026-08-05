using System.Security.Claims;
using AssignmentSystem.Api.Data;
using AssignmentSystem.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Tests;

public static class TestHelpers
{
    public static ApplicationDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>Attaches a fake authenticated ClaimsPrincipal to a controller, simulating a logged-in user.</summary>
    public static void SetUser(ControllerBase controller, int userId, UserRole role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    public static string HashPassword(User user, string password) => new PasswordHasher<User>().HashPassword(user, password);
}

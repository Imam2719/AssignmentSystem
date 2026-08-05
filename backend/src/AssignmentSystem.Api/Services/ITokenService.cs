using AssignmentSystem.Api.Models;

namespace AssignmentSystem.Api.Services;

public interface ITokenService
{
    (string token, DateTime expiresAt) CreateToken(User user);
}

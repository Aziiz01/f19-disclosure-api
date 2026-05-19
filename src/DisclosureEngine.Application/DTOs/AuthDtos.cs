namespace DisclosureEngine.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password, Guid TenantId, string Role);

public sealed record RegisterResponse(Guid UserId);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Role, Guid TenantId);

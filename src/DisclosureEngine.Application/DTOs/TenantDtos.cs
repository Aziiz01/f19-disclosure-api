namespace DisclosureEngine.Application.DTOs;

public sealed record CreateTenantRequest(string Name);

public sealed record TenantResponse(Guid Id, string Name, DateTime CreatedAt);

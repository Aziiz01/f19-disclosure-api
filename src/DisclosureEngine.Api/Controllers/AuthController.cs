using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Application.DTOs;
using DisclosureEngine.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DisclosureEngine.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager     = userManager;
        _signInManager   = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>Registers a new user under the given tenant. 201 with user id; 400 if password or validation fails.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || request.TenantId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new { error = "Email, Password, TenantId, and Role are required." });
        }

        var user = new ApplicationUser
        {
            Email          = request.Email,
            UserName       = request.Email,
            EmailConfirmed = true,
            TenantId       = request.TenantId,
            Role           = request.Role
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error  = "Registration failed.",
                errors = result.Errors.Select(e => new { e.Code, e.Description })
            });
        }

        return CreatedAtAction(nameof(Register), new { id = user.Id }, new RegisterResponse(user.Id));
    }

    /// <summary>Verifies credentials, returns a signed JWT. 401 on any failure — no distinction between bad user vs bad password.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return Unauthorized();

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded) return Unauthorized();

        var token = _jwtTokenService.GenerateToken(user.Id, user.Email!, user.TenantId, user.Role);
        return Ok(new LoginResponse(token.Token, token.ExpiresAt, user.Role, user.TenantId));
    }
}

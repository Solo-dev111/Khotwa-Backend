using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Khotwa.Services;

public class AuthService
{
    private readonly KhotwaDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        KhotwaDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    // REGISTER STUDENT OR INSTRUCTOR
    public async Task<AuthResponseDto?> RegisterAsync(
        RegisterDto dto, UserRole role)
    {
        string email = dto.Email.Trim().ToLowerInvariant();

        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
            return null;

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = role
        };

        // Hash password before saving
        user.PasswordHash = _passwordHasher.HashPassword(
            user, dto.Password);

        // Create profile for students
        if (role == UserRole.Student)
        {
            user.StudentProfile = new StudentProfile();
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateToken(user);
    }

    // LOGIN
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        string email = dto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return null;

        // Check password
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password);

        if (result == PasswordVerificationResult.Failed)
            return null;

        // Update old hashes if necessary
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user, dto.Password);

            await _context.SaveChangesAsync();
        }

        return GenerateToken(user);
    }

    // GENERATE JWT TOKEN
    private AuthResponseDto GenerateToken(User user)
    {
        string jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is missing");

        string issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing");

        string audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.FullName),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        DateTime expiresAt = DateTime.UtcNow.AddHours(2);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials
        );

        string tokenString =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = tokenString,
            ExpiresAt = expiresAt
        };
    }
}
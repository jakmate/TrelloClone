using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrelloClone.Shared.DTOs;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IUnitOfWork uow, IConfiguration config)
    {
        _users = users;
        _uow = uow;
        _config = config;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);
        if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        // Check if user already exists
        if (await _users.GetByEmailAsync(req.Email) != null)
        {
            throw new InvalidOperationException("User already exists");
        }

        var user = new User
        {
            UserName = req.UserName,
            Email = req.Email,
            PasswordHash = HashPassword(req.Password)
        };

        _users.Add(user);
        await _uow.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        return new CurrentUserResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
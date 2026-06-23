using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PRN232_Ebay_Buyer.API.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string username, string email, string role);
    int? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _key;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _key = Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"]!);
    }

    public string GenerateToken(int userId, string username, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name,            username),
            new Claim(ClaimTypes.Email,            email),
            new Claim(ClaimTypes.Role,            role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_key),
            SecurityAlgorithms.HmacSha256);

        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60);

        var token = new JwtSecurityToken(
            issuer:    _configuration["Jwt:Issuer"],
            audience:  _configuration["Jwt:Audience"],
            claims:    claims,
            expires:   DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims
                .First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            return userId;
        }
        catch
        {
            return null;
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BancaRevistas.API.Services;

/// <summary>
/// Emite e valida os tokens JWT da API. O token carrega o id, o nome, o
/// e-mail e o perfil de permissão do funcionário, permitindo que os
/// controllers apliquem as regras de autorização sem consultar o banco.
/// </summary>
public class TokenService
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _chave;
    private readonly int _expiracaoHoras;

    public TokenService(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var secret = jwt["Key"] ?? throw new InvalidOperationException("A chave JWT não foi configurada (Jwt:Key).");
        var issuer = jwt["Issuer"] ?? "BancaRevistasAPI";
        var audience = jwt["Audience"] ?? "BancaRevistasWeb";

        _chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _expiracaoHoras = int.TryParse(jwt["ExpirationHours"], out var h) ? h : 8;
        Issuer = issuer;
        Audience = audience;
    }

    public string Issuer { get; }
    public string Audience { get; }

    /// <summary>Gera o token JWT assinado do usuário autenticado.</summary>
    public string Gerar(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Permissao.ToString()),
            new("permissao", usuario.Permissao.ToString())
        };

        var credenciais = new SigningCredentials(_chave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expiracaoHoras),
            signingCredentials: credenciais);

        return _handler.WriteToken(token);
    }
}

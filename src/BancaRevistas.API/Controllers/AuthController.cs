using BancaRevistas.API.Services;
using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Enums;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Autenticação dos funcionários da banca: login, perfil e troca de senha.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsuarioService _usuarios;
    private readonly TokenService _tokens;

    public AuthController(UsuarioService usuarios, TokenService tokens)
    {
        _usuarios = usuarios;
        _tokens = tokens;
    }

    /// <summary>Autentica um funcionário e devolve o token JWT de acesso.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new { mensagem = "E-mail e senha são obrigatórios." });

        var usuario = await _usuarios.ObterPorEmailAsync(request.Email);
        if (usuario == null || !usuario.Ativo || !usuario.Autenticar(request.Senha))
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        var token = _tokens.Gerar(usuario);
        return Ok(new LoginResponse(token, new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Permissao, usuario.Ativo)));
    }

    /// <summary>Devolve os dados do usuário autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> MeuPerfil()
    {
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var usuario = await _usuarios.ObterPorIdAsync(id);
        if (usuario == null) return NotFound(new { mensagem = "Usuário não encontrado." });

        return Ok(new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Permissao, usuario.Ativo));
    }

    /// <summary>Permite ao próprio funcionário trocar a senha.</summary>
    [HttpPost("alterar-senha")]
    [Authorize]
    public async Task<ActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
    {
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var ok = await _usuarios.AlterarSenhaAsync(id, request.SenhaAtual, request.NovaSenha);
        if (!ok) return BadRequest(new { mensagem = "Senha atual incorreta." });
        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }
}

/// <summary>
/// CRUD de usuários (funcionários). Restrito ao perfil administrador.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _usuarios;

    public UsuariosController(UsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar([FromQuery] bool apenasAtivos = false) =>
        Ok(await _usuarios.ListarAsync(apenasAtivos));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Obter(int id)
    {
        var usuario = await _usuarios.ObterPorIdAsync(id);
        if (usuario == null) return NotFound(new { mensagem = "Usuário não encontrado." });
        return Ok(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar([FromBody] CriarUsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { mensagem = "Nome e e-mail são obrigatórios." });
        if (request.Senha.Length < 6)
            return BadRequest(new { mensagem = "A senha deve ter ao menos 6 caracteres." });

        var usuario = await _usuarios.CriarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = usuario.Id },
            new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Permissao, usuario.Ativo));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Atualizar(int id, [FromBody] CriarUsuarioRequest request)
    {
        var ok = await _usuarios.AtualizarAsync(id, request.Nome, request.Permissao, true);
        if (!ok) return NotFound(new { mensagem = "Usuário não encontrado." });
        return Ok(new { mensagem = "Usuário atualizado." });
    }
}

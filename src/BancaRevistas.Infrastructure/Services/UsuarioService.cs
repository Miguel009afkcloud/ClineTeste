using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Gerencia os funcionários da banca: cadastro, edição, ativação e consulta.
/// A senha nunca é devolvida ao cliente; apenas o hash é persistido.
/// </summary>
public class UsuarioService
{
    private readonly Data.BancaContext _context;

    public UsuarioService(Data.BancaContext context) => _context = context;

    /// <summary>Lista todos os usuários, do mais recente ao mais antigo.</summary>
    public async Task<List<UsuarioDto>> ListarAsync(bool apenasAtivos = false)
    {
        var query = _context.Usuarios.AsNoTracking();
        if (apenasAtivos)
            query = query.Where(u => u.Ativo);
        return await query.OrderBy(u => u.Nome).Select(u => new UsuarioDto(u.Id, u.Nome, u.Email, u.Permissao, u.Ativo)).ToListAsync();
    }

    public async Task<Usuario?> ObterPorEmailAsync(string email) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<Usuario?> ObterPorIdAsync(int id) =>
        await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    /// <summary>
    /// Cria um novo usuário. Lança <see cref="InvalidOperationException"/> quando
    /// o e-mail já está em uso, evitando duplicidade de contas.
    /// </summary>
    public async Task<Usuario> CriarAsync(CriarUsuarioRequest request)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            throw new InvalidOperationException("Já existe um usuário cadastrado com este e-mail.");

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Email = request.Email,
            Permissao = request.Permissao,
            Ativo = true
        };
        usuario.SetSenha(request.Senha);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    /// <summary>Atualiza os dados de cadastro (não a senha) de um usuário.</summary>
    public async Task<bool> AtualizarAsync(int id, string nome, PermissaoUsuario permissao, bool ativo)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return false;

        usuario.Nome = nome;
        usuario.Permissao = permissao;
        usuario.Ativo = ativo;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Troca a senha do usuário exigindo a senha atual. Usada pelo próprio
    /// funcionário no perfil.
    /// </summary>
    public async Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null || !usuario.Autenticar(senhaAtual)) return false;

        usuario.SetSenha(novaSenha);
        await _context.SaveChangesAsync();
        return true;
    }
}

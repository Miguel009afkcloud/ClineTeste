using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using BancaRevistas.Infrastructure.Services;

namespace BancaRevistas.Tests;

/// <summary>
/// Testes de autenticação e gestão de usuários da banca: hash de senha,
/// validação de credenciais, unicidade de e-mail e troca de senha.
/// </summary>
public class UsuarioServiceTests
{
    [Fact]
    public async Task DeveAutenticarComSenhaCorreta()
    {
        using var banco = new BancoTeste();
        var servico = new UsuarioService(banco.Context);
        await servico.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Maria",
            Email = "maria@banca.com.br",
            Senha = "senha123",
            Permissao = PermissaoUsuario.Atendente
        });

        var usuario = await servico.ObterPorEmailAsync("maria@banca.com.br");

        Assert.NotNull(usuario);
        Assert.True(usuario!.Autenticar("senha123"));
        Assert.False(usuario.Autenticar("senhaErrada"));
    }

    [Fact]
    public async Task NaoDeveArmazenarSenhaEmTextoPuro()
    {
        using var banco = new BancoTeste();
        var servico = new UsuarioService(banco.Context);

        await servico.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Joao",
            Email = "joao@banca.com.br",
            Senha = "senha123",
            Permissao = PermissaoUsuario.Operador
        });

        var usuario = await servico.ObterPorEmailAsync("joao@banca.com.br");

        Assert.NotEqual("senha123", usuario!.SenhaHash);
        Assert.StartsWith("$2", usuario.SenhaHash);
    }

    [Fact]
    public async Task DeveRejeitarEmailDuplicado()
    {
        using var banco = new BancoTeste();
        var servico = new UsuarioService(banco.Context);

        await servico.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Primeiro",
            Email = "dup@banca.com.br",
            Senha = "senha123",
            Permissao = PermissaoUsuario.Operador
        });

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Segundo",
            Email = "dup@banca.com.br",
            Senha = "senha123",
            Permissao = PermissaoUsuario.Operador
        }));
    }

    [Fact]
    public async Task DeveAlterarSenha_ExigindoSenhaAtual()
    {
        using var banco = new BancoTeste();
        var servico = new UsuarioService(banco.Context);

        await servico.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Carlos",
            Email = "carlos@banca.com.br",
            Senha = "senha123",
            Permissao = PermissaoUsuario.Operador
        });

        var usuario = await servico.ObterPorEmailAsync("carlos@banca.com.br");

        var falhou = await servico.AlterarSenhaAsync(usuario!.Id, "errada", "novasenha");
        Assert.False(falhou);

        var sucesso = await servico.AlterarSenhaAsync(usuario.Id, "senha123", "novasenha");
        Assert.True(sucesso);

        usuario = await servico.ObterPorEmailAsync("carlos@banca.com.br");
        Assert.True(usuario!.Autenticar("novasenha"));
    }

    [Fact]
    public async Task DeveDesativarUsuario()
    {
        using var banco = new BancoTeste();
        var servico = new UsuarioService(banco.Context);

        await servico.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "Ana",
            Email = "ana@banca.com.br",
            Senha = "senha123",
            Permissao = PermissaoUsuario.Estoquista
        });

        var usuario = await servico.ObterPorEmailAsync("ana@banca.com.br");

        var ok = await servico.AtualizarAsync(usuario!.Id, "Ana", PermissaoUsuario.Operador, ativo: false);
        Assert.True(ok);

        usuario = await servico.ObterPorIdAsync(usuario.Id);
        Assert.False(usuario!.Ativo);
        Assert.Equal(PermissaoUsuario.Operador, usuario.Permissao);
    }
}

using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using BancaRevistas.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Tests;

/// <summary>
/// Cria um contexto EF Core sobre um banco SQLite em memória, já com o
/// esquema criado, para que cada teste rode de forma isolada.
/// </summary>
public sealed class BancoTeste : IDisposable
{
    private readonly SqliteConnection _conexao;

    public BancoTeste()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        var opcoes = new DbContextOptionsBuilder<BancaContext>()
            .UseSqlite(_conexao)
            .Options;

        Context = new BancaContext(opcoes);
        Context.Database.EnsureCreated();
    }

    public BancaContext Context { get; }

    /// <summary>Cria um usuário e devolve seu id.</summary>
    public int CriarUsuario(string nome = "Operador", PermissaoUsuario permissao = PermissaoUsuario.Operador)
    {
        var usuario = new Usuario { Nome = nome, Email = $"{nome.ToLower()}@banca.com.br", Permissao = permissao };
        usuario.SetSenha("senha123");
        Context.Usuarios.Add(usuario);
        Context.SaveChanges();
        return usuario.Id;
    }

    /// <summary>Cria uma publicação e devolve seu id.</summary>
    public int CriarPublicacao(string titulo = "Revista Teste", decimal preco = 10m, int estoque = 20, int estoqueMinimo = 5)
    {
        var publicacao = new Publicacao
        {
            Titulo = titulo,
            Preco = preco,
            PrecoCusto = 5m,
            Estoque = estoque,
            EstoqueMinimo = estoqueMinimo,
            Ativo = true
        };

        Context.Publicacoes.Add(publicacao);
        Context.SaveChanges();
        return publicacao.Id;
    }

    /// <summary>Cria um cliente e devolve seu id.</summary>
    public int CriarCliente(string nome = "Cliente Teste", decimal limiteCredito = 100m)
    {
        var cliente = new Cliente { Nome = nome, LimiteCredito = limiteCredito, Ativo = true };
        Context.Clientes.Add(cliente);
        Context.SaveChanges();
        return cliente.Id;
    }

    /// <summary>Abre um caixa para o usuário informado e devolve seu id.</summary>
    public int AbrirCaixa(int usuarioId, decimal valorInicial = 0m)
    {
        var caixa = new Caixa
        {
            UsuarioId = usuarioId,
            Abertura = DateTime.UtcNow,
            ValorInicial = valorInicial,
            Status = "Aberto"
        };

        Context.Caixas.Add(caixa);
        Context.SaveChanges();
        return caixa.Id;
    }

    public void Dispose() => Context.Dispose();
}

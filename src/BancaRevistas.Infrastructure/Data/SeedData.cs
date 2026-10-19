using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Data;

/// <summary>
/// Carga inicial do banco da banca. Executada apenas quando a base está vazia:
/// cria o administrador, o caixa padrão, o catálogo de categorias e um
/// conjunto de publicações reais de revista para o PDV já funcionar.
/// </summary>
public static class SeedData
{
    public static async Task AplicarAsync(BancaContext context, CancellationToken ct = default)
    {
        if (await context.Usuarios.AnyAsync(ct))
            return;

        // --- Usuário administrador padrão ---
        var admin = new Usuario
        {
            Nome = "Administrador",
            Email = "admin@banca.com.br",
            Permissao = PermissaoUsuario.Administrador,
            Ativo = true
        };
        admin.SetSenha("admin123");
        context.Usuarios.Add(admin);

        // --- Operador padrão ---
        var operador = new Usuario
        {
            Nome = "Operador",
            Email = "operador@banca.com.br",
            Permissao = PermissaoUsuario.Operador,
            Ativo = true
        };
        operador.SetSenha("operador123");
        context.Usuarios.Add(operador);
        await context.SaveChangesAsync(ct);

        // --- Caixa padrão (referência das vendas) ---
        context.Caixas.Add(new Caixa
        {
            UsuarioId = admin.Id,
            Abertura = DateTime.UtcNow,
            ValorInicial = 0,
            Status = nameof(StatusCaixa.Fechado),
            Fechamento = DateTime.UtcNow
        });

        // --- Categorias ---
        var categorias = new List<Categoria>
        {
            new() { Nome = "Revistas", Descricao = "Revistas semanais e mensais" },
            new() { Nome = "Jornais", Descricao = "Jornais locais e nacionais" },
            new() { Nome = "Quadrinhos", Descricao = "Gibis e mangás" },
            new() { Nome = "Livros", Descricao = "Livros de bolso e best-sellers" },
            new() { Nome = "Papelaria", Descricao = "Cadernos, canetas e material escolar" },
            new() { Nome = "Conveniences", Descricao = "Doces, bebidas e snacks" }
        };
        context.Categorias.AddRange(categorias);
        await context.SaveChangesAsync(ct);

        // --- Fornecedores ---
        var editoras = new List<Fornecedor>
        {
            new() { RazaoSocial = "Editora Globo S.A.", NomeFantasia = "Globo", Cnpj = "27.865.757/0001-02", Telefone = "(11) 2126-5000", Email = "vendas@globo.com.br" },
            new() { RazaoSocial = "Editora Editora Abril S.A.", NomeFantasia = "Abril", Cnpj = "61.803.802/0001-00", Telefone = "(11) 3037-5000", Email = "atendimento@abril.com.br" },
            new() { RazaoSocial = "Editora Estadão S.A.", NomeFantasia = "Estadão", Cnpj = "04.613.487/0001-08", Telefone = "(11) 3866-2000", Email = "vendas@estadao.com.br" }
        };
        context.Fornecedores.AddRange(editoras);
        await context.SaveChangesAsync(ct);

        // --- Publicações de exemplo ---
        var rev = categorias[0];
        var jornal = categorias[1];
        var gibi = categorias[2];
        var papelaria = categorias[4];
        var doce = categorias[5];

        context.Publicacoes.AddRange(
            new Publicacao { Titulo = "Revista Bahia", Autor = "Editora Globo", Tipo = TipoPublicacao.Revista, CategoriaId = rev.Id, FornecedorId = editoras[0].Id, Preco = 14.90m, PrecoCusto = 8.50m, Estoque = 40, EstoqueMinimo = 10, NumeroEdicao = "Edição 1247" },
            new Publicacao { Titulo = "Veja", Autor = "Editora Globo", Tipo = TipoPublicacao.Revista, CategoriaId = rev.Id, FornecedorId = editoras[0].Id, Preco = 18.50m, PrecoCusto = 11.00m, Estoque = 25, EstoqueMinimo = 10, NumeroEdicao = "Edição 2913" },
            new Publicacao { Titulo = "Veja Rio", Autor = "Editora Globo", Tipo = TipoPublicacao.Revista, CategoriaId = rev.Id, FornecedorId = editoras[0].Id, Preco = 12.00m, PrecoCusto = 7.20m, Estoque = 8, EstoqueMinimo = 10, NumeroEdicao = "Edição 512" },
            new Publicacao { Titulo = "Estadão", Autor = "Editora Estadão", Tipo = TipoPublicacao.Jornal, CategoriaId = jornal.Id, FornecedorId = editoras[2].Id, Preco = 6.00m, PrecoCusto = 3.80m, Estoque = 60, EstoqueMinimo = 20 },
            new Publicacao { Titulo = "O Globo", Autor = "Organizações Globo", Tipo = TipoPublicacao.Jornal, CategoriaId = jornal.Id, FornecedorId = editoras[0].Id, Preco = 5.50m, PrecoCusto = 3.50m, Estoque = 55, EstoqueMinimo = 20 },
            new Publicacao { Titulo = "Turma da Mônica", Autor = "MS Comic", Tipo = TipoPublicacao.Quadrinho, CategoriaId = gibi.Id, Preco = 9.90m, PrecoCusto = 5.50m, Estoque = 30, EstoqueMinimo = 8 },
            new Publicacao { Titulo = "Cissn", Autor = "Editora-versus", Tipo = TipoPublicacao.Quadrinho, CategoriaId = gibi.Id, Preco = 12.90m, PrecoCusto = 7.00m, Estoque = 18, EstoqueMinimo = 8 },
            new Publicacao { Titulo = "Caderno Gourmet", Autor = "Papelaria Central", Tipo = TipoPublicacao.Papelaria, CategoriaId = papelaria.Id, Preco = 15.00m, PrecoCusto = 8.00m, Estoque = 22, EstoqueMinimo = 6 },
            new Publicacao { Titulo = "Caneta Esferográfica", Autor = "Papelaria Central", Tipo = TipoPublicacao.Papelaria, CategoriaId = papelaria.Id, CodigoBarras = "7891000100103", Preco = 2.50m, PrecoCusto = 1.10m, Estoque = 120, EstoqueMinimo = 30 },
            new Publicacao { Titulo = "Chocolate ao Leite", Autor = "Conveniences", Tipo = TipoPublicacao.Doce, CategoriaId = doce.Id, CodigoBarras = "7891000000111", Preco = 4.50m, PrecoCusto = 2.60m, Estoque = 90, EstoqueMinimo = 25 },
            new Publicacao { Titulo = "Água Mineral 500ml", Autor = "Conveniences", Tipo = TipoPublicacao.Bebida, CategoriaId = doce.Id, CodigoBarras = "7891000000227", Preco = 3.00m, PrecoCusto = 1.50m, Estoque = 150, EstoqueMinimo = 40 }
        );

        // --- Clientes de exemplo ---
        context.Clientes.AddRange(
            new Cliente { Nome = "Maria Silva", CpfCnpj = "123.456.789-00", Telefone = "(71) 98888-1111", LimiteCredito = 200m },
            new Cliente { Nome = "João Pereira", CpfCnpj = "987.654.321-00", Telefone = "(71) 97777-2222", LimiteCredito = 100m }
        );

        await context.SaveChangesAsync(ct);
    }
}

using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Gerencia o catálogo de publicações e itens da banca: cadastro, edição,
/// busca por título/código de barras e listagem de itens ativos.
/// </summary>
public class PublicacaoService
{
    private readonly Data.BancaContext _context;

    public PublicacaoService(Data.BancaContext context) => _context = context;

    private IQueryable<Publicacao> Base() => _context.Publicacoes
        .AsNoTracking()
        .Include(p => p.Categoria)
        .Include(p => p.Fornecedor);

    private static PublicacaoDto Mapear(Publicacao p) => new(
        p.Id, p.Titulo, p.Subtitulo, p.Autor, p.CodigoBarras, p.Tipo,
        p.CategoriaId, p.Categoria?.Nome, p.FornecedorId, p.Fornecedor?.NomeFantasia ?? p.Fornecedor?.RazaoSocial,
        p.Preco, p.PrecoCusto, p.Estoque, p.EstoqueMinimo, p.NumeroEdicao, p.DataPublicacao, p.Ativo, p.EstoqueCritico);

    /// <summary>Lista o catálogo. O filtro de busca atua sobre título e código de barras.</summary>
    public async Task<List<PublicacaoDto>> ListarAsync(string? busca = null, bool apenasAtivos = true, int? categoriaId = null, int? fornecedorId = null)
    {
        var query = Base();
        if (apenasAtivos) query = query.Where(p => p.Ativo);
        if (categoriaId.HasValue) query = query.Where(p => p.CategoriaId == categoriaId);
        if (fornecedorId.HasValue) query = query.Where(p => p.FornecedorId == fornecedorId);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(p => p.Titulo.Contains(termo) ||
                                     (p.Autor != null && p.Autor.Contains(termo)) ||
                                     (p.CodigoBarras != null && p.CodigoBarras.Contains(termo)));
        }

        return await query.OrderBy(p => p.Titulo).Select(p => new PublicacaoDto(
            p.Id, p.Titulo, p.Subtitulo, p.Autor, p.CodigoBarras, p.Tipo,
            p.CategoriaId, p.Categoria!.Nome, p.FornecedorId, p.Fornecedor!.NomeFantasia,
            p.Preco, p.PrecoCusto, p.Estoque, p.EstoqueMinimo, p.NumeroEdicao, p.DataPublicacao, p.Ativo,
            p.EstoqueMinimo > 0 && p.Estoque <= p.EstoqueMinimo)).ToListAsync();
    }

    public async Task<PublicacaoDto?> ObterPorIdAsync(int id)
    {
        var p = await Base().FirstOrDefaultAsync(x => x.Id == id);
        return p == null ? null : Mapear(p);
    }

    /// <summary>
    /// Busca exata por código de barras (usada pelo leitor de código do PDV).
    /// Considera apenas publicações ativas.
    /// </summary>
    public async Task<PublicacaoDto?> ObterPorCodigoBarrasAsync(string codigoBarras)
    {
        var p = await Base().FirstOrDefaultAsync(x => x.CodigoBarras == codigoBarras && x.Ativo);
        return p == null ? null : Mapear(p);
    }

    /// <summary>
    /// Cadastra uma nova publicação. Se o estoque inicial for maior que zero,
    /// registra a movimentação de entrada para manter o histórico consistente.
    /// </summary>
    public async Task<PublicacaoDto> CriarAsync(PublicacaoInput input, int? usuarioId = null)
    {
        var publicacao = new Publicacao
        {
            Titulo = input.Titulo,
            Subtitulo = input.Subtitulo,
            Autor = input.Autor,
            CodigoBarras = input.CodigoBarras,
            Tipo = input.Tipo,
            CategoriaId = input.CategoriaId,
            FornecedorId = input.FornecedorId,
            Preco = input.Preco,
            PrecoCusto = input.PrecoCusto,
            Estoque = 0,
            EstoqueMinimo = input.EstoqueMinimo,
            NumeroEdicao = input.NumeroEdicao,
            DataPublicacao = input.DataPublicacao,
            Ativo = input.Ativo
        };

        _context.Publicacoes.Add(publicacao);
        await _context.SaveChangesAsync();

        if (input.Estoque > 0)
        {
            publicacao.Estoque = input.Estoque;
            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                PublicacaoId = publicacao.Id,
                Tipo = nameof(TipoMovimentacao.Entrada),
                Quantidade = input.Estoque,
                PrecoUnitario = input.PrecoCusto,
                UsuarioId = usuarioId,
                Motivo = "Estoque inicial"
            });
            await _context.SaveChangesAsync();
        }

        return (await ObterPorIdAsync(publicacao.Id))!;
    }

    /// <summary>Atualiza o cadastro da publicação sem alterar o estoque atual.</summary>
    public async Task<PublicacaoDto?> AtualizarAsync(int id, PublicacaoInput input)
    {
        var publicacao = await _context.Publicacoes.FindAsync(id);
        if (publicacao == null) return null;

        publicacao.Titulo = input.Titulo;
        publicacao.Subtitulo = input.Subtitulo;
        publicacao.Autor = input.Autor;
        publicacao.CodigoBarras = input.CodigoBarras;
        publicacao.Tipo = input.Tipo;
        publicacao.CategoriaId = input.CategoriaId;
        publicacao.FornecedorId = input.FornecedorId;
        publicacao.Preco = input.Preco;
        publicacao.PrecoCusto = input.PrecoCusto;
        publicacao.EstoqueMinimo = input.EstoqueMinimo;
        publicacao.NumeroEdicao = input.NumeroEdicao;
        publicacao.DataPublicacao = input.DataPublicacao;
        publicacao.Ativo = input.Ativo;

        await _context.SaveChangesAsync();
        return await ObterPorIdAsync(id);
    }

    /// <summary>Desativa (soft delete) a publicação preservando o histórico de vendas.</summary>
    public async Task<bool> DesativarAsync(int id)
    {
        var publicacao = await _context.Publicacoes.FindAsync(id);
        if (publicacao == null) return false;

        publicacao.Ativo = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>Publicações cujo estoque atingiu o nível mínimo de reposição.</summary>
    public async Task<List<EstoqueBaixoDto>> ListarEstoqueCriticoAsync()
    {
        return await _context.Publicacoes
            .AsNoTracking()
            .Where(p => p.Ativo && p.EstoqueMinimo > 0 && p.Estoque <= p.EstoqueMinimo)
            .OrderBy(p => p.Estoque)
            .Select(p => new EstoqueBaixoDto(p.Id, p.Titulo, p.Estoque, p.EstoqueMinimo, p.Tipo.ToString()))
            .ToListAsync();
    }
}

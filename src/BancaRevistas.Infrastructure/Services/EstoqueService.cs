using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Centraliza as operações de estoque da banca: ajustes manuais, entrada por
/// compra, saída por venda e histórico de movimentações.
/// </summary>
public class EstoqueService
{
    private readonly Data.BancaContext _context;

    public EstoqueService(Data.BancaContext context) => _context = context;

    /// <summary>
    /// Ajusta o saldo de estoque de uma publicação e registra a movimentação
    /// correspondente. <paramref name="somar"/> true soma, false subtrai.
    /// </summary>
    public async Task<bool> AjustarAsync(int publicacaoId, int quantidade, string motivo, bool somar, int? usuarioId)
    {
        if (quantidade <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(quantidade));

        var publicacao = await _context.Publicacoes.FindAsync(publicacaoId);
        if (publicacao == null) return false;

        if (!somar && publicacao.Estoque < quantidade)
            throw new InvalidOperationException($"Estoque insuficiente para '{publicacao.Titulo}'.");

        publicacao.Estoque = somar ? publicacao.Estoque + quantidade : publicacao.Estoque - quantidade;

        _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            PublicacaoId = publicacaoId,
            Tipo = somar ? nameof(TipoMovimentacao.Entrada) : nameof(TipoMovimentacao.Saida),
            Quantidade = quantidade,
            UsuarioId = usuarioId,
            Motivo = motivo
        });

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Registra uma perda ou devolução de exemplar (revista devolvida pelo
    /// cliente, exemplar danificado, etc.).
    /// </summary>
    public async Task<bool> RegistrarPerdaAsync(int publicacaoId, int quantidade, string motivo, int? usuarioId)
    {
        if (quantidade <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(quantidade));

        var publicacao = await _context.Publicacoes.FindAsync(publicacaoId);
        if (publicacao == null) return false;

        if (publicacao.Estoque < quantidade)
            throw new InvalidOperationException($"Estoque insuficiente para '{publicacao.Titulo}'.");

        publicacao.Estoque -= quantidade;
        _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            PublicacaoId = publicacaoId,
            Tipo = nameof(TipoMovimentacao.Perda),
            Quantidade = quantidade,
            UsuarioId = usuarioId,
            Motivo = motivo
        });

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>Histórico de movimentações, do mais recente para o mais antigo.</summary>
    public async Task<List<MovimentacaoEstoqueDto>> HistoricoAsync(DateTime? inicio = null, DateTime? fim = null, int? publicacaoId = null)
    {
        var query = _context.MovimentacoesEstoque
            .AsNoTracking()
            .Include(m => m.Publicacao)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (inicio.HasValue) query = query.Where(m => m.DataMovimentacao >= inicio.Value);
        if (fim.HasValue) query = query.Where(m => m.DataMovimentacao <= fim.Value);
        if (publicacaoId.HasValue) query = query.Where(m => m.PublicacaoId == publicacaoId.Value);

        return await query
            .OrderByDescending(m => m.DataMovimentacao)
            .Select(m => new MovimentacaoEstoqueDto(
                m.Id, m.PublicacaoId, m.Publicacao!.Titulo, m.Tipo, m.Quantidade, m.PrecoUnitario,
                m.DataMovimentacao, m.Usuario!.Nome, m.Motivo))
            .ToListAsync();
    }
}

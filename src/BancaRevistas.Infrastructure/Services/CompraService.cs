using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Registra as compras de reposição de estoque feitas às distribuidoras. Cada
/// compra dá entrada no estoque das publicações e gera a movimentação
/// correspondente, tudo em uma transação única.
/// </summary>
public class CompraService
{
    private readonly Data.BancaContext _context;

    public CompraService(Data.BancaContext context) => _context = context;

    public async Task<CompraDto> RegistrarAsync(CompraInput input, int usuarioId)
    {
        if (input.Itens == null || !input.Itens.Any())
            throw new InvalidOperationException("A compra deve conter pelo menos um item.");

        var fornecedor = await _context.Fornecedores.FindAsync(input.FornecedorId)
            ?? throw new InvalidOperationException("Fornecedor não encontrado.");

        var total = input.Itens.Sum(i => i.Quantidade * i.PrecoUnitario - i.Desconto);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var compra = new Compra
            {
                FornecedorId = input.FornecedorId,
                UsuarioId = usuarioId,
                DataCompra = DateTime.UtcNow,
                ValorTotal = total,
                Status = nameof(StatusCompra.Concluida),
                Observacoes = input.Observacoes
            };

            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();

            foreach (var item in input.Itens)
            {
                var publicacao = await _context.Publicacoes.FindAsync(item.PublicacaoId);
                if (publicacao == null)
                    throw new InvalidOperationException($"Publicação ID {item.PublicacaoId} não encontrada.");

                publicacao.Estoque += item.Quantidade;

                _context.ItensCompras.Add(new ItemCompra
                {
                    CompraId = compra.Id,
                    PublicacaoId = item.PublicacaoId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    Desconto = item.Desconto
                });

                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    PublicacaoId = item.PublicacaoId,
                    Tipo = nameof(TipoMovimentacao.Entrada),
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    UsuarioId = usuarioId,
                    Motivo = $"Compra #{compra.Id} - {fornecedor.NomeFantasia ?? fornecedor.RazaoSocial}",
                    ReferenciaCompraId = compra.Id
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await ObterPorIdAsync(compra.Id))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CompraDto>> ListarAsync(DateTime? inicio = null, DateTime? fim = null, int? fornecedorId = null)
    {
        var query = _context.Compras
            .AsNoTracking()
            .Include(c => c.Fornecedor)
            .AsQueryable();

        if (inicio.HasValue) query = query.Where(c => c.DataCompra >= inicio.Value);
        if (fim.HasValue) query = query.Where(c => c.DataCompra <= fim.Value);
        if (fornecedorId.HasValue) query = query.Where(c => c.FornecedorId == fornecedorId);

        return await query
            .OrderByDescending(c => c.DataCompra)
            .Select(c => new CompraDto(c.Id, c.FornecedorId, c.Fornecedor!.RazaoSocial, c.DataCompra,
                c.ValorTotal, c.Status, c.Observacoes, c.Itens.Sum(i => i.Quantidade)))
            .ToListAsync();
    }

    public async Task<CompraDto?> ObterPorIdAsync(int id)
    {
        var compra = await _context.Compras.AsNoTracking().Include(c => c.Fornecedor).FirstOrDefaultAsync(c => c.Id == id);
        if (compra == null) return null;
        var totalItens = await _context.ItensCompras.Where(i => i.CompraId == id).SumAsync(i => i.Quantidade);
        return new CompraDto(compra.Id, compra.FornecedorId, compra.Fornecedor?.RazaoSocial, compra.DataCompra,
            compra.ValorTotal, compra.Status, compra.Observacoes, totalItens);
    }
}

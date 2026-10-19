using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Serviço de vendas (PDV da banca). Aplica as regras de negócio herdadas do
/// PDVStore: exige caixa aberto, valida estoque, trata venda fiada com limite
/// de crédito e registra tudo em uma única transação com rollback.
/// </summary>
public class VendaService
{
    private readonly Data.BancaContext _context;
    private readonly CaixaService _caixaService;

    public VendaService(Data.BancaContext context, CaixaService caixaService)
    {
        _context = context;
        _caixaService = caixaService;
    }

    /// <summary>
    /// Registra uma venda completa dentro de UMA transação com rollback.
    /// Ordem das regras: (0) exige CAIXA ABERTO; (0.1) venda FIADA exige cliente
    /// ativo e respeita o limite de crédito; (1) calcula o total e valida o
    /// estoque; (2) grava cabeçalho + itens; (3) baixa o estoque registrando a
    /// movimentação de saída; (4) increase o saldo devedor em vendas fiadas;
    /// (5) processa o pagamento (gerando o PixTxId no PIX) e só então commita.
    /// </summary>
    public async Task<VendaDto> RegistrarAsync(VendaInput input, int usuarioId)
    {
        if (input.Itens == null || !input.Itens.Any())
            throw new InvalidOperationException("A venda deve conter pelo menos um item.");

        if (string.IsNullOrWhiteSpace(input.FormaPagamento))
            throw new InvalidOperationException("Selecione uma forma de pagamento.");

        var caixa = await _caixaService.ObterAbertoAsync()
                    ?? throw new InvalidOperationException("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");

        // 0.1 Venda fiada exige cliente e respeita limite de crédito
        bool fiado = input.FormaPagamento.Equals(nameof(FormaPagamento.Fiado), StringComparison.OrdinalIgnoreCase);
        if (fiado)
        {
            if (!input.ClienteId.HasValue)
                throw new InvalidOperationException("Venda fiada exige um cliente selecionado.");

            var cliente = await _context.Clientes.FindAsync(input.ClienteId.Value);
            if (cliente == null || !cliente.Ativo)
                throw new InvalidOperationException("Cliente informado não está ativo.");

            decimal total = input.Itens.Sum(i => i.Quantidade * i.PrecoUnitario - i.Desconto) - input.Desconto;
            if (cliente.LimiteCredito > 0 && cliente.SaldoDevedor + total > cliente.LimiteCredito)
                throw new InvalidOperationException($"Limite de crédito do cliente excedido. Saldo atual: {cliente.SaldoDevedor:C2}.");
        }

        var totalVenda = input.Itens.Sum(i => i.Quantidade * i.PrecoUnitario - i.Desconto) - input.Desconto;
        if (totalVenda < 0)
            throw new InvalidOperationException("O valor total da venda não pode ser negativo.");

        // 1. Validar estoque antes de qualquer gravação
        foreach (var item in input.Itens)
        {
            var publicacao = await _context.Publicacoes.FindAsync(item.PublicacaoId);
            if (publicacao == null)
                throw new InvalidOperationException($"Publicação ID {item.PublicacaoId} não encontrada.");
            if (publicacao.Estoque < item.Quantidade)
                throw new InvalidOperationException($"Estoque insuficiente para '{publicacao.Titulo}'. Disponível: {publicacao.Estoque}.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var venda = new Venda
            {
                UsuarioId = usuarioId,
                CaixaId = caixa.Id,
                ClienteId = input.ClienteId,
                DataVenda = DateTime.UtcNow,
                Desconto = input.Desconto,
                ValorTotal = totalVenda,
                FormaPagamento = input.FormaPagamento,
                Status = nameof(StatusVenda.Concluida),
                PixTxId = input.FormaPagamento.Equals(nameof(FormaPagamento.Pix), StringComparison.OrdinalIgnoreCase)
                    ? $"E{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}"
                    : null
            };

            _context.Vendas.Add(venda);
            await _context.SaveChangesAsync();

            // 2. Gravar itens e baixar estoque com movimentação de saída
            foreach (var item in input.Itens)
            {
                var publicacao = await _context.Publicacoes.FindAsync(item.PublicacaoId);
                if (publicacao == null) continue;

                publicacao.Estoque -= item.Quantidade;

                _context.ItensVendas.Add(new ItemVenda
                {
                    VendaId = venda.Id,
                    PublicacaoId = item.PublicacaoId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    Desconto = item.Desconto
                });

                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    PublicacaoId = item.PublicacaoId,
                    Tipo = nameof(TipoMovimentacao.Saida),
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    UsuarioId = usuarioId,
                    Motivo = $"Venda #{venda.Id}",
                    ReferenciaVendaId = venda.Id
                });
            }

            // 3. Venda fiada aumenta o saldo devedor do cliente
            if (fiado && input.ClienteId.HasValue)
            {
                var cliente = await _context.Clientes.FindAsync(input.ClienteId.Value);
                if (cliente != null) cliente.SaldoDevedor += totalVenda;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await ObterPorIdAsync(venda.Id))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>Consulta de vendas com filtros de período, forma de pagamento e cliente.</summary>
    public async Task<List<VendaDto>> ListarAsync(DateTime? inicio = null, DateTime? fim = null, string? formaPagamento = null, int? clienteId = null, int pagina = 1, int tamanhoPagina = 50)
    {
        var query = _context.Vendas
            .AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Publicacao)
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .AsQueryable();

        if (inicio.HasValue) query = query.Where(v => v.DataVenda >= inicio.Value);
        if (fim.HasValue) query = query.Where(v => v.DataVenda <= fim.Value);
        if (!string.IsNullOrWhiteSpace(formaPagamento)) query = query.Where(v => v.FormaPagamento == formaPagamento);
        if (clienteId.HasValue) query = query.Where(v => v.ClienteId == clienteId);

        var vendas = await query
            .OrderByDescending(v => v.DataVenda)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return vendas.Select(Mapear).ToList();
    }

    public async Task<VendaDto?> ObterPorIdAsync(int id)
    {
        var venda = await _context.Vendas
            .AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Publicacao)
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);
        return venda == null ? null : Mapear(venda);
    }

    /// <summary>
    /// Cancela uma venda, desfazendo todos os efeitos dela dentro de uma
    /// transação: devolve os itens ao estoque, registra o estorno e, se a
    /// venda era fiada, desconta o valor do saldo devedor do cliente.
    /// </summary>
    public async Task<bool> CancelarAsync(int id, string motivo, int? usuarioId)
    {
        var venda = await _context.Vendas
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venda == null || venda.Status == nameof(StatusVenda.Cancelada))
            return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in venda.Itens)
            {
                var publicacao = await _context.Publicacoes.FindAsync(item.PublicacaoId);
                if (publicacao != null) publicacao.Estoque += item.Quantidade;

                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    PublicacaoId = item.PublicacaoId,
                    Tipo = nameof(TipoMovimentacao.Devolucao),
                    Quantidade = item.Quantidade,
                    UsuarioId = usuarioId,
                    Motivo = $"Estorno - Venda #{venda.Id}: {motivo}",
                    ReferenciaVendaId = venda.Id
                });
            }

            if (venda.ClienteId.HasValue &&
                venda.FormaPagamento.Equals(nameof(FormaPagamento.Fiado), StringComparison.OrdinalIgnoreCase))
            {
                var cliente = await _context.Clientes.FindAsync(venda.ClienteId.Value);
                if (cliente != null) cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - venda.ValorTotal);
            }

            venda.Status = nameof(StatusVenda.Cancelada);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    private static VendaDto Mapear(Venda v) => new(
        v.Id, v.DataVenda, v.ValorTotal, v.Desconto, v.FormaPagamento, v.PixTxId, v.Status,
        v.CaixaId, v.ClienteId, v.Cliente?.Nome, v.Usuario?.Nome,
        v.Itens.Select(i => new ItemVendaDto(i.Id, i.PublicacaoId, i.Publicacao?.Titulo ?? string.Empty,
            i.Quantidade, i.PrecoUnitario, i.Desconto, i.Subtotal)).ToList());
}

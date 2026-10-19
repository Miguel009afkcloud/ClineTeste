using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Dashboard e relatórios gerenciais da banca: faturamento do dia e do mês,
/// ticket médio, formas de pagamento, publicações mais vendidas, situation
/// do estoque e carteira de assinaturas.
/// </summary>
public class RelatorioService
{
    private readonly Data.BancaContext _context;

    public RelatorioService(Data.BancaContext context) => _context = context;

    /// <summary>Consolida os indicadores principais exibidos no painel inicial.</summary>
    public async Task<DashboardDto> MontarDashboardAsync()
    {
        var agora = DateTime.UtcNow;
        var inicioHoje = agora.Date;
        var fimHoje = inicioHoje.AddDays(1).AddTicks(-1);
        var inicioMes = new DateTime(agora.Year, agora.Month, 1);
        var inicio30 = agora.AddDays(-29);

        var vendasHoje = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicioHoje && v.DataVenda <= fimHoje && v.Status == nameof(StatusVenda.Concluida))
            .ToListAsync();

        var totalHoje = vendasHoje.Sum(v => v.ValorTotal);

        var vendasMes = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicioMes && v.Status == nameof(StatusVenda.Concluida))
            .ToListAsync();

        var porForma = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio30 && v.Status == nameof(StatusVenda.Concluida))
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new ResumoFormaPagamento(g.Key, g.Count(), g.Sum(v => v.ValorTotal)))
            .ToListAsync();

        var porDia = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio30 && v.Status == nameof(StatusVenda.Concluida))
            .GroupBy(v => v.DataVenda.Date)
            .OrderBy(g => g.Key)
            .Select(g => new VendaPorDia(g.Key, g.Count(), g.Sum(v => v.ValorTotal)))
            .ToListAsync();

        // O agrupamento por publicação com ordenação sobre o resultado
        // projetado não é traduzível pelo provedor SQL, por isso os itens
        // elegíveis são materializados e agregados em memória.
        var itensTop = await _context.ItensVendas
            .AsNoTracking()
            .Where(i => i.Venda!.Status == nameof(StatusVenda.Concluida) && i.Venda.DataVenda >= inicio30)
            .Select(i => new { i.PublicacaoId, i.Publicacao!.Titulo, i.Quantidade, i.PrecoUnitario, i.Desconto })
            .ToListAsync();

        var top = itensTop
            .GroupBy(i => new { i.PublicacaoId, i.Titulo })
            .Select(g => new TopPublicacao(
                g.Key.PublicacaoId,
                g.Key.Titulo,
                g.Sum(i => i.Quantidade),
                g.Sum(i => i.Quantidade * i.PrecoUnitario - i.Desconto)))
            .OrderByDescending(g => g.Quantidade)
            .Take(10)
            .ToList();

        var totalPublicacoes = await _context.Publicacoes.CountAsync();
        var ativas = await _context.Publicacoes.CountAsync(p => p.Ativo);
        var criticas = await _context.Publicacoes.CountAsync(p => p.Ativo && p.EstoqueMinimo > 0 && p.Estoque <= p.EstoqueMinimo);
        var saldoDevedor = await _context.Clientes.Where(c => c.Ativo).SumAsync(c => c.SaldoDevedor);

        var assinaturas = await _context.Assinaturas
            .AsNoTracking()
            .Where(a => a.Status == nameof(StatusAssinatura.Ativa))
            .ToListAsync();

        var porStatus = await _context.Assinaturas
            .AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new ResumoAssinatura(g.Key, g.Count()))
            .ToListAsync();

        return new DashboardDto(
            totalHoje,
            vendasHoje.Count,
            vendasHoje.Count > 0 ? totalHoje / vendasHoje.Count : 0,
            vendasMes.Sum(v => v.ValorTotal),
            vendasMes.Count,
            vendasMes.Sum(v => v.ValorTotal),
            totalPublicacoes,
            ativas,
            criticas,
            saldoDevedor,
            assinaturas.Count,
            assinaturas.Sum(a => a.ValorMensal),
            porForma,
            porDia,
            top,
            porStatus);
    }

    /// <summary>Relatório de vendas por período com os principais recortes.</summary>
    public async Task<RelatorioVendasDto> RelatorioVendasAsync(DateTime inicio, DateTime fim)
    {
        var vendas = await _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == nameof(StatusVenda.Concluida))
            .ToListAsync();

        var total = vendas.Sum(v => v.ValorTotal);

        var porForma = vendas
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new ResumoFormaPagamento(g.Key, g.Count(), g.Sum(v => v.ValorTotal)))
            .OrderByDescending(g => g.Total)
            .ToList();

        // Mesma estratégia do dashboard: materializa os itens e agrega em
        // memória, pois a ordenação sobre o resultado projetado não é
        // traduzível pelo provedor SQL.
        var itensTop = await _context.ItensVendas
            .AsNoTracking()
            .Where(i => i.Venda!.DataVenda >= inicio && i.Venda.DataVenda <= fim && i.Venda.Status == nameof(StatusVenda.Concluida))
            .Select(i => new { i.PublicacaoId, i.Publicacao!.Titulo, i.Quantidade, i.PrecoUnitario, i.Desconto })
            .ToListAsync();

        var top = itensTop
            .GroupBy(i => new { i.PublicacaoId, i.Titulo })
            .Select(g => new TopPublicacao(
                g.Key.PublicacaoId,
                g.Key.Titulo,
                g.Sum(i => i.Quantidade),
                g.Sum(i => i.Quantidade * i.PrecoUnitario - i.Desconto)))
            .OrderByDescending(g => g.Quantidade)
            .Take(15)
            .ToList();

        return new RelatorioVendasDto(
            inicio, fim, vendas.Count, total,
            vendas.Count > 0 ? total / vendas.Count : 0,
            vendas.Sum(v => v.Desconto),
            porForma, top);
    }
}

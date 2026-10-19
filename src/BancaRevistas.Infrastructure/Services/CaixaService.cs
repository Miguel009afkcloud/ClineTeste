using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Controla a abertura, o fechamento e as sangrias do caixa da banca.
/// Só pode existir um caixa aberto por vez.
/// </summary>
public class CaixaService
{
    private readonly Data.BancaContext _context;

    public CaixaService(Data.BancaContext context) => _context = context;

    /// <summary>
    /// Abre um novo caixa. Lança <see cref="InvalidOperationException"/> se já
    /// existir um caixa aberto, evitando vendas paralelas em dois caixas.
    /// </summary>
    public async Task<CaixaDto> AbrirAsync(AbrirCaixaRequest request, int usuarioId)
    {
        if (await ObterAbertoAsync() != null)
            throw new InvalidOperationException("Já existe um caixa aberto. Feche o caixa atual antes de abrir outro.");

        var caixa = new Caixa
        {
            UsuarioId = usuarioId,
            Abertura = DateTime.UtcNow,
            ValorInicial = request.ValorInicial,
            Status = nameof(StatusCaixa.Aberto)
        };

        _context.Caixas.Add(caixa);
        await _context.SaveChangesAsync();
        return (await ObterPorIdAsync(caixa.Id))!;
    }

    /// <summary>Caixa aberto no momento, ou null quando não há caixa aberto.</summary>
    public async Task<CaixaDto?> ObterAbertoAsync()
    {
        var caixa = await _context.Caixas
            .AsNoTracking()
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Status == nameof(StatusCaixa.Aberto));
        return caixa == null ? null : await MapearAsync(caixa);
    }

    public async Task<CaixaDto?> ObterPorIdAsync(int id)
    {
        var caixa = await _context.Caixas
            .AsNoTracking()
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);
        return caixa == null ? null : await MapearAsync(caixa);
    }

    /// <summary>Histórico de caixas, do mais recente para o mais antigo.</summary>
    public async Task<List<CaixaDto>> ListarAsync()
    {
        var caixas = await _context.Caixas
            .AsNoTracking()
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.Abertura)
            .ToListAsync();

        var resultado = new List<CaixaDto>(caixas.Count);
        foreach (var caixa in caixas)
            resultado.Add(await MapearAsync(caixa));
        return resultado;
    }

    /// <summary>
    /// Fecha o caixa apurando o valor final (valor inicial + vendas concluídas
    /// - sangrias) e comparando com o valor conferido pelo operador.
    /// </summary>
    public async Task<bool> FecharAsync(int id, FecharCaixaRequest request)
    {
        var caixa = await _context.Caixas.FindAsync(id);
        if (caixa == null || caixa.Status != nameof(StatusCaixa.Aberto)) return false;

        var totalVendas = await _context.Vendas
            .Where(v => v.CaixaId == id && v.Status == nameof(StatusVenda.Concluida))
            .SumAsync(v => v.ValorTotal);

        var sangria = caixa.Sangria ?? 0;
        caixa.ValorFinal = caixa.ValorInicial + totalVendas - sangria;
        caixa.ValorConferido = request.ValorConferido;
        caixa.Diferenca = request.ValorConferido - caixa.ValorFinal;
        caixa.Fechamento = DateTime.UtcNow;
        caixa.Status = nameof(StatusCaixa.Fechado);

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Registra uma sangria (retirada de dinheiro do caixa). O valor é
    /// acumulado e impacta o cálculo do fechamento.
    /// </summary>
    public async Task<bool> RegistrarSangriaAsync(int id, decimal valor, string? motivo)
    {
        if (valor <= 0)
            throw new ArgumentException("O valor da sangria deve ser maior que zero.", nameof(valor));

        var caixa = await _context.Caixas.FindAsync(id);
        if (caixa == null || caixa.Status != nameof(StatusCaixa.Aberto)) return false;

        caixa.Sangria = (caixa.Sangria ?? 0) + valor;
        caixa.ValorFinal = caixa.ValorFinal ?? caixa.ValorInicial;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>Projeta o DTO do caixa, já com o total de vendas apurado.</summary>
    private async Task<CaixaDto> MapearAsync(Caixa caixa)
    {
        var totalVendas = await _context.Vendas
            .Where(v => v.CaixaId == caixa.Id && v.Status == nameof(StatusVenda.Concluida))
            .SumAsync(v => v.ValorTotal);
        var contagem = await _context.Vendas
            .Where(v => v.CaixaId == caixa.Id && v.Status == nameof(StatusVenda.Concluida))
            .CountAsync();

        return new CaixaDto(
            caixa.Id, caixa.UsuarioId, caixa.Usuario?.Nome, caixa.Abertura, caixa.Fechamento,
            caixa.ValorInicial, caixa.ValorFinal, caixa.Sangria, caixa.ValorConferido, caixa.Diferenca,
            caixa.Status, totalVendas, contagem);
    }
}

using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Serviço de assinaturas: o cliente recebe a publicação periodicamente e paga
/// um valor mensal. É o serviço de recorrência que diferencia a banca de
/// uma simples loja de revistas.
/// </summary>
public class AssinaturaService
{
    private readonly Data.BancaContext _context;

    public AssinaturaService(Data.BancaContext context) => _context = context;

    /// <summary>
    /// Cria uma assinatura. Calcula automaticamente a data da próxima entrega
    /// conforme a periodicidade escolhida e valida que o cliente e a publicação
    /// existam e estejam ativos.
    /// </summary>
    public async Task<AssinaturaDto> CriarAsync(AssinaturaInput input, int usuarioId)
    {
        var cliente = await _context.Clientes.FindAsync(input.ClienteId)
            ?? throw new RegraDeNegocioException("Cliente não encontrado.");
        if (!cliente.Ativo)
            throw new RegraDeNegocioException("Cliente não está ativo.");

        var publicacao = await _context.Publicacoes.FindAsync(input.PublicacaoId)
            ?? throw new RegraDeNegocioException("Publicação não encontrada.");
        if (!publicacao.Ativo)
            throw new RegraDeNegocioException("Publicação não está ativa.");

        var inicio = input.DataInicio ?? DateTime.UtcNow;
        var assinatura = new Assinatura
        {
            ClienteId = input.ClienteId,
            PublicacaoId = input.PublicacaoId,
            ValorMensal = input.ValorMensal,
            Periodicidade = input.Periodicidade,
            DataInicio = inicio,
            ProximaEntrega = CalcularProximaEntrega(inicio, input.Periodicidade),
            Status = nameof(StatusAssinatura.Ativa),
            UsuarioId = usuarioId
        };

        _context.Assinaturas.Add(assinatura);
        await _context.SaveChangesAsync();
        return (await ObterPorIdAsync(assinatura.Id))!;
    }

    public async Task<List<AssinaturaDto>> ListarAsync(string? status = null, int? clienteId = null)
    {
        var query = _context.Assinaturas
            .AsNoTracking()
            .Include(a => a.Cliente)
            .Include(a => a.Publicacao)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (clienteId.HasValue) query = query.Where(a => a.ClienteId == clienteId);

        return await query
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AssinaturaDto(a.Id, a.ClienteId, a.Cliente!.Nome, a.PublicacaoId, a.Publicacao!.Titulo,
                a.DataInicio, a.ProximaEntrega, a.ValorMensal, a.Periodicidade, a.Status))
            .ToListAsync();
    }

    public async Task<AssinaturaDto?> ObterPorIdAsync(int id)
    {
        var a = await _context.Assinaturas.AsNoTracking()
            .Include(x => x.Cliente).Include(x => x.Publicacao)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return null;
        return new AssinaturaDto(a.Id, a.ClienteId, a.Cliente?.Nome, a.PublicacaoId, a.Publicacao?.Titulo,
            a.DataInicio, a.ProximaEntrega, a.ValorMensal, a.Periodicidade, a.Status);
    }

    /// <summary>Altera a situação da assinatura (ativa, pausada, cancelada, expirada).</summary>
    public async Task<AssinaturaDto?> AlterarStatusAsync(int id, StatusAssinatura status)
    {
        var assinatura = await _context.Assinaturas.FindAsync(id);
        if (assinatura == null) return null;

        assinatura.Status = status.ToString();
        if (status == StatusAssinatura.Cancelada)
            assinatura.DataFim = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await ObterPorIdAsync(id);
    }

    /// <summary>
    /// Processa as entregas vencidas: gera a movimentação de saída do estoque
    /// e avança a data da próxima entrega. Usado pelo job diário.
    /// </summary>
    public async Task<int> ProcessarEntregasAsync(int? usuarioId = null)
    {
        var hoje = DateTime.UtcNow;
        var vencidas = await _context.Assinaturas
            .Include(a => a.Publicacao)
            .Where(a => a.Status == nameof(StatusAssinatura.Ativa) && a.ProximaEntrega <= hoje)
            .ToListAsync();

        foreach (var assinatura in vencidas)
        {
            var publicacao = assinatura.Publicacao;
            if (publicacao != null && publicacao.Estoque > 0)
            {
                publicacao.Estoque -= 1;
                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    PublicacaoId = publicacao.Id,
                    Tipo = nameof(TipoMovimentacao.Saida),
                    Quantidade = 1,
                    PrecoUnitario = publicacao.Preco,
                    UsuarioId = usuarioId,
                    Motivo = $"Entrega de assinatura #{assinatura.Id}"
                });
            }

            assinatura.ProximaEntrega = CalcularProximaEntrega(hoje, assinatura.Periodicidade);
        }

        await _context.SaveChangesAsync();
        return vencidas.Count;
    }

    /// <summary>Calcula a próxima entrega conforme a periodicidade escolhida.</summary>
    public static DateTime CalcularProximaEntrega(DateTime baseData, string periodicidade) => periodicidade.ToLowerInvariant() switch
    {
        "bimestral" => baseData.AddMonths(2),
        "trimestral" => baseData.AddMonths(3),
        "semestral" => baseData.AddMonths(6),
        "anual" => baseData.AddYears(1),
        _ => baseData.AddMonths(1)
    };
}

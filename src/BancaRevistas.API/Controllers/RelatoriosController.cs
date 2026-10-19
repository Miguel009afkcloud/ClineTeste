using BancaRevistas.Domain.DTOs;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Dashboard e relatórios gerenciais da banca.
/// </summary>
[ApiController]
[Route("api/relatorios")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly RelatorioService _relatorios;

    public RelatoriosController(RelatorioService relatorios) => _relatorios = relatorios;

    /// <summary>Indicadores consolidados do painel principal.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard() => Ok(await _relatorios.MontarDashboardAsync());

    /// <summary>Relatório de vendas por período.</summary>
    [HttpGet("vendas")]
    public async Task<ActionResult<RelatorioVendasDto>> Vendas([FromQuery] DateTime? inicio = null, [FromQuery] DateTime? fim = null)
    {
        var fimPadrao = DateTime.UtcNow;
        var inicioPadrao = fimPadrao.AddDays(-29);
        return Ok(await _relatorios.RelatorioVendasAsync(inicio ?? inicioPadrao, fim ?? fimPadrao));
    }
}

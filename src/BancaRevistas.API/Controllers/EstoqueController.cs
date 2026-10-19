using BancaRevistas.Domain.DTOs;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Gestão de estoque da banca: ajustes manuais, registro de perdas e
/// histórico de movimentações.
/// </summary>
[ApiController]
[Route("api/estoque")]
[Authorize]
public class EstoqueController : ControllerBase
{
    private readonly EstoqueService _estoque;

    public EstoqueController(EstoqueService estoque) => _estoque = estoque;

    /// <summary>Histórico de movimentações de estoque.</summary>
    [HttpGet("movimentacoes")]
    public async Task<ActionResult<List<MovimentacaoEstoqueDto>>> Historico(
        [FromQuery] DateTime? inicio = null,
        [FromQuery] DateTime? fim = null,
        [FromQuery] int? publicacaoId = null) =>
        Ok(await _estoque.HistoricoAsync(inicio, fim, publicacaoId));

    /// <summary>Ajusta o saldo de estoque de uma publicação (soma ou subtração).</summary>
    [HttpPost("ajustar/{publicacaoId:int}")]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult> Ajustar(int publicacaoId, [FromBody] AjustarEstoqueRequest request)
    {
        var ok = await _estoque.AjustarAsync(publicacaoId, request.Quantidade, request.Motivo, request.Somar, UsuarioId);
        if (!ok) return NotFound(new { mensagem = "Publicação não encontrada." });
        return Ok(new { mensagem = "Estoque ajustado." });
    }

    /// <summary>Registra a perda ou devolução de exemplares.</summary>
    [HttpPost("perda/{publicacaoId:int}")]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult> RegistrarPerda(int publicacaoId, [FromBody] AjustarEstoqueRequest request)
    {
        var ok = await _estoque.RegistrarPerdaAsync(publicacaoId, request.Quantidade, request.Motivo, UsuarioId);
        if (!ok) return NotFound(new { mensagem = "Publicação não encontrada." });
        return Ok(new { mensagem = "Perda registrada." });
    }

    private int UsuarioId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Enums;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Assinaturas de publicações: o serviço de recorrência da banca, em que o
/// cliente recebe a edição periodicamente e paga um valor mensal.
/// </summary>
[ApiController]
[Route("api/assinaturas")]
[Authorize]
public class AssinaturasController : ControllerBase
{
    private readonly AssinaturaService _assinaturas;

    public AssinaturasController(AssinaturaService assinaturas) => _assinaturas = assinaturas;

    [HttpGet]
    public async Task<ActionResult<List<AssinaturaDto>>> Listar([FromQuery] string? status = null, [FromQuery] int? clienteId = null) =>
        Ok(await _assinaturas.ListarAsync(status, clienteId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssinaturaDto>> Obter(int id)
    {
        var assinatura = await _assinaturas.ObterPorIdAsync(id);
        if (assinatura == null) return NotFound(new { mensagem = "Assinatura não encontrada." });
        return Ok(assinatura);
    }

    /// <summary>Cria uma nova assinatura e calcula a primeira entrega.</summary>
    [HttpPost]
    public async Task<ActionResult<AssinaturaDto>> Criar([FromBody] AssinaturaInput input)
    {
        var assinatura = await _assinaturas.CriarAsync(input, UsuarioId);
        return CreatedAtAction(nameof(Obter), new { id = assinatura.Id }, assinatura);
    }

    /// <summary>Altera a situação da assinatura (ativa, pausada, cancelada, expirada).</summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<AssinaturaDto>> AlterarStatus(int id, [FromBody] AlterarStatusAssinaturaRequest request)
    {
        var assinatura = await _assinaturas.AlterarStatusAsync(id, request.Status);
        if (assinatura == null) return NotFound(new { mensagem = "Assinatura não encontrada." });
        return Ok(assinatura);
    }

    /// <summary>Processa as entregas vencidas, baixando o estoque e avançando o ciclo.</summary>
    [HttpPost("processar-entregas")]
    [Authorize(Roles = "Administrador,Atendente")]
    public async Task<ActionResult> ProcessarEntregas()
    {
        var processadas = await _assinaturas.ProcessarEntregasAsync(UsuarioId);
        return Ok(new { mensagem = $"{processadas} entrega(s) processada(s).", quantidade = processadas });
    }

    private int UsuarioId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

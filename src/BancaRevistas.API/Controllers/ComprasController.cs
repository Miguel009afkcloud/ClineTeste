using BancaRevistas.Domain.DTOs;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Compras de reposição de estoque junto às distribuidoras.
/// </summary>
[ApiController]
[Route("api/compras")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly CompraService _compras;

    public ComprasController(CompraService compras) => _compras = compras;

    /// <summary>Registra uma compra, dando entrada no estoque das publicações.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<CompraDto>> Registrar([FromBody] CompraInput input)
    {
        var compra = await _compras.RegistrarAsync(input, UsuarioId);
        return CreatedAtAction(nameof(Obter), new { id = compra.Id }, compra);
    }

    [HttpGet]
    public async Task<ActionResult<List<CompraDto>>> Listar(
        [FromQuery] DateTime? inicio = null,
        [FromQuery] DateTime? fim = null,
        [FromQuery] int? fornecedorId = null) =>
        Ok(await _compras.ListarAsync(inicio, fim, fornecedorId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompraDto>> Obter(int id)
    {
        var compra = await _compras.ObterPorIdAsync(id);
        if (compra == null) return NotFound(new { mensagem = "Compra não encontrada." });
        return Ok(compra);
    }

    private int UsuarioId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

using BancaRevistas.Domain.DTOs;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// PDV da banca: registro de vendas, consulta do histórico e cancelamento
/// com estorno de estoque.
/// </summary>
[ApiController]
[Route("api/vendas")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly VendaService _vendas;

    public VendasController(VendaService vendas) => _vendas = vendas;

    /// <summary>Registra uma venda. Exige caixa aberto e estoque disponível.</summary>
    [HttpPost]
    public async Task<ActionResult<VendaDto>> Registrar([FromBody] VendaInput input)
    {
        var venda = await _vendas.RegistrarAsync(input, UsuarioId);
        return CreatedAtAction(nameof(Obter), new { id = venda.Id }, venda);
    }

    /// <summary>Lista o histórico de vendas com filtros.</summary>
    [HttpGet]
    public async Task<ActionResult<List<VendaDto>>> Listar(
        [FromQuery] DateTime? inicio = null,
        [FromQuery] DateTime? fim = null,
        [FromQuery] string? formaPagamento = null,
        [FromQuery] int? clienteId = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 50) =>
        Ok(await _vendas.ListarAsync(inicio, fim, formaPagamento, clienteId, pagina, tamanhoPagina));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendaDto>> Obter(int id)
    {
        var venda = await _vendas.ObterPorIdAsync(id);
        if (venda == null) return NotFound(new { mensagem = "Venda não encontrada." });
        return Ok(venda);
    }

    /// <summary>Cancela a venda, devolvendo os itens ao estoque.</summary>
    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult> Cancelar(int id, [FromBody] CancelarVendaRequest request)
    {
        var ok = await _vendas.CancelarAsync(id, request.Motivo, UsuarioId);
        if (!ok) return BadRequest(new { mensagem = "Venda não encontrada ou já cancelada." });
        return Ok(new { mensagem = "Venda cancelada e estoque estornado." });
    }

    private int UsuarioId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

/// <summary>
/// Sessão de caixa da banca: abertura, fechamento e sangrias.
/// </summary>
[ApiController]
[Route("api/caixa")]
[Authorize]
public class CaixaController : ControllerBase
{
    private readonly CaixaService _caixa;

    public CaixaController(CaixaService caixa) => _caixa = caixa;

    /// <summary>Caixa aberto no momento, se houver.</summary>
    [HttpGet("atual")]
    public async Task<ActionResult<CaixaDto>> Atual()
    {
        var caixa = await _caixa.ObterAbertoAsync();
        if (caixa == null) return NotFound(new { mensagem = "Nenhum caixa aberto." });
        return Ok(caixa);
    }

    [HttpGet]
    public async Task<ActionResult<List<CaixaDto>>> Listar() => Ok(await _caixa.ListarAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CaixaDto>> Obter(int id)
    {
        var caixa = await _caixa.ObterPorIdAsync(id);
        if (caixa == null) return NotFound(new { mensagem = "Caixa não encontrado." });
        return Ok(caixa);
    }

    /// <summary>Abre um novo caixa. Só pode existir um caixa aberto por vez.</summary>
    [HttpPost("abrir")]
    public async Task<ActionResult<CaixaDto>> Abrir([FromBody] AbrirCaixaRequest request)
    {
        var caixa = await _caixa.AbrirAsync(request, UsuarioId);
        return Ok(caixa);
    }

    /// <summary>Fecha o caixa apurando o valor final e a diferença do conferido.</summary>
    [HttpPost("{id:int}/fechar")]
    public async Task<ActionResult<CaixaDto>> Fechar(int id, [FromBody] FecharCaixaRequest request)
    {
        var ok = await _caixa.FecharAsync(id, request);
        if (!ok) return BadRequest(new { mensagem = "Caixa não encontrado ou já fechado." });
        return Ok(await _caixa.ObterPorIdAsync(id));
    }

    /// <summary>Registra uma sangria (retirada de dinheiro do caixa).</summary>
    [HttpPost("{id:int}/sangria")]
    public async Task<ActionResult> Sangria(int id, [FromBody] SangriaRequest request)
    {
        var ok = await _caixa.RegistrarSangriaAsync(id, request.Valor, request.Motivo);
        if (!ok) return BadRequest(new { mensagem = "Caixa não encontrado ou já fechado." });
        return Ok(new { mensagem = "Sangria registrada." });
    }

    private int UsuarioId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

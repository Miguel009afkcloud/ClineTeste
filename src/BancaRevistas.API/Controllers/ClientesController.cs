using BancaRevistas.Domain.DTOs;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Cadastro de clientes da banca, com controle de crédito para as vendas a
/// prazo e as assinaturas ativas.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _clientes;

    public ClientesController(ClienteService clientes) => _clientes = clientes;

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Listar([FromQuery] string? busca = null, [FromQuery] bool apenasAtivos = true) =>
        Ok(await _clientes.ListarAsync(busca, apenasAtivos));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Obter(int id)
    {
        var cliente = await _clientes.ObterPorIdAsync(id);
        if (cliente == null) return NotFound(new { mensagem = "Cliente não encontrado." });
        return Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Criar([FromBody] ClienteInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Nome))
            return BadRequest(new { mensagem = "O nome do cliente é obrigatório." });

        var cliente = await _clientes.CriarAsync(input);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Atualizar(int id, [FromBody] ClienteInput input)
    {
        var cliente = await _clientes.AtualizarAsync(id, input);
        if (cliente == null) return NotFound(new { mensagem = "Cliente não encontrado." });
        return Ok(cliente);
    }

    /// <summary>Registra um pagamento do cliente, reduzindo o saldo devedor.</summary>
    [HttpPost("{id:int}/liquidar")]
    public async Task<ActionResult> Liquidar(int id, [FromBody] LiquidarDividaRequest request)
    {
        var ok = await _clientes.LiquidarDividaAsync(id, request.Valor);
        if (!ok) return NotFound(new { mensagem = "Cliente não encontrado." });
        return Ok(new { mensagem = "Pagamento registrado." });
    }
}

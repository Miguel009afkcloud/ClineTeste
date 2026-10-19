using BancaRevistas.Domain.DTOs;
using BancaRevistas.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BancaRevistas.API.Controllers;

/// <summary>
/// Catálogo de publicações e itens vendidos na banca (revistas, jornais,
/// quadrinhos, papelaria e conveniências).
/// </summary>
[ApiController]
[Route("api/publicacoes")]
[Authorize]
public class PublicacoesController : ControllerBase
{
    private readonly PublicacaoService _publicacoes;

    public PublicacoesController(PublicacaoService publicacoes) => _publicacoes = publicacoes;

    /// <summary>Lista o catálogo com filtros de busca, categoria e fornecedor.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PublicacaoDto>>> Listar(
        [FromQuery] string? busca = null,
        [FromQuery] bool apenasAtivos = true,
        [FromQuery] int? categoriaId = null,
        [FromQuery] int? fornecedorId = null) =>
        Ok(await _publicacoes.ListarAsync(busca, apenasAtivos, categoriaId, fornecedorId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublicacaoDto>> Obter(int id)
    {
        var publicacao = await _publicacoes.ObterPorIdAsync(id);
        if (publicacao == null) return NotFound(new { mensagem = "Publicação não encontrada." });
        return Ok(publicacao);
    }

    /// <summary>Busca exata por código de barras (leitor do PDV).</summary>
    [HttpGet("codigo-barras/{codigo}")]
    public async Task<ActionResult<PublicacaoDto>> ObterPorCodigoBarras(string codigo)
    {
        var publicacao = await _publicacoes.ObterPorCodigoBarrasAsync(codigo);
        if (publicacao == null) return NotFound(new { mensagem = "Nenhuma publicação encontrada para o código informado." });
        return Ok(publicacao);
    }

    /// <summary>Publicações que atingiram o estoque mínimo (alerta de reposição).</summary>
    [HttpGet("estoque-critico")]
    public async Task<ActionResult<List<EstoqueBaixoDto>>> EstoqueCritico() =>
        Ok(await _publicacoes.ListarEstoqueCriticoAsync());

    [HttpPost]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<PublicacaoDto>> Criar([FromBody] PublicacaoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Titulo))
            return BadRequest(new { mensagem = "O título é obrigatório." });
        if (input.Preco <= 0)
            return BadRequest(new { mensagem = "O preço de venda deve ser maior que zero." });

        var publicacao = await _publicacoes.CriarAsync(input, UsuarioId);
        return CreatedAtAction(nameof(Obter), new { id = publicacao.Id }, publicacao);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<PublicacaoDto>> Atualizar(int id, [FromBody] PublicacaoInput input)
    {
        var publicacao = await _publicacoes.AtualizarAsync(id, input);
        if (publicacao == null) return NotFound(new { mensagem = "Publicação não encontrada." });
        return Ok(publicacao);
    }

    /// <summary>Desativa a publicação preservando o histórico de vendas.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult> Desativar(int id)
    {
        var ok = await _publicacoes.DesativarAsync(id);
        if (!ok) return NotFound(new { mensagem = "Publicação não encontrada." });
        return Ok(new { mensagem = "Publicação desativada." });
    }

    private int UsuarioId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

/// <summary>
/// Categorias do catálogo de publicações.
/// </summary>
[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly CatalogoService _catalogo;

    public CategoriasController(CatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet]
    public async Task<ActionResult<List<CategoriaDto>>> Listar([FromQuery] bool apenasAtivas = true) =>
        Ok(await _catalogo.ListarCategoriasAsync(apenasAtivas));

    [HttpPost]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<CategoriaDto>> Criar([FromBody] CategoriaInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Nome))
            return BadRequest(new { mensagem = "O nome da categoria é obrigatório." });
        return Ok(await _catalogo.CriarCategoriaAsync(input));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<CategoriaDto>> Atualizar(int id, [FromBody] CategoriaInput input)
    {
        var categoria = await _catalogo.AtualizarCategoriaAsync(id, input);
        if (categoria == null) return NotFound(new { mensagem = "Categoria não encontrada." });
        return Ok(categoria);
    }
}

/// <summary>
/// Fornecedores (distribuidoras e editoras) da banca.
/// </summary>
[ApiController]
[Route("api/fornecedores")]
[Authorize]
public class FornecedoresController : ControllerBase
{
    private readonly CatalogoService _catalogo;

    public FornecedoresController(CatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet]
    public async Task<ActionResult<List<FornecedorDto>>> Listar([FromQuery] bool apenasAtivos = true) =>
        Ok(await _catalogo.ListarFornecedoresAsync(apenasAtivos));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FornecedorDto>> Obter(int id)
    {
        var fornecedor = await _catalogo.ObterFornecedorAsync(id);
        if (fornecedor == null) return NotFound(new { mensagem = "Fornecedor não encontrado." });
        return Ok(fornecedor);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<FornecedorDto>> Criar([FromBody] FornecedorInput input)
    {
        if (string.IsNullOrWhiteSpace(input.RazaoSocial))
            return BadRequest(new { mensagem = "A razão social é obrigatória." });
        return Ok(await _catalogo.CriarFornecedorAsync(input));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador,Estoquista")]
    public async Task<ActionResult<FornecedorDto>> Atualizar(int id, [FromBody] FornecedorInput input)
    {
        var fornecedor = await _catalogo.AtualizarFornecedorAsync(id, input);
        if (fornecedor == null) return NotFound(new { mensagem = "Fornecedor não encontrado." });
        return Ok(fornecedor);
    }
}

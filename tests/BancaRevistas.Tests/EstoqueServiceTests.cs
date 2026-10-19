using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Infrastructure.Services;

namespace BancaRevistas.Tests;

/// <summary>
/// Testes do controle de estoque: ajustes manuais, perdas, histórico de
/// movimentações e alerta de estoque crítico.
/// </summary>
public class EstoqueServiceTests
{
    [Fact]
    public async Task DeveSomarEstoqueEAregistrarMovimentacao()
    {
        using var banco = new BancoTeste();
        var publicacaoId = banco.CriarPublicacao(estoque: 10);
        var servico = new EstoqueService(banco.Context);

        var ok = await servico.AjustarAsync(publicacaoId, 15, "Reposição do fornecedor", somar: true, usuarioId: null);
        Assert.True(ok);

        var publicacao = await banco.Context.Publicacoes.FindAsync(publicacaoId);
        Assert.Equal(25, publicacao!.Estoque);

        var historico = await servico.HistoricoAsync();
        Assert.Single(historico);
        Assert.Equal("Entrada", historico[0].Tipo);
        Assert.Equal(15, historico[0].Quantidade);
    }

    [Fact]
    public async Task DeveSubtrairEstoque()
    {
        using var banco = new BancoTeste();
        var publicacaoId = banco.CriarPublicacao(estoque: 10);
        var servico = new EstoqueService(banco.Context);

        await servico.AjustarAsync(publicacaoId, 4, "Perda", somar: false, usuarioId: null);

        var publicacao = await banco.Context.Publicacoes.FindAsync(publicacaoId);
        Assert.Equal(6, publicacao!.Estoque);
    }

    [Fact]
    public async Task DeveRejeitarSubtracao_AcimaDoEstoque()
    {
        using var banco = new BancoTeste();
        var publicacaoId = banco.CriarPublicacao(estoque: 3);
        var servico = new EstoqueService(banco.Context);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => servico.AjustarAsync(publicacaoId, 10, "Ajuste", somar: false, usuarioId: null));
    }

    [Fact]
    public async Task DeveRegistrarPerda()
    {
        using var banco = new BancoTeste();
        var publicacaoId = banco.CriarPublicacao(estoque: 10);
        var servico = new EstoqueService(banco.Context);

        await servico.RegistrarPerdaAsync(publicacaoId, 2, "Exemplar danificado", null);

        var publicacao = await banco.Context.Publicacoes.FindAsync(publicacaoId);
        Assert.Equal(8, publicacao!.Estoque);

        var historico = await servico.HistoricoAsync();
        Assert.Equal("Perda", historico[0].Tipo);
    }

    [Fact]
    public async Task DeveListarEstoqueCritico()
    {
        using var banco = new BancoTeste();
        banco.CriarPublicacao("Estoque Ok", estoque: 50, estoqueMinimo: 10);
        banco.CriarPublicacao("Estoque Critico", estoque: 3, estoqueMinimo: 10);
        banco.CriarPublicacao("Sem Minimo", estoque: 0, estoqueMinimo: 0);

        var servico = new PublicacaoService(banco.Context);

        var criticos = await servico.ListarEstoqueCriticoAsync();

        Assert.Single(criticos);
        Assert.Equal("Estoque Critico", criticos[0].Titulo);
    }

    [Fact]
    public async Task DeveRegistrarMovimentacaoDeEntrada_AoCriarPublicacao()
    {
        using var banco = new BancoTeste();
        var servico = new PublicacaoService(banco.Context);

        await servico.CriarAsync(new PublicacaoInput
        {
            Titulo = "Nova Revista",
            Preco = 12m,
            PrecoCusto = 6m,
            Estoque = 25,
            EstoqueMinimo = 5
        });

        var historico = await new EstoqueService(banco.Context).HistoricoAsync();
        Assert.Single(historico);
        Assert.Equal("Entrada", historico[0].Tipo);
        Assert.Equal("Estoque inicial", historico[0].Motivo);
    }
}

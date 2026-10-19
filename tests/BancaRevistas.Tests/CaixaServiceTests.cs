using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Infrastructure.Services;

namespace BancaRevistas.Tests;

/// <summary>
/// Testes do controle de caixa da banca: abertura única, fechamento com
/// apuração de valor e registro de sangrias.
/// </summary>
public class CaixaServiceTests
{
    [Fact]
    public async Task DeveAbrirCaixa_ComValorInicial()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var servico = new CaixaService(banco.Context);

        var caixa = await servico.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 50m }, usuarioId);

        Assert.Equal("Aberto", caixa.Status);
        Assert.Equal(50m, caixa.ValorInicial);
        Assert.Equal(usuarioId, caixa.UsuarioId);
    }

    [Fact]
    public async Task DeveRejeitarSegundoCaixaAberto()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var servico = new CaixaService(banco.Context);

        await servico.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 0m }, usuarioId);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => servico.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 0m }, usuarioId));

        Assert.Contains("Já existe um caixa aberto", excecao.Message);
    }

    [Fact]
    public async Task DeveRegistrarSangria()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var servico = new CaixaService(banco.Context);

        var caixa = await servico.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 100m }, usuarioId);

        var ok = await servico.RegistrarSangriaAsync(caixa.Id, 30m, "Pagamento do fornecedor");
        Assert.True(ok);

        var atualizado = await servico.ObterPorIdAsync(caixa.Id);
        Assert.Equal(30m, atualizado!.Sangria);
    }

    [Fact]
    public async Task DeveRejeitarSangria_ComValorNaoPositivo()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var servico = new CaixaService(banco.Context);

        var caixa = await servico.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 100m }, usuarioId);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.RegistrarSangriaAsync(caixa.Id, 0m, null));
    }

    [Fact]
    public async Task DeveFecharCaixa_ApurandoValorFinal()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var servico = new CaixaService(banco.Context);
        var vendaService = new VendaService(banco.Context, servico);

        var publicacaoId = banco.CriarPublicacao(preco: 10m, estoque: 50);
        var caixa = await servico.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 20m }, usuarioId);

        await vendaService.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 2, PrecoUnitario = 10m } },
            FormaPagamento = "Dinheiro"
        }, usuarioId);

        await servico.RegistrarSangriaAsync(caixa.Id, 5m, "Despesa");

        var fechado = await servico.FecharAsync(caixa.Id, new FecharCaixaRequest { ValorConferido = 35m });

        Assert.True(fechado);

        var atualizado = await servico.ObterPorIdAsync(caixa.Id);
        Assert.Equal("Fechado", atualizado!.Status);
        // 20 (inicial) + 20 (vendas) - 5 (sangria) = 35
        Assert.Equal(35m, atualizado.ValorFinal);
        Assert.Equal(0m, atualizado.Diferenca);
        Assert.NotNull(atualizado.Fechamento);
    }
}

using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Infrastructure.Services;

namespace BancaRevistas.Tests;

/// <summary>
/// Testes das regras de venda do PDV da banca, herdadas do PDVStore:
/// exigência de caixa aberto, validação de estoque, venda fiada com
/// limite de crédito, baixa de estoque e estorno no cancelamento.
/// </summary>
public class VendaServiceTests
{
    private static VendaService CriarServico(BancoTeste banco, out CaixaService caixaService)
    {
        caixaService = new CaixaService(banco.Context);
        return new VendaService(banco.Context, caixaService);
    }

    [Fact]
    public async Task DeveRegistrarVenda_E_BaixarEstoque()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var caixaId = banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao(estoque: 10);

        var servico = CriarServico(banco, out _);

        var venda = await servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 3, PrecoUnitario = 10m } },
            FormaPagamento = "Dinheiro"
        }, usuarioId);

        Assert.Equal(30m, venda.ValorTotal);
        Assert.Equal(1, venda.Id);
        Assert.Equal(caixaId, venda.CaixaId);

        var publicacao = await banco.Context.Publicacoes.FindAsync(publicacaoId);
        Assert.Equal(7, publicacao!.Estoque);
    }

    [Fact]
    public async Task DeveRejeitarVenda_SemCaixaAberto()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        var publicacaoId = banco.CriarPublicacao();

        var servico = CriarServico(banco, out _);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 1, PrecoUnitario = 10m } },
            FormaPagamento = "Dinheiro"
        }, usuarioId));

        Assert.Contains("caixa", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeveRejeitarVenda_ComEstoqueInsuficiente()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao(estoque: 2);

        var servico = CriarServico(banco, out _);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 5, PrecoUnitario = 10m } },
            FormaPagamento = "Dinheiro"
        }, usuarioId));

        Assert.Contains("Estoque insuficiente", excecao.Message);
    }

    [Fact]
    public async Task DeveCalcularDesconto_NoValorTotal()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao(preco: 10m, estoque: 50);

        var servico = CriarServico(banco, out _);

        var venda = await servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 2, PrecoUnitario = 10m } },
            FormaPagamento = "Dinheiro",
            Desconto = 5m
        }, usuarioId);

        Assert.Equal(15m, venda.ValorTotal);
    }

    [Fact]
    public async Task DeveGerarPixTxId_QuandoPagamentoEhPix()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao();

        var servico = CriarServico(banco, out _);

        var venda = await servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 1, PrecoUnitario = 10m } },
            FormaPagamento = "Pix"
        }, usuarioId);

        Assert.False(string.IsNullOrWhiteSpace(venda.PixTxId));
    }

    [Fact]
    public async Task VendaFiada_DeveExigirCliente()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao();

        var servico = CriarServico(banco, out _);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 1, PrecoUnitario = 10m } },
            FormaPagamento = "Fiado"
        }, usuarioId));

        Assert.Contains("cliente", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VendaFiada_DeveRespeitarLimiteDeCredito()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao(preco: 50m, estoque: 100);
        var clienteId = banco.CriarCliente(limiteCredito: 100m);

        var servico = CriarServico(banco, out _);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 3, PrecoUnitario = 50m } },
            FormaPagamento = "Fiado",
            ClienteId = clienteId
        }, usuarioId));

        Assert.Contains("Limite de cr", excecao.Message);
    }

    [Fact]
    public async Task VendaFiada_DeveSomarAoSaldoDevedor()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao(preco: 10m, estoque: 50);
        var clienteId = banco.CriarCliente(limiteCredito: 500m);

        var servico = CriarServico(banco, out _);

        await servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 2, PrecoUnitario = 10m } },
            FormaPagamento = "Fiado",
            ClienteId = clienteId
        }, usuarioId);

        var cliente = await banco.Context.Clientes.FindAsync(clienteId);
        Assert.Equal(20m, cliente!.SaldoDevedor);
    }

    [Fact]
    public async Task CancelarVenda_DeveDevolverEstoqueESaldoDevedor()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);
        var publicacaoId = banco.CriarPublicacao(preco: 10m, estoque: 50);
        var clienteId = banco.CriarCliente(limiteCredito: 500m);

        var servico = CriarServico(banco, out _);

        var venda = await servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput> { new() { PublicacaoId = publicacaoId, Quantidade = 2, PrecoUnitario = 10m } },
            FormaPagamento = "Fiado",
            ClienteId = clienteId
        }, usuarioId);

        var cancelada = await servico.CancelarAsync(venda.Id, "Erro de digitação", usuarioId);
        Assert.True(cancelada);

        var publicacao = await banco.Context.Publicacoes.FindAsync(publicacaoId);
        var cliente = await banco.Context.Clientes.FindAsync(clienteId);

        Assert.Equal(50, publicacao!.Estoque);
        Assert.Equal(0m, cliente!.SaldoDevedor);

        var vendaAtual = await banco.Context.Vendas.FindAsync(venda.Id);
        Assert.Equal("Cancelada", vendaAtual!.Status);
    }

    [Fact]
    public async Task DeveRejeitarVenda_SemItens()
    {
        using var banco = new BancoTeste();
        var usuarioId = banco.CriarUsuario();
        banco.AbrirCaixa(usuarioId);

        var servico = CriarServico(banco, out _);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.RegistrarAsync(new VendaInput
        {
            Itens = new List<ItemVendaInput>(),
            FormaPagamento = "Dinheiro"
        }, usuarioId));

        Assert.Contains("pelo menos um item", excecao.Message);
    }
}

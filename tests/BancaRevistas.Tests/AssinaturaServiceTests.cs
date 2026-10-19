using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using BancaRevistas.Domain.Enums;
using BancaRevistas.Infrastructure.Services;

namespace BancaRevistas.Tests;

/// <summary>
/// Testes do serviço de assinaturas da banca: criação, cálculo da próxima
/// entrega conforme a periodicidade, mudança de status e processamento de
/// entregas vencidas.
/// </summary>
public class AssinaturaServiceTests
{
    [Fact]
    public async Task DeveCriarAssinatura_ComProximaEntregaMensal()
    {
        using var banco = new BancoTeste();
        var clienteId = banco.CriarCliente();
        var publicacaoId = banco.CriarPublicacao();
        var usuarioId = banco.CriarUsuario();

        var servico = new AssinaturaService(banco.Context);

        var assinatura = await servico.CriarAsync(new AssinaturaInput
        {
            ClienteId = clienteId,
            PublicacaoId = publicacaoId,
            ValorMensal = 12m,
            Periodicidade = "Mensal"
        }, usuarioId);

        Assert.Equal("Ativa", assinatura.Status);
        Assert.Equal(12m, assinatura.ValorMensal);
        Assert.Equal(assinatura.DataInicio.AddMonths(1), assinatura.ProximaEntrega);
    }

    [Theory]
    [InlineData("Mensal", 1)]
    [InlineData("Bimestral", 2)]
    [InlineData("Trimestral", 3)]
    [InlineData("Semestral", 6)]
    [InlineData("Anual", 12)]
    public void DeveCalcularProximaEntrega_SegundoPeriodicidade(string periodicidade, int mesesEsperados)
    {
        var baseData = new DateTime(2026, 1, 15);

        var proxima = AssinaturaService.CalcularProximaEntrega(baseData, periodicidade);

        Assert.Equal(baseData.AddMonths(mesesEsperados), proxima);
    }

    [Fact]
    public async Task DeveRejeitarAssinatura_DeClienteInativo()
    {
        using var banco = new BancoTeste();
        var clienteId = banco.CriarCliente();
        var publicacaoId = banco.CriarPublicacao();
        var usuarioId = banco.CriarUsuario();

        var cliente = await banco.Context.Clientes.FindAsync(clienteId);
        cliente!.Ativo = false;
        await banco.Context.SaveChangesAsync();

        var servico = new AssinaturaService(banco.Context);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.CriarAsync(new AssinaturaInput
        {
            ClienteId = clienteId,
            PublicacaoId = publicacaoId,
            ValorMensal = 12m
        }, usuarioId));
    }

    [Fact]
    public async Task DeveCancelarAssinatura()
    {
        using var banco = new BancoTeste();
        var clienteId = banco.CriarCliente();
        var publicacaoId = banco.CriarPublicacao();
        var usuarioId = banco.CriarUsuario();

        var servico = new AssinaturaService(banco.Context);

        var assinatura = await servico.CriarAsync(new AssinaturaInput
        {
            ClienteId = clienteId,
            PublicacaoId = publicacaoId,
            ValorMensal = 12m
        }, usuarioId);

        var cancelada = await servico.AlterarStatusAsync(assinatura.Id, StatusAssinatura.Cancelada);

        Assert.Equal("Cancelada", cancelada!.Status);
        Assert.NotNull(cancelada.DataFim);
    }

    [Fact]
    public async Task DeveProcessarEntregasVencidas_BaixandoEstoque()
    {
        using var banco = new BancoTeste();
        var clienteId = banco.CriarCliente();
        var publicacaoId = banco.CriarPublicacao(estoque: 10);
        var usuarioId = banco.CriarUsuario();

        var servico = new AssinaturaService(banco.Context);

        var assinatura = await servico.CriarAsync(new AssinaturaInput
        {
            ClienteId = clienteId,
            PublicacaoId = publicacaoId,
            ValorMensal = 12m
        }, usuarioId);

        // Empurra a entrega para o passado
        var entity = await banco.Context.Assinaturas.FindAsync(assinatura.Id);
        entity!.ProximaEntrega = DateTime.UtcNow.AddDays(-1);
        await banco.Context.SaveChangesAsync();

        var processadas = await servico.ProcessarEntregasAsync(usuarioId);
        Assert.Equal(1, processadas);

        var publicacao = await banco.Context.Publicacoes.FindAsync(publicacaoId);
        Assert.Equal(9, publicacao!.Estoque);

        var movimentacoes = await new EstoqueService(banco.Context).HistoricoAsync();
        Assert.Single(movimentacoes);
        Assert.Equal("Saida", movimentacoes[0].Tipo);
    }
}

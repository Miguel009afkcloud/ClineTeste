namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Sessão de caixa aberta por um atendente. Só existe um caixa aberto por
/// vez, e o fechamento apura o valor final conferido a partir das vendas
/// concluídas menos as sangrias.
/// </summary>
public class Caixa : IHasId
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Abertura { get; set; } = DateTime.UtcNow;
    public DateTime? Fechamento { get; set; }
    public decimal ValorInicial { get; set; }

    /// <summary>Valor apurado no fechamento (ValorInicial + vendas - sangrias).</summary>
    public decimal? ValorFinal { get; set; }

    /// <summary>Total de retiradas de dinheiro do caixa durante o turno.</summary>
    public decimal? Sangria { get; set; }

    /// <summary>Valor em dinheiro efetivamente contado no fechamento.</summary>
    public decimal? ValorConferido { get; set; }

    /// <summary>Diferença entre o valor conferido e o valor apurado.</summary>
    public decimal? Diferenca { get; set; }

    public string Status { get; set; } = nameof(Enums.StatusCaixa.Aberto);

    public Usuario? Usuario { get; set; }
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}

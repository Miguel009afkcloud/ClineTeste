namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Cliente da banca. Além do cadastro tradicional, carrega o controle de
/// crédito usado nas vendas a prazo ("fiado") e o histórico de assinaturas.
/// </summary>
public class Cliente : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    /// <summary>Limite de crédito liberado para compras a prazo. Zero = sem limite.</summary>
    public decimal LimiteCredito { get; set; }

    /// <summary>Saldo devedor atual do cliente.</summary>
    public decimal SaldoDevedor { get; set; }

    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
    public ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}

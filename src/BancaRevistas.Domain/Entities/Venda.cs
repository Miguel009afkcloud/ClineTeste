namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Venda registrada no PDV da banca. Contém o cabeçalho (total, pagamento,
/// caixa e atendente) e a coleção de itens vendidos.
/// </summary>
public class Venda : IHasId
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public string FormaPagamento { get; set; } = "Dinheiro";
    public string? PixTxId { get; set; }
    public string Status { get; set; } = nameof(Enums.StatusVenda.Concluida);
    public int CaixaId { get; set; } = 1;
    public int? ClienteId { get; set; }

    public Usuario? Usuario { get; set; }
    public Caixa? Caixa { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}

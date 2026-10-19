namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Item de uma venda: a quantidade de uma publicação e o preço cobrado.
/// O subtotal é calculado, não persistido.
/// </summary>
public class ItemVenda : IHasId
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public int PublicacaoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }

    /// <summary>Total do item (preço unitário x quantidade), descontado o item.</summary>
    public decimal Subtotal => Quantidade * PrecoUnitario - Desconto;

    /// <summary>Desconto aplicado apenas a este item.</summary>
    public decimal Desconto { get; set; }

    public Publicacao? Publicacao { get; set; }
    public Venda? Venda { get; set; }
}

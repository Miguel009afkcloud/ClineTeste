namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Item de uma compra: a quantidade recebida de uma publicação e o custo pago.
/// </summary>
public class ItemCompra : IHasId
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int PublicacaoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }

    public decimal Subtotal => Quantidade * PrecoUnitario - Desconto;

    public Publicacao? Publicacao { get; set; }
    public Compra? Compra { get; set; }
}

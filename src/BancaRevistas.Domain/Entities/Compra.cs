namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Compra de reposição de estoque realizada de um fornecedor (distribuidora).
/// As compras dão entrada às publicações e geram movimentação de estoque.
/// </summary>
public class Compra : IHasId
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime DataCompra { get; set; } = DateTime.UtcNow;
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = nameof(Enums.StatusCompra.Concluida);
    public string? Observacoes { get; set; }

    public Fornecedor? Fornecedor { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<ItemCompra> Itens { get; set; } = new List<ItemCompra>();
}

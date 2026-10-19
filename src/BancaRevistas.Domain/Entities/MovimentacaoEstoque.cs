namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Histórico de toda movimentação de estoque (entrada, saída, ajuste, perda
/// ou devolução), permitindo auditoria completa do giro de cada publicação.
/// </summary>
public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int PublicacaoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal? PrecoUnitario { get; set; }
    public DateTime DataMovimentacao { get; set; } = DateTime.UtcNow;
    public int? UsuarioId { get; set; }
    public string? Motivo { get; set; }
    public int? ReferenciaVendaId { get; set; }
    public int? ReferenciaCompraId { get; set; }

    public Publicacao? Publicacao { get; set; }
    public Usuario? Usuario { get; set; }
}

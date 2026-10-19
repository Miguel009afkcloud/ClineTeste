namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Assinatura de uma publicação para um cliente: a banca entrega a edição
/// periodicamente e o cliente paga um valor mensal. É o serviço de
/// recorrência típico de banca de revistas.
/// </summary>
public class Assinatura : IHasId
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int PublicacaoId { get; set; }
    public DateTime DataInicio { get; set; } = DateTime.UtcNow;
    public DateTime? DataFim { get; set; }
    public DateTime ProximaEntrega { get; set; } = DateTime.UtcNow;

    /// <summary>Valor cobrado por mês de assinatura.</summary>
    public decimal ValorMensal { get; set; }

    /// <summary>Periodicidade de entrega (mensal, bimestral, trimestral...).</summary>
    public string Periodicidade { get; set; } = "Mensal";

    public string Status { get; set; } = nameof(Enums.StatusAssinatura.Ativa);
    public int? UsuarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Cliente? Cliente { get; set; }
    public Publicacao? Publicacao { get; set; }
    public Usuario? Usuario { get; set; }
}

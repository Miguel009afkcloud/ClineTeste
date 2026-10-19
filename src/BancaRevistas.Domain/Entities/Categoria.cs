namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Categoria de groupings de publicações (ex.: "Revistas", "Jornais", "Quadrinhos",
/// "Papelaria"). Permite filtrar o PDV e gerar relatórios por linha de produto.
/// </summary>
public class Categoria : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<Publicacao> Publicacoes { get; set; } = new List<Publicacao>();
}

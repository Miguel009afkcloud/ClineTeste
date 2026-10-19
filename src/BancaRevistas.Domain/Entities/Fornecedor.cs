namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Distribuidora / editora que fornece as publicações e demais itens vendidos
/// na banca. É a base das compras de reposição de estoque.
/// </summary>
public class Fornecedor : IHasId
{
    public int Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Publicacao> Publicacoes { get; set; } = new List<Publicacao>();
    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
}

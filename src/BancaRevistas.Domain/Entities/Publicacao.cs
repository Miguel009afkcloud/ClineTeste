using System.ComponentModel.DataAnnotations.Schema;
using BancaRevistas.Domain.Enums;

namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Publicação (revista, jornal, quadrinho, livro) ou item de papelaria e
/// conveniência vendido na banca. Substitui o "Produto" do PDVStore com
/// atributos editoriais próprios de uma banca de revistas.
/// </summary>
public class Publicacao : IHasId
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Autor { get; set; }
    public string? CodigoBarras { get; set; }

    /// <summary>Tipo do item: define se é publicação, papelaria, doce ou bebida.</summary>
    public TipoPublicacao Tipo { get; set; } = TipoPublicacao.Revista;

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    public decimal Preco { get; set; }
    public decimal PrecoCusto { get; set; }

    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; }

    /// <summary>Edição/mês do exemplar (ex.: "Edição 42", "Março/2026").</summary>
    public string? NumeroEdicao { get; set; }
    public DateTime? DataPublicacao { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Propriedade computada de conveniência: expõe o saldo de estoque com a
    /// nomenclatura usada pelas telas de PDV e estoque. Não é mapeada como coluna.
    /// </summary>
    [NotMapped]
    public int EstoqueAtual
    {
        get => Estoque;
        set => Estoque = value;
    }

    /// <summary>True quando o estoque atingiu o nível mínimo de reposição.</summary>
    [NotMapped]
    public bool EstoqueCritico => EstoqueMinimo > 0 && Estoque <= EstoqueMinimo;

    public ICollection<ItemVenda> ItensVenda { get; set; } = new List<ItemVenda>();
    public ICollection<ItemCompra> ItensCompra { get; set; } = new List<ItemCompra>();
    public ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}

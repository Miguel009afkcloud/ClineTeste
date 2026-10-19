using BancaRevistas.Domain.Enums;

namespace BancaRevistas.Domain.DTOs;

// ==================== AUTENTICACAO ====================

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public record LoginResponse(string Token, UsuarioDto Usuario);

public record UsuarioDto(int Id, string Nome, string Email, PermissaoUsuario Permissao, bool Ativo);

public class CriarUsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public PermissaoUsuario Permissao { get; set; } = PermissaoUsuario.Operador;
}

public class AlterarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
}

public class LiquidarDividaRequest
{
    public decimal Valor { get; set; }
}

// ==================== PUBLICACOES ====================

public record PublicacaoDto(
    int Id,
    string Titulo,
    string? Subtitulo,
    string? Autor,
    string? CodigoBarras,
    TipoPublicacao Tipo,
    int? CategoriaId,
    string? Categoria,
    int? FornecedorId,
    string? Fornecedor,
    decimal Preco,
    decimal PrecoCusto,
    int Estoque,
    int EstoqueMinimo,
    string? NumeroEdicao,
    DateTime? DataPublicacao,
    bool Ativo,
    bool EstoqueCritico);

public class PublicacaoInput
{
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Autor { get; set; }
    public string? CodigoBarras { get; set; }
    public TipoPublicacao Tipo { get; set; } = TipoPublicacao.Revista;
    public int? CategoriaId { get; set; }
    public int? FornecedorId { get; set; }
    public decimal Preco { get; set; }
    public decimal PrecoCusto { get; set; }
    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public string? NumeroEdicao { get; set; }
    public DateTime? DataPublicacao { get; set; }
    public bool Ativo { get; set; } = true;
}

public record CategoriaDto(int Id, string Nome, string? Descricao, bool Ativo, int TotalPublicacoes);

public class CategoriaInput
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}

public record FornecedorDto(
    int Id,
    string RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? Telefone,
    string? Email,
    string? Endereco,
    bool Ativo,
    int TotalPublicacoes);

public class FornecedorInput
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; } = true;
}

// ==================== CLIENTES ====================

public record ClienteDto(
    int Id,
    string Nome,
    string? CpfCnpj,
    string? Telefone,
    string? Email,
    string? Endereco,
    bool Ativo,
    decimal LimiteCredito,
    decimal SaldoDevedor,
    int TotalAssinaturas);

public class ClienteInput
{
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public decimal LimiteCredito { get; set; }
    public bool Ativo { get; set; } = true;
}

// ==================== CAIXA ====================

public class AbrirCaixaRequest
{
    public decimal ValorInicial { get; set; }
}

public class FecharCaixaRequest
{
    public decimal ValorConferido { get; set; }
    public string? Observacoes { get; set; }
}

public record CaixaDto(
    int Id,
    int UsuarioId,
    string? Usuario,
    DateTime Abertura,
    DateTime? Fechamento,
    decimal ValorInicial,
    decimal? ValorFinal,
    decimal? Sangria,
    decimal? ValorConferido,
    decimal? Diferenca,
    string Status,
    decimal TotalVendas,
    int TotalVendasContagem);

public class SangriaRequest
{
    public decimal Valor { get; set; }
    public string? Motivo { get; set; }
}

// ==================== VENDAS ====================

public class ItemVendaInput
{
    public int PublicacaoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
}

public class VendaInput
{
    public ICollection<ItemVendaInput> Itens { get; set; } = new List<ItemVendaInput>();
    public string FormaPagamento { get; set; } = "Dinheiro";
    public decimal Desconto { get; set; }
    public int? ClienteId { get; set; }
}

public record ItemVendaDto(
    int Id,
    int PublicacaoId,
    string Titulo,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Subtotal);

public record VendaDto(
    int Id,
    DateTime DataVenda,
    decimal ValorTotal,
    decimal Desconto,
    string FormaPagamento,
    string? PixTxId,
    string Status,
    int CaixaId,
    int? ClienteId,
    string? Cliente,
    string? Usuario,
    ICollection<ItemVendaDto> Itens);

public class CancelarVendaRequest
{
    public string Motivo { get; set; } = string.Empty;
}

// ==================== COMPRAS ====================

public class ItemCompraInput
{
    public int PublicacaoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
}

public class CompraInput
{
    public int FornecedorId { get; set; }
    public ICollection<ItemCompraInput> Itens { get; set; } = new List<ItemCompraInput>();
    public string? Observacoes { get; set; }
}

public record CompraDto(
    int Id,
    int FornecedorId,
    string? Fornecedor,
    DateTime DataCompra,
    decimal ValorTotal,
    string Status,
    string? Observacoes,
    int TotalItens);

// ==================== ASSINATURAS ====================

public class AssinaturaInput
{
    public int ClienteId { get; set; }
    public int PublicacaoId { get; set; }
    public decimal ValorMensal { get; set; }
    public string Periodicidade { get; set; } = "Mensal";
    public DateTime? DataInicio { get; set; }
}

public record AssinaturaDto(
    int Id,
    int ClienteId,
    string? Cliente,
    int PublicacaoId,
    string? Publicacao,
    DateTime DataInicio,
    DateTime ProximaEntrega,
    decimal ValorMensal,
    string Periodicidade,
    string Status);

public class AlterarStatusAssinaturaRequest
{
    public StatusAssinatura Status { get; set; }
}

// ==================== ESTOQUE ====================

public record MovimentacaoEstoqueDto(
    int Id,
    int PublicacaoId,
    string? Publicacao,
    string Tipo,
    int Quantidade,
    decimal? PrecoUnitario,
    DateTime DataMovimentacao,
    string? Usuario,
    string? Motivo);

public class AjustarEstoqueRequest
{
    public int Quantidade { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public bool Somar { get; set; } = true;
}

public record EstoqueBaixoDto(int PublicacaoId, string Titulo, int Estoque, int EstoqueMinimo, string Tipo);

// ==================== RELATORIOS ====================

public record DashboardDto(
    decimal TotalVendasHoje,
    int VendasHoje,
    decimal TicketMedio,
    decimal TotalVendasMes,
    int VendasMes,
    decimal TotalFaturamento,
    int TotalPublicacoes,
    int PublicacoesAtivas,
    int EstoqueCritico,
    decimal SaldoDevedorTotal,
    int AssinaturasAtivas,
    decimal ReceitaAssinaturas,
    ICollection<ResumoFormaPagamento> VendasPorFormaPagamento,
    ICollection<VendaPorDia> VendasPorDia,
    ICollection<TopPublicacao> TopPublicacoes,
    ICollection<ResumoAssinatura> AssinaturasPorStatus);

public record ResumoFormaPagamento(string FormaPagamento, int Quantidade, decimal Total);

public record VendaPorDia(DateTime Dia, int Quantidade, decimal Total);

public record TopPublicacao(int PublicacaoId, string Titulo, int Quantidade, decimal Total);

public record ResumoAssinatura(string Status, int Quantidade);

public record RelatorioVendasDto(
    DateTime Inicio,
    DateTime Fim,
    int TotalVendas,
    decimal TotalFaturamento,
    decimal TicketMedio,
    decimal TotalDescontos,
    ICollection<ResumoFormaPagamento> PorFormaPagamento,
    ICollection<TopPublicacao> TopPublicacoes);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);

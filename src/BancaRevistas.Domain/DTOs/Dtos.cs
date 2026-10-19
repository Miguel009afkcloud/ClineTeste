using BancaRevistas.Domain.Enums;

namespace BancaRevistas.Domain.DTOs;

// ==================== AUTENTICACAO ====================

public record LoginRequest(string Email, string Senha);

public record LoginResponse(string Token, UsuarioDto Usuario);

public record UsuarioDto(int Id, string Nome, string Email, PermissaoUsuario Permissao, bool Ativo);

public record CriarUsuarioRequest(string Nome, string Email, string Senha, PermissaoUsuario Permissao);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

public record LiquidarDividaRequest(decimal Valor);

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

public record PublicacaoInput(
    string Titulo,
    string? Subtitulo,
    string? Autor,
    string? CodigoBarras,
    TipoPublicacao Tipo,
    int? CategoriaId,
    int? FornecedorId,
    decimal Preco,
    decimal PrecoCusto,
    int Estoque,
    int EstoqueMinimo,
    string? NumeroEdicao,
    DateTime? DataPublicacao,
    bool Ativo = true);

public record CategoriaDto(int Id, string Nome, string? Descricao, bool Ativo, int TotalPublicacoes);

public record CategoriaInput(string Nome, string? Descricao, bool Ativo = true);

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

public record FornecedorInput(
    string RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? Telefone,
    string? Email,
    string? Endereco,
    bool Ativo = true);

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

public record ClienteInput(
    string Nome,
    string? CpfCnpj,
    string? Telefone,
    string? Email,
    string? Endereco,
    decimal LimiteCredito = 0,
    bool Ativo = true);

// ==================== CAIXA ====================

public record AbrirCaixaRequest(decimal ValorInicial);

public record FecharCaixaRequest(decimal ValorConferido, string? Observacoes);

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

public record SangriaRequest(decimal Valor, string? Motivo);

// ==================== VENDAS ====================

public record ItemVendaInput(int PublicacaoId, int Quantidade, decimal PrecoUnitario, decimal Desconto = 0);

public record VendaInput(
    ICollection<ItemVendaInput> Itens,
    string FormaPagamento,
    decimal Desconto = 0,
    int? ClienteId = null);

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

public record CancelarVendaRequest(string Motivo);

// ==================== COMPRAS ====================

public record ItemCompraInput(int PublicacaoId, int Quantidade, decimal PrecoUnitario, decimal Desconto = 0);

public record CompraInput(
    int FornecedorId,
    ICollection<ItemCompraInput> Itens,
    string? Observacoes);

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

public record AssinaturaInput(
    int ClienteId,
    int PublicacaoId,
    decimal ValorMensal,
    string Periodicidade = "Mensal",
    DateTime? DataInicio = null);

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

public record AlterarStatusAssinaturaRequest(StatusAssinatura Status);

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

public record AjustarEstoqueRequest(int Quantidade, string Motivo, bool Somar = true);

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

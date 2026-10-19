namespace BancaRevistas.Domain.Enums;

/// <summary>Natureza do item comercializado pela banca.</summary>
public enum TipoPublicacao
{
    Revista = 1,
    Jornal = 2,
    Quadrinho = 3,
    Livro = 4,
    Papelaria = 5,
    Doce = 6,
    Bebida = 7,
    Outro = 8
}

/// <summary>Perfil de acesso do funcionário da banca.</summary>
public enum PermissaoUsuario
{
    /// <summary>Opera o caixa e registra vendas.</summary>
    Operador = 1,
    /// <summary>Atende clientes, assinaturas e vendas sem operação de caixa.</summary>
    Atendente = 2,
    /// <summary>Cuida do cadastro de publicações e do estoque.</summary>
    Estoquista = 3,
    /// <summary>Acesso total, incluindo relatórios e gestão de usuários.</summary>
    Administrador = 4
}

/// <summary>Situação de uma venda registrada.</summary>
public enum StatusVenda
{
    Pendente = 1,
    Concluida = 2,
    Cancelada = 3
}

/// <summary>Situação de uma sessão de caixa.</summary>
public enum StatusCaixa
{
    Aberto = 1,
    Fechado = 2
}

/// <summary>Situação de uma compra de reposição.</summary>
public enum StatusCompra
{
    Pendente = 1,
    Concluida = 2,
    Cancelada = 3
}

/// <summary>Situação de uma assinatura.</summary>
public enum StatusAssinatura
{
    Ativa = 1,
    Pausada = 2,
    Cancelada = 3,
    Expirada = 4
}

/// <summary>Tipo de movimentação registrada no histórico de estoque.</summary>
public enum TipoMovimentacao
{
    Entrada = 1,
    Saida = 2,
    Ajuste = 3,
    Devolucao = 4,
    Perda = 5
}

/// <summary>Formas de pagamento aceitas no PDV da banca.</summary>
public enum FormaPagamento
{
    Dinheiro = 1,
    Pix = 2,
    CartaoDebito = 3,
    CartaoCredito = 4,
    Fiado = 5
}

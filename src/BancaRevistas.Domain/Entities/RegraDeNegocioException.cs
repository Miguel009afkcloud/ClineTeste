namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Exceção de regra de negócio. Diferente de erros técnicos (falhas de
/// banco, LINQ não traduzido, etc.), a mensagem desta exceção é segura
/// para ser devolvida ao usuário final, pois descreve exatamente a regra
/// que foi violada.
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem)
    {
    }

    public RegraDeNegocioException(string mensagem, Exception innerException) : base(mensagem, innerException)
    {
    }
}

using BancaRevistas.Domain.Enums;
using BCrypt.Net;

namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Funcionário da banca (operador de caixa, atendente, estoquista ou
/// administrador). A senha nunca é armazenada em texto puro: somente o hash
/// BCrypt é persistido.
/// </summary>
public class Usuario : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PermissaoUsuario Permissao { get; set; } = PermissaoUsuario.Operador;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Caixa> Caixas { get; set; } = new List<Caixa>();
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();

    /// <summary>
    /// Grava o hash BCrypt da nova senha. A senha original é descartada
    /// imediatamente e nunca é persistida.
    /// </summary>
    public void SetSenha(string senha) => SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);

    /// <summary>
    /// Valida a senha informada comparando-a com o hash armazenado.
    /// Retorna true apenas se o hash conferir.
    /// </summary>
    public bool Autenticar(string senha) => BCrypt.Net.BCrypt.Verify(senha, SenhaHash);

    public bool EhAdmin() => Permissao == PermissaoUsuario.Administrador;

    public bool PodeGerenciarUsuarios() => Permissao == PermissaoUsuario.Administrador;

    /// <summary>Texto amigável em PT-BR do perfil de permissão.</summary>
    public static string DescreverPermissao(PermissaoUsuario permissao) => permissao switch
    {
        PermissaoUsuario.Operador => "Operador (Caixa)",
        PermissaoUsuario.Atendente => "Atendente",
        PermissaoUsuario.Estoquista => "Estoquista",
        PermissaoUsuario.Administrador => "Administrador",
        _ => permissao.ToString()
    };
}

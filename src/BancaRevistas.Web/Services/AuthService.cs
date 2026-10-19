using System.Net.Http.Json;
using Microsoft.JSInterop;
using BancaRevistas.Domain.DTOs;

namespace BancaRevistas.Web.Services;

/// <summary>
/// Mantém o estado de autenticação do usuário logado no front-end. O token
/// JWT é guardado no <c>sessionStorage</c> do navegador (via JS interop),
/// de modo que a sessão não se perde ao recarregar a página, e é injetado
/// automaticamente em todas as chamadas à API.
/// </summary>
public class AuthService
{
    private const string ChaveToken = "banca_token";
    private const string ChaveUsuario = "banca_usuario";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private string? _token;
    private UsuarioDto? _usuario;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    /// <summary>Usuário autenticado no momento, ou null.</summary>
    public UsuarioDto? Usuario => _usuario;

    public bool Autenticado => _usuario is not null;

    /// <summary>Token JWT atual, carregado do armazenamento do navegador sob demanda.</summary>
    public async Task<string?> ObterTokenAsync()
    {
        if (_token is not null) return _token;

        try
        {
            _token = await _js.InvokeAsync<string?>($"localStorage.getItem('{ChaveToken}')");
            var usuarioJson = await _js.InvokeAsync<string?>($"localStorage.getItem('{ChaveUsuario}')");
            if (!string.IsNullOrWhiteSpace(usuarioJson))
                _usuario = System.Text.Json.JsonSerializer.Deserialize<UsuarioDto>(usuarioJson);
        }
        catch (Exception)
        {
            // O JS interop só está disponível após a primeira renderização
            // interativa; nesse momento o usuário ainda não está logado.
            _token = null;
        }

        return _token;
    }

    /// <summary>Realiza o login e persiste o token e o perfil do usuário.</summary>
    public async Task<(bool Sucesso, string Mensagem)> LoginAsync(string email, string senha)
    {
        var resposta = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = email, Senha = senha });

        if (!resposta.IsSuccessStatusCode)
        {
            var erro = await resposta.Content.ReadAsStringAsync();
            return (false, ExtrairMensagem(erro, "E-mail ou senha inválidos."));
        }

        var login = await resposta.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Resposta inválida do servidor.");

        _token = login.Token;
        _usuario = login.Usuario;

        await _js.InvokeVoidAsync($"localStorage.setItem('{ChaveToken}', '{login.Token}')");
        await _js.InvokeVoidAsync($"localStorage.setItem('{ChaveUsuario}', '{System.Text.Json.JsonSerializer.Serialize(login.Usuario)}')");

        return (true, $"Bem-vindo(a), {login.Usuario.Nome}!");
    }

    /// <summary>Encerra a sessão, limpando o token armazenado.</summary>
    public async Task LogoutAsync()
    {
        _token = null;
        _usuario = null;
        try
        {
            await _js.InvokeVoidAsync($"localStorage.removeItem('{ChaveToken}')");
            await _js.InvokeVoidAsync($"localStorage.removeItem('{ChaveUsuario}')");
        }
        catch (Exception)
        {
            // Ignora: o logout local já foi aplicado em memória.
        }
    }

    /// <summary>Extrai a mensagem de erro devolvida pela API.</summary>
    public static string ExtrairMensagem(string corpo, string padrao)
    {
        if (string.IsNullOrWhiteSpace(corpo)) return padrao;

        try
        {
            using var documento = System.Text.Json.JsonDocument.Parse(corpo);
            if (documento.RootElement.TryGetProperty("mensagem", out var elemento))
                return elemento.GetString() ?? padrao;
        }
        catch (System.Text.Json.JsonException)
        {
            // Corpo não é JSON: usa o padrão.
        }

        return padrao;
    }
}

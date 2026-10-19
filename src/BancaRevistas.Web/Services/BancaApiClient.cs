using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BancaRevistas.Domain.DTOs;

namespace BancaRevistas.Web.Services;

/// <summary>
/// Cliente HTTP tipado da API da banca. Centraliza a injeção do token JWT
/// e a leitura das respostas padronizadas de erro ({ mensagem }),
/// devolvendo sempre um resultado consistente para as páginas.
/// </summary>
public class BancaApiClient
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public BancaApiClient(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    /// <summary>Resultado padrão das operações da API.</summary>
    public record Resposta<T>(bool Sucesso, string Mensagem, T? Dados);

    /// <summary>Prepara a requisição com o token de acesso do usuário logado.</summary>
    private async Task PrepararAsync()
    {
        var token = await _auth.ObterTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Executa um GET e devolve a lista de resultados ou o erro.</summary>
    public async Task<Resposta<List<T>>> GetListAsync<T>(string rota)
    {
        await PrepararAsync();
        try
        {
            var resposta = await _http.GetAsync(rota);
            if (!resposta.IsSuccessStatusCode)
                return new Resposta<List<T>>(false, await LerErroAsync(resposta), null);

            var dados = await resposta.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
            return new Resposta<List<T>>(true, string.Empty, dados);
        }
        catch (Exception ex)
        {
            return new Resposta<List<T>>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Executa um GET devolvendo um único objeto.</summary>
    public async Task<Resposta<T?>> GetAsync<T>(string rota) where T : class
    {
        await PrepararAsync();
        try
        {
            var resposta = await _http.GetAsync(rota);
            if (!resposta.IsSuccessStatusCode)
                return new Resposta<T?>(false, await LerErroAsync(resposta), null);

            var dados = await resposta.Content.ReadFromJsonAsync<T>();
            return new Resposta<T?>(true, string.Empty, dados);
        }
        catch (Exception ex)
        {
            return new Resposta<T?>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Executa um POST com corpo JSON, devolvendo o objeto criado.</summary>
    public async Task<Resposta<TResposta?>> PostAsync<TResposta, TCorpo>(string rota, TCorpo corpo) where TResposta : class
    {
        await PrepararAsync();
        try
        {
            var resposta = await _http.PostAsJsonAsync(rota, corpo);
            if (!resposta.IsSuccessStatusCode)
                return new Resposta<TResposta?>(false, await LerErroAsync(resposta), null);

            var dados = await resposta.Content.ReadFromJsonAsync<TResposta>();
            return new Resposta<TResposta?>(true, string.Empty, dados);
        }
        catch (Exception ex)
        {
            return new Resposta<TResposta?>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Executa um POST sem retorno de conteúdo.</summary>
    public async Task<Resposta<object?>> PostAsync(string rota, object? corpo = null)
    {
        await PrepararAsync();
        try
        {
            var resposta = corpo is null
                ? await _http.PostAsync(rota, null)
                : await _http.PostAsJsonAsync(rota, corpo);

            if (!resposta.IsSuccessStatusCode)
                return new Resposta<object?>(false, await LerErroAsync(resposta), null);

            return new Resposta<object?>(true, string.Empty, null);
        }
        catch (Exception ex)
        {
            return new Resposta<object?>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Executa um PUT com corpo JSON, devolvendo o objeto atualizado.</summary>
    public async Task<Resposta<TResposta?>> PutAsync<TResposta, TCorpo>(string rota, TCorpo corpo) where TResposta : class
    {
        await PrepararAsync();
        try
        {
            var resposta = await _http.PutAsJsonAsync(rota, corpo);
            if (!resposta.IsSuccessStatusCode)
                return new Resposta<TResposta?>(false, await LerErroAsync(resposta), null);

            var dados = await resposta.Content.ReadFromJsonAsync<TResposta>();
            return new Resposta<TResposta?>(true, string.Empty, dados);
        }
        catch (Exception ex)
        {
            return new Resposta<TResposta?>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Executa um PUT sem retorno de conteúdo.</summary>
    public async Task<Resposta<object?>> PutAsync(string rota, object? corpo = null)
    {
        await PrepararAsync();
        try
        {
            var resposta = corpo is null
                ? await _http.PutAsync(rota, null)
                : await _http.PutAsJsonAsync(rota, corpo);

            if (!resposta.IsSuccessStatusCode)
                return new Resposta<object?>(false, await LerErroAsync(resposta), null);

            return new Resposta<object?>(true, string.Empty, null);
        }
        catch (Exception ex)
        {
            return new Resposta<object?>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Executa um DELETE.</summary>
    public async Task<Resposta<object?>> DeleteAsync(string rota)
    {
        await PrepararAsync();
        try
        {
            var resposta = await _http.DeleteAsync(rota);
            if (!resposta.IsSuccessStatusCode)
                return new Resposta<object?>(false, await LerErroAsync(resposta), null);

            return new Resposta<object?>(true, string.Empty, null);
        }
        catch (Exception ex)
        {
            return new Resposta<object?>(false, $"Não foi possível falar com o servidor: {ex.Message}", null);
        }
    }

    /// <summary>Lê a mensagem de erro padronizada devolvida pela API.</summary>
    private static async Task<string> LerErroAsync(HttpResponseMessage resposta)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();

        if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            return "Sessão expirada ou não autenticado. Faça login novamente.";

        return AuthService.ExtrairMensagem(corpo, $"Erro {(int)resposta.StatusCode}: {resposta.ReasonPhrase}");
    }
}

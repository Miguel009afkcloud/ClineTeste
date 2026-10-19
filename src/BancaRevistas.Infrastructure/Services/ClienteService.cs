using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Gerencia o cadastro de clientes da banca, com controle de crédito para as
/// vendas a prazo ("fiado") e consulta de assinaturas ativas.
/// </summary>
public class ClienteService
{
    private readonly Data.BancaContext _context;

    public ClienteService(Data.BancaContext context) => _context = context;

    public async Task<List<ClienteDto>> ListarAsync(string? busca = null, bool apenasAtivos = true)
    {
        var query = _context.Clientes.AsNoTracking().AsQueryable();
        if (apenasAtivos) query = query.Where(c => c.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(c => c.Nome.Contains(termo) ||
                                     (c.CpfCnpj != null && c.CpfCnpj.Contains(termo)) ||
                                     (c.Telefone != null && c.Telefone.Contains(termo)));
        }

        return await query
            .OrderBy(c => c.Nome)
            .Select(c => new ClienteDto(
                c.Id, c.Nome, c.CpfCnpj, c.Telefone, c.Email, c.Endereco, c.Ativo,
                c.LimiteCredito, c.SaldoDevedor,
                c.Assinaturas.Count(a => a.Status == "Ativa")))
            .ToListAsync();
    }

    public async Task<ClienteDto?> ObterPorIdAsync(int id)
    {
        var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return null;

        var assinaturas = await _context.Assinaturas.CountAsync(a => a.ClienteId == id && a.Status == "Ativa");
        return new ClienteDto(cliente.Id, cliente.Nome, cliente.CpfCnpj, cliente.Telefone, cliente.Email,
            cliente.Endereco, cliente.Ativo, cliente.LimiteCredito, cliente.SaldoDevedor, assinaturas);
    }

    public async Task<ClienteDto> CriarAsync(ClienteInput input)
    {
        var cliente = new Cliente
        {
            Nome = input.Nome,
            CpfCnpj = input.CpfCnpj,
            Telefone = input.Telefone,
            Email = input.Email,
            Endereco = input.Endereco,
            LimiteCredito = input.LimiteCredito,
            Ativo = input.Ativo
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return (await ObterPorIdAsync(cliente.Id))!;
    }

    public async Task<ClienteDto?> AtualizarAsync(int id, ClienteInput input)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return null;

        cliente.Nome = input.Nome;
        cliente.CpfCnpj = input.CpfCnpj;
        cliente.Telefone = input.Telefone;
        cliente.Email = input.Email;
        cliente.Endereco = input.Endereco;
        cliente.LimiteCredito = input.LimiteCredito;
        cliente.Ativo = input.Ativo;

        await _context.SaveChangesAsync();
        return await ObterPorIdAsync(id);
    }

    /// <summary>Baixa um pagamento do saldo devedor do cliente.</summary>
    public async Task<bool> LiquidarDividaAsync(int id, decimal valor)
    {
        if (valor <= 0) throw new ArgumentException("O valor da liquidação deve ser maior que zero.", nameof(valor));

        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return false;

        cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - valor);
        await _context.SaveChangesAsync();
        return true;
    }
}

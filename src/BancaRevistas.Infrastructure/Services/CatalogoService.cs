using BancaRevistas.Domain.DTOs;
using BancaRevistas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Services;

/// <summary>
/// Cadastros de apoio da banca: categorias de publicação e fornecedores
/// (distribuidoras/editoras).
/// </summary>
public class CatalogoService
{
    private readonly Data.BancaContext _context;

    public CatalogoService(Data.BancaContext context) => _context = context;

    // ---------------- Categorias ----------------

    public async Task<List<CategoriaDto>> ListarCategoriasAsync(bool apenasAtivas = true)
    {
        var query = _context.Categorias.AsNoTracking().AsQueryable();
        if (apenasAtivas) query = query.Where(c => c.Ativo);

        return await query
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto(c.Id, c.Nome, c.Descricao, c.Ativo, c.Publicacoes.Count))
            .ToListAsync();
    }

    public async Task<CategoriaDto> CriarCategoriaAsync(CategoriaInput input)
    {
        var categoria = new Categoria { Nome = input.Nome, Descricao = input.Descricao, Ativo = input.Ativo };
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        return new CategoriaDto(categoria.Id, categoria.Nome, categoria.Descricao, categoria.Ativo, 0);
    }

    public async Task<CategoriaDto?> AtualizarCategoriaAsync(int id, CategoriaInput input)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null) return null;

        categoria.Nome = input.Nome;
        categoria.Descricao = input.Descricao;
        categoria.Ativo = input.Ativo;
        await _context.SaveChangesAsync();

        var total = await _context.Publicacoes.CountAsync(p => p.CategoriaId == id);
        return new CategoriaDto(categoria.Id, categoria.Nome, categoria.Descricao, categoria.Ativo, total);
    }

    // ---------------- Fornecedores ----------------

    public async Task<List<FornecedorDto>> ListarFornecedoresAsync(bool apenasAtivos = true)
    {
        var query = _context.Fornecedores.AsNoTracking().AsQueryable();
        if (apenasAtivos) query = query.Where(f => f.Ativo);

        return await query
            .OrderBy(f => f.RazaoSocial)
            .Select(f => new FornecedorDto(f.Id, f.RazaoSocial, f.NomeFantasia, f.Cnpj, f.Telefone,
                f.Email, f.Endereco, f.Ativo, f.Publicacoes.Count))
            .ToListAsync();
    }

    public async Task<FornecedorDto?> ObterFornecedorAsync(int id)
    {
        var f = await _context.Fornecedores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return null;
        var total = await _context.Publicacoes.CountAsync(p => p.FornecedorId == id);
        return new FornecedorDto(f.Id, f.RazaoSocial, f.NomeFantasia, f.Cnpj, f.Telefone, f.Email, f.Endereco, f.Ativo, total);
    }

    public async Task<FornecedorDto> CriarFornecedorAsync(FornecedorInput input)
    {
        var fornecedor = new Fornecedor
        {
            RazaoSocial = input.RazaoSocial,
            NomeFantasia = input.NomeFantasia,
            Cnpj = input.Cnpj,
            Telefone = input.Telefone,
            Email = input.Email,
            Endereco = input.Endereco,
            Ativo = input.Ativo
        };

        _context.Fornecedores.Add(fornecedor);
        await _context.SaveChangesAsync();
        return new FornecedorDto(fornecedor.Id, fornecedor.RazaoSocial, fornecedor.NomeFantasia, fornecedor.Cnpj,
            fornecedor.Telefone, fornecedor.Email, fornecedor.Endereco, fornecedor.Ativo, 0);
    }

    public async Task<FornecedorDto?> AtualizarFornecedorAsync(int id, FornecedorInput input)
    {
        var fornecedor = await _context.Fornecedores.FindAsync(id);
        if (fornecedor == null) return null;

        fornecedor.RazaoSocial = input.RazaoSocial;
        fornecedor.NomeFantasia = input.NomeFantasia;
        fornecedor.Cnpj = input.Cnpj;
        fornecedor.Telefone = input.Telefone;
        fornecedor.Email = input.Email;
        fornecedor.Endereco = input.Endereco;
        fornecedor.Ativo = input.Ativo;

        await _context.SaveChangesAsync();
        return await ObterFornecedorAsync(id);
    }
}

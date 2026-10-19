using BancaRevistas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BancaRevistas.Infrastructure.Data;

/// <summary>
/// Contexto do Entity Framework Core da banca de revistas. Centraliza o
/// mapeamento das entidades, os relacionamentos entre elas e as regras de
/// exclusão, além do seed inicial de dados (usuário administrador, caixa
/// padrão e catálogo de publicações de exemplo).
/// </summary>
public class BancaContext : DbContext
{
    public BancaContext(DbContextOptions<BancaContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Publicacao> Publicacoes { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Fornecedor> Fornecedores { get; set; } = null!;
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Caixa> Caixas { get; set; } = null!;
    public DbSet<Venda> Vendas { get; set; } = null!;
    public DbSet<ItemVenda> ItensVendas { get; set; } = null!;
    public DbSet<Compra> Compras { get; set; } = null!;
    public DbSet<ItemCompra> ItensCompras { get; set; } = null!;
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; } = null!;
    public DbSet<Assinatura> Assinaturas { get; set; } = null!;

    /// <summary>
    /// Mapeamento do modelo: define chaves, relacionamentos, regras de exclusão
    /// e índices. Garante que apagar um fornecedor ou uma publicação não apague
    /// o histórico de vendas, que vendas sem cliente fiquem com ClienteId nulo
    /// e que a exclusão de uma venda remove seus itens (cascata).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Publicacao>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Publicacoes)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Publicacao>()
            .HasOne(p => p.Fornecedor)
            .WithMany(f => f.Publicacoes)
            .HasForeignKey(p => p.FornecedorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Publicacao>()
            .HasIndex(p => p.CodigoBarras)
            .IsUnique(false);

        modelBuilder.Entity<Publicacao>()
            .HasIndex(p => p.Titulo);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Fornecedor>()
            .HasIndex(f => f.Cnpj)
            .IsUnique(false);

        modelBuilder.Entity<Caixa>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.Caixas)
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Caixa)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.CaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Cliente)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Usuario)
            .WithMany(u => u.Vendas)
            .HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Publicacao)
            .WithMany(p => p.ItensVenda)
            .HasForeignKey(i => i.PublicacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemVenda>()
            .HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Fornecedor)
            .WithMany(f => f.Compras)
            .HasForeignKey(c => c.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemCompra>()
            .HasOne(i => i.Publicacao)
            .WithMany(p => p.ItensCompra)
            .HasForeignKey(i => i.PublicacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemCompra>()
            .HasOne(i => i.Compra)
            .WithMany(c => c.Itens)
            .HasForeignKey(i => i.CompraId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Publicacao)
            .WithMany()
            .HasForeignKey(m => m.PublicacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Assinatura>()
            .HasOne(a => a.Cliente)
            .WithMany(c => c.Assinaturas)
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Assinatura>()
            .HasOne(a => a.Publicacao)
            .WithMany(p => p.Assinaturas)
            .HasForeignKey(a => a.PublicacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assinatura>()
            .HasOne(a => a.Usuario)
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // Propriedades decimais com precisão fixa (18,2) para valores monetários.
        modelBuilder.Entity<Publicacao>().Property(p => p.Preco).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Publicacao>().Property(p => p.PrecoCusto).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ItemVenda>().Property(i => i.PrecoUnitario).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ItemVenda>().Property(i => i.Desconto).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ItemCompra>().Property(i => i.PrecoUnitario).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ItemCompra>().Property(i => i.Desconto).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Venda>().Property(v => v.ValorTotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Venda>().Property(v => v.Desconto).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Compra>().Property(c => c.ValorTotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Caixa>().Property(c => c.ValorInicial).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Caixa>().Property(c => c.ValorFinal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Caixa>().Property(c => c.Sangria).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Caixa>().Property(c => c.ValorConferido).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Caixa>().Property(c => c.Diferenca).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Cliente>().Property(c => c.LimiteCredito).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Cliente>().Property(c => c.SaldoDevedor).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Assinatura>().Property(a => a.ValorMensal).HasColumnType("decimal(18,2)");

        // Herda a convenção de nomes de tabela/coluna do PDVStore.
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<Publicacao>().ToTable("Publicacoes");
    }
}

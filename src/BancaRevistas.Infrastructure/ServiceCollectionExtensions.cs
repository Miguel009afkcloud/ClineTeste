using BancaRevistas.Infrastructure.Data;
using BancaRevistas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BancaRevistas.Infrastructure;

/// <summary>
/// Registro centralizado dos serviços e do banco de dados da banca. Basta
/// chamar <see cref="AdicionarBanca"/> no Program.cs da API para ter todo o
/// núcleo de negócio disponível via injeção de dependências.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AdicionarBanca(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=bancarevistas.db";

        // O provedor é escolhido a partir da string de conexão: Sqlite para
        // uso local/portátil e SqlServer para a instalação do balcão.
        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<BancaContext>(options => options.UseSqlServer(connectionString));
        }
        else
        {
            services.AddDbContext<BancaContext>(options => options.UseSqlite(connectionString));
        }

        services.AddScoped<UsuarioService>();
        services.AddScoped<PublicacaoService>();
        services.AddScoped<EstoqueService>();
        services.AddScoped<VendaService>();
        services.AddScoped<CaixaService>();
        services.AddScoped<ClienteService>();
        services.AddScoped<CompraService>();
        services.AddScoped<AssinaturaService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<RelatorioService>();

        return services;
    }
}

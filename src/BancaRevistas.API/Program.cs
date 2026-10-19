using System.Text;
using BancaRevistas.API.Middleware;
using BancaRevistas.API.Services;
using BancaRevistas.Infrastructure;
using BancaRevistas.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// Configuração geral da Web API da banca de revistas
// =====================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banca de Revistas - API",
        Version = "v1",
        Description = "API de gestão da banca de revistas: PDV, estoque, clientes, assinaturas, caixa e relatórios."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe apenas o token JWT (Bearer).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(document =>
    {
        var referencia = new OpenApiSecuritySchemeReference("Bearer", document, null!);
        return new OpenApiSecurityRequirement { [referencia] = new List<string>() };
    });
});

// Núcleo de negócio (EF Core + serviços) herdado do PDVStore
builder.Services.AdicionarBanca(builder.Configuration);

// Emissor de tokens JWT
builder.Services.AddSingleton<TokenService>();

// Autenticação e autorização
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("A chave JWT não foi configurada (Jwt:Key).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// CORS liberado para o front-end Blazor em desenvolvimento
var origensPermitidas = builder.Configuration["Cors:Origens"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new[] { "http://localhost:5000", "http://localhost:5001" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd", policy => policy
        .WithOrigins(origensPermitidas)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Content-Disposition"));
});

var app = builder.Build();

// =====================================================================
// Pipeline HTTP
// =====================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Banca de Revistas API v1");
        options.DocumentTitle = "Banca de Revistas - API";
    });
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("FrontEnd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint de verificação de saúde da API
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    servico = "Banca de Revistas API",
    horario = DateTime.UtcNow
}));

// =====================================================================
// Inicialização do banco e carga inicial
// =====================================================================
using (var escopo = app.Services.CreateScope())
{
    var context = escopo.ServiceProvider.GetRequiredService<BancaContext>();
    var usaSqlServer = (builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
        .Contains("Server=", StringComparison.OrdinalIgnoreCase);

    if (usaSqlServer)
        context.Database.Migrate();
    else
        context.Database.EnsureCreated();

    await SeedData.AplicarAsync(context);
}

app.Run();

/// <summary>Marcador usado pelos testes de integração da API.</summary>
public partial class Program { }

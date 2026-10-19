# Banca de Revistas — Web API + Front-end

Sistema completo de gestão para uma **banca de revistas**, construído sobre a
base do repositório [PDVStore-1.0](https://github.com/marciodeandrade1/PDVStore-1.0).
A lógica de negócio do PDV (caixa único, baixa de estoque, venda fiada com
limite de crédito, estorno em cancelamento) foi portada e adaptada ao
domínio de banca de revistas.

**Tecnologias:** .NET 10 (Visual Studio 2026) · ASP.NET Core Web API · Blazor
Web App · Entity Framework Core · JWT Bearer · Swagger · xUnit

---

## Arquitetura da solução

```
BancaRevistas.slnx
├── src/
│   ├── BancaRevistas.Domain/         Entidades, enums e DTOs (sem dependências)
│   ├── BancaRevistas.Infrastructure/ EF Core (DbContext, seed) e serviços de negócio
│   ├── BancaRevistas.API/            Controllers REST, JWT, Swagger, middleware
│   └── BancaRevistas.Web/            Front-end Blazor Web App (11 páginas)
└── tests/
    └── BancaRevistas.Tests/          35 testes xUnit sobre as regras de negócio
```

O `Domain` não depende de nada; o `Infrastructure` depende do `Domain`; a
`API` e o `Web` consomem ambos. O front-end conversa com a API **apenas por
HTTP**, o que permite hospedar as duas partes separadamente.

---

## Como executar

### 1. Restaurar e compilar

```bash
dotnet restore BancaRevistas.slnx
dotnet build BancaRevistas.slnx
```

### 2. Subir a API (porta 5138)

```bash
dotnet run --project src/BancaRevistas.API
```

- Swagger: <http://localhost:5138/swagger>
- Health check: <http://localhost:5138/api/health>

O banco é criado automaticamente na primeira execução, já com a carga inicial
de demonstração (usuários, categorias, editoras, publicações e clientes).

### 3. Subir o front-end (porta 5090)

```bash
dotnet run --project src/BancaRevistas.Web
```

Acesse <http://localhost:5090> e entre com um dos acessos de demonstração:

| Perfil        | E-mail                     | Senha         |
|---------------|----------------------------|---------------|
| Administrador | `admin@banca.com.br`       | `admin123`    |
| Operador      | `operador@banca.com.br`    | `operador123` |

> Em produção, troque a chave JWT em `src/BancaRevistas.API/appsettings.json`
> (`Jwt:Key`) e as senhas padrão dos usuários.

### 4. Rodar os testes

```bash
dotnet test tests/BancaRevistas.Tests
```

---

## Banco de dados

O provedor é escolhido automaticamente a partir da string de conexão
(`ConnectionStrings:DefaultConnection`):

- **SQLite** (padrão): `Data Source=bancarevistas.db` — portátil, ideal para
  desenvolvimento e para o balcão em um único computador.
- **SQL Server**: qualquer conexão com `Server=` — para uso multi-terminal.

```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BancaRevistas;Trusted_Connection=True;MultipleActiveResultSets=true"
```

Com SQL Server, as migrações são aplicadas automaticamente (`Database.Migrate`);
com SQLite, o esquema é criado via `EnsureCreated`.

---

## Módulos do sistema

| Módulo          | Funcionalidades                                                                 |
|-----------------|---------------------------------------------------------------------------------|
| **PDV**         | Busca por título/código de barras, cupom, quantidades, desconto, 5 formas de pagamento, cliente para fiado |
| **Publicações** | Cadastro completo (título, autor, edição, categoria, fornecedor, código de barras, preço e custo), busca e filtro |
| **Estoque**     | Saldo, nível mínimo, alerta de reposição, ajustes manuais, perdas, histórico completo de movimentações |
| **Caixa**       | Abertura (uma sessão por vez), sangrias, fechamento com apuração e diferença entre conferido e apurado |
| **Clientes**    | Cadastro, limite de crédito, saldo devedor, liquidação de dívida |
| **Assinaturas** | Serviço de recorrência: mensal/bimestral/trimestral/semestral/anual, pausas, cancelamentos e processamento de entregas |
| **Compras**     | Reposição de estoque por distribuidora, com entrada automática e custo atualizado |
| **Fornecedores**| Editoras e distribuidoras (CNPJ, contato, publicações vinculadas) |
| **Relatórios**  | Faturamento, ticket médio, por forma de pagamento, publicações mais vendidas, por dia |
| **Usuários**    | CRUD de funcionários com 4 perfis de acesso (somente administradores) |

### Perfis de acesso

| Perfil         | Pode fazer                                                              |
|----------------|-------------------------------------------------------------------------|
| `Administrador`| Acesso total, incluindo gestão de usuários, cadastros e relatórios       |
| `Operador`     | Opera o caixa, registra e cancela vendas                                |
| `Atendente`    | Vendas, clientes, assinaturas e consultas                               |
| `Estoquista`   | Cadastro de publicações, compras, fornecedores e ajustes de estoque     |

---

## Regras de negócio herdadas do PDVStore

1. **Caixa único** — só existe uma sessão de caixa aberta por vez; uma venda
   sem caixa aberto é rejeitada.
2. **Baixa de estoque transacional** — a venda valida o estoque, grava
   cabeçalho + itens, dá baixa e registra a movimentação dentro de uma única
   transação com rollback.
3. **Venda fiada** — exige cliente ativo e respeita o limite de crédito
   (`SaldoDevedor + total ≤ LimiteCredito`), aumentando o saldo devedor.
4. **Cancelamento com estorno** — devolve os itens ao estoque, registra a
   movimentação de devolução e desconta o valor do saldo devedor do cliente.
5. **Fechamento de caixa** — `ValorInicial + vendas concluídas − sangrias`,
   comparado com o valor conferido para apurar a diferença.
6. **Senhas em hash** — BCrypt; o texto puro nunca é persistido.

---

## Endpoints principais

| Método | Rota                                  | Descrição                          |
|--------|---------------------------------------|------------------------------------|
| POST   | `/api/auth/login`                     | Autentica e devolve o token JWT     |
| GET    | `/api/auth/me`                        | Perfil do usuário autenticado       |
| GET    | `/api/publicacoes`                    | Lista o catálogo com filtros        |
| GET    | `/api/publicacoes/codigo-barras/{c}`  | Busca exata pelo código de barras   |
| GET    | `/api/publicacoes/estoque-critico`    | Itens que atingiram o estoque mínimo|
| POST   | `/api/vendas`                         | Registra uma venda no PDV           |
| POST   | `/api/vendas/{id}/cancelar`           | Cancela a venda com estorno         |
| GET    | `/api/caixa/atual`                    | Caixa aberto no momento             |
| POST   | `/api/caixa/abrir`                    | Abre uma sessão de caixa            |
| POST   | `/api/caixa/{id}/fechar`              | Fecha e apura o valor               |
| POST   | `/api/estoque/ajustar/{publicacaoId}` | Ajusta o saldo de estoque           |
| GET    | `/api/estoque/movimentacoes`          | Histórico de movimentações          |
| GET    | `/api/assinaturas`                    | Lista as assinaturas                |
| POST   | `/api/assinaturas/processar-entregas` | Processa as entregas vencidas       |
| POST   | `/api/compras`                        | Registra uma compra de reposição    |
| GET    | `/api/relatorios/dashboard`           | Indicadores do painel               |
| GET    | `/api/relatorios/vendas`              | Relatório de vendas por período     |

---

## Testes

35 testes cobrindo as regras críticas:

- `VendaServiceTests` — baixa de estoque, desconto, PIX, fiado e limite de
  crédito, estorno de estoque e de saldo devedor, rejeição de venda sem itens.
- `CaixaServiceTests` — abertura única, sangrias, apuração do fechamento.
- `EstoqueServiceTests` — ajustes, perdas, histórico, alerta de estoque crítico.
- `UsuarioServiceTests` — hash BCrypt, e-mail duplicado, troca de senha.
- `AssinaturaServiceTests` — cálculo de periodicidade, cancelamento, entregas
  vencidas com baixa de estoque.

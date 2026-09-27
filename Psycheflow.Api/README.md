# Psycheflow API

API do **Psycheflow**, ERP para clínicas de psicologia e psicólogos autônomos (TCC — Centro Universitário FAG).

- **.NET 10** · ASP.NET Core **Minimal APIs** · **EF Core 10** + **PostgreSQL 17**
- Arquitetura **Vertical Slice** com núcleo compartilhado (`Common/`) no estilo Clean
- **TDD**: todo caso de uso tem testes unitários e de integração (Postgres real via Testcontainers)
- Docker Compose para desenvolvimento · CI no GitHub Actions (`.github/workflows/api-ci.yml` na raiz do monorepo) com cobertura mínima de 80%

## Sumário

- [Como rodar](#como-rodar)
- [Arquitetura](#arquitetura)
- [Como criar um novo caso de uso (slice)](#como-criar-um-novo-caso-de-uso-slice)
- [Testes](#testes)
- [Banco de dados e migrations](#banco-de-dados-e-migrations)
- [Endpoints](#endpoints)
- [Configuração e segredos](#configuração-e-segredos)

---

## Como rodar

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e [Docker Desktop](https://www.docker.com/products/docker-desktop/).

### Tudo em containers (banco + API)

```bash
cp .env.example .env
docker compose up -d --build
```

- API: http://localhost:8080 · documentação interativa (Scalar): http://localhost:8080/scalar
- As migrations são aplicadas na subida e, em `Development`, os **dados de demonstração** são carregados.

### Banco no Docker, API pelo `dotnet run` (melhor para desenvolver)

```bash
docker compose up -d db
dotnet run --project src/Psycheflow.Api
```

- API: http://localhost:5240 · Scalar: http://localhost:5240/scalar
- `appsettings.Development.json` já aponta para o banco do compose (credenciais **somente de desenvolvimento**).
- `src/Psycheflow.Api/Psycheflow.Api.http` tem requisições de exemplo prontas.

### Logins de demonstração

Senha de todos: `Psycheflow@123` (definida em `Common/Persistence/DevData.cs`, só existe em `Development`).

| E-mail | Perfis |
|--------|--------|
| `ana@psycheflow.dev` | Admin + Psicóloga (agenda com sessões) |
| `bruno@psycheflow.dev` | Psicólogo |
| `gestao@psycheflow.dev` | Manager |
| `admin@psycheflow.dev` | Admin |

---

## Arquitetura

Um único projeto de API organizado **por funcionalidade** (vertical slices). Cada caso de uso fica numa pasta com
tudo o que precisa — request, validação, regra e endpoint — em vez de espalhado por camadas técnicas.
O que é realmente compartilhado (infraestrutura e regras transversais) fica em `Common/`.

```
src/Psycheflow.Api/
├─ Program.cs                  # só composição: AddCommon → AddPersistence → AddFeatures → MapFeatures
├─ Common/
│  ├─ Auth/                    # JWT, ICurrentUser, roles e policies (Staff, Management)
│  ├─ Domain/                  # Entity, Result/Error, ITenantEntity, ISoftDeletable, Phone
│  ├─ Endpoints/               # Error → ProblemDetails, filtro de validação, paginação
│  ├─ Errors/                  # handler global de exceções (500 sem vazar detalhes)
│  ├─ OpenApi/                 # documento OpenAPI + esquema Bearer
│  ├─ Persistence/             # AppDbContext, filtros globais, auditoria, migrations, DevData
│  ├─ Storage/                 # IFileStorage (anexos em disco/volume)
│  ├─ Time/                    # ClinicClock (TimeProvider + fuso da clínica)
│  └─ Validation/              # regras FluentValidation reutilizáveis (pt-BR)
└─ Features/
   ├─ Auth/                    # Register, Login, ChangePassword, Me
   ├─ Users/                   # CreateUser (senha temporária), ListUsers
   ├─ Companies/               # Company + CompanySettings, Get/UpdateSettings
   ├─ Psychologists/           # perfil, CRP, expediente (working hours)
   ├─ Patients/                # CRUD, CPF, endereço, filtros
   ├─ Scheduling/              # TimeSlot, Schedule, disponibilidade, bloqueios, agenda
   ├─ Sessions/                # ciclo de vida da sessão (agendar → concluir/cancelar/falta)
   ├─ Payments/                # pagamento pendente por sessão, lançar, editar, cancelar/estornar
   ├─ Recurrences/             # sessões semanais/mensais em janelas de 3 meses
   ├─ Documents/               # PDFs QuestPDF: recibo, declaração, relatórios de sessões e feedback
   ├─ PsychologicalReports/    # laudos e relatórios psicológicos (CFP 06/2019) + PDF
   ├─ MedicalRecords/          # prontuários com anexos (só o psicólogo autor)
   └─ Ai/                      # assistente de IA: Claude/OpenAI/Gemini, pseudonimização, auditoria
tests/
├─ Psycheflow.Api.UnitTests/         # domínio, value objects, validators
└─ Psycheflow.Api.IntegrationTests/  # HTTP de ponta a ponta contra Postgres (Testcontainers)
```

### Decisões principais

| Tema | Decisão |
|------|---------|
| Execução do caso de uso | Endpoint (minimal API) → **Handler** (classe simples, injetada) → `Result<T>`. Sem MediatR. |
| Erros | Regras de negócio retornam `Result`/`Error` (sem exceções) e viram **ProblemDetails** (RFC 9457): 401/403/404/409/422/423/503. Exceções = 500. |
| Validação | **FluentValidation** por request, executada por um endpoint filter → 422 com erros por campo (camelCase). |
| Domínio | Entidades com construtor privado e métodos que protegem invariantes (ex.: `Session.Complete`, `Psychologist.SetWorkingHours`). Value objects: `Cpf`, `Phone`, `LicenseNumber`, `TimeSlot`. |
| Acesso a dados | Handlers usam o `AppDbContext` diretamente (sem repositórios). Configuração EF fica no módulo da entidade. |
| Multiempresa | **Filtro global** por `CompanyId` do usuário logado + **exclusão lógica** (`DeletedAt`) em todas as entidades de negócio. Um interceptor preenche `CompanyId`, `CreatedAt`/`UpdatedAt` e converte `Remove` em soft delete. |
| Autorização | Política padrão/fallback exige usuário interno autenticado; endpoints públicos usam `AllowAnonymous()`. Usuários com senha temporária só acessam `/auth/me` e `/auth/change-password`. |
| Sigilo clínico | Admin/Manager gerenciam agendas, mas anotações e feedback das sessões só aparecem para o psicólogo da sessão. |
| Datas | Agenda em data/hora **locais da clínica** (`date` + `time`); auditoria em UTC (`timestamptz`). "Agora" vem sempre do `TimeProvider`. |
| Concorrência na agenda | Escritas na agenda de um psicólogo usam `pg_advisory_xact_lock` dentro da transação para impedir dupla marcação simultânea. |
| Nomes | Rotas `/api/v1/<recurso-kebab>`, JSON camelCase (enums como texto), banco snake_case. |
| IA | Porta `IAiTextGenerator` com um adaptador por SDK oficial (Anthropic, OpenAI, Google.GenAI). Chaves só no servidor; a clínica escolhe o provedor e os dados liberados; o texto é **pseudonimizado** antes de sair e cada uso é auditado. Nos testes, um provedor falso substitui os reais. |

---

## Como criar um novo caso de uso (slice)

Siga o fluxo **TDD**: os testes vêm antes da implementação.

1. **Teste de integração** em `tests/Psycheflow.Api.IntegrationTests/Features/<Módulo>/` cobrindo sucesso, cada erro
   (status HTTP esperado), permissão e isolamento entre empresas. Herde de `IntegrationTest` e use os helpers
   (`RegisterAccountAsync`, `CreateStaffAsync`, `TravelTo`/`RefreshAsync`, `QueryDbAsync`).
2. **Teste unitário** das regras puras (entidade, value object, validator) em `tests/Psycheflow.Api.UnitTests/`.
3. Rode e veja falhar. Implemente o mínimo:

```
Features/<Módulo>/<CasoDeUso>/
  <CasoDeUso>Request.cs      sealed record
  <CasoDeUso>Validator.cs    AbstractValidator<Request>  (registrado automaticamente)
  <CasoDeUso>Handler.cs      sealed class com Handle(...) → Result<T>  (registrado automaticamente pelo sufixo)
  <CasoDeUso>Endpoint.cs     static Map(group) → MapPost/MapGet... .WithRequestValidation<Request>()
```

4. Mapeie o endpoint no `<Módulo>Endpoints.cs` (e o módulo em `Features/FeatureSetup.cs`, se for novo).
5. Se mudou o modelo: `dotnet ef migrations add <Nome> --project src/Psycheflow.Api --output-dir Common/Persistence/Migrations`.
6. `dotnet build` (warnings são erros) e `dotnet test` verdes.

Regras: um slice **não chama outro slice**; lógica compartilhada vai para o domínio (entidade/VO) ou para um serviço
do módulo (ex.: `ScheduleAvailability`, `SessionAccess`).

---

## Testes

```bash
dotnet test                       # unitários + integração (precisa do Docker rodando)
dotnet test tests/Psycheflow.Api.UnitTests
```

- Integração: `WebApplicationFactory` + **Testcontainers** (`postgres:17-alpine`) + **Respawn** (banco limpo a cada teste).
- O relógio da API é controlado nos testes (`TestClock`): "agora" padrão é segunda-feira 05/10/2026 09:00 (São Paulo).
- Cobertura (mesmo comando do CI):

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage --settings coverlet.runsettings
dotnet tool restore
dotnet reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:coverage/report -reporttypes:Html
```

---

## Banco de dados e migrations

- Uma migration inicial (`InitialCreate`) com todo o modelo e os **dados essenciais** (roles via `HasData`).
- Dados de **demonstração** (`DevData`) são carregados pelo EF (`UseAsyncSeeding`) apenas em `Development`.
- Testes nunca dependem de dados de demonstração: cada teste cria o que precisa.

```bash
dotnet tool restore
dotnet ef migrations add <Nome> --project src/Psycheflow.Api --output-dir Common/Persistence/Migrations
dotnet ef database update --project src/Psycheflow.Api
```

---

## Endpoints

Todos sob `/api/v1`, autenticados por `Authorization: Bearer <token>` (exceto registro e login).
Documentação completa e interativa em `/scalar` (ambiente Development).

| Módulo | Rotas |
|--------|-------|
| Auth | `POST /auth/register` · `POST /auth/login` · `POST /auth/change-password` · `GET /auth/me` |
| Usuários (Admin/Manager) | `POST /users` · `GET /users` |
| Configurações | `GET /settings` · `PUT /settings` (Admin/Manager) |
| Psicólogos | `GET /psychologists` · `GET /psychologists/me` · `GET/PUT /psychologists/{id}` · `GET/PUT /psychologists/{id}/working-hours` |
| Pacientes | `POST /patients` · `GET /patients` · `GET/PUT /patients/{id}` |
| Agenda | `GET /agenda` · `POST /schedule-blocks` · `DELETE /schedule-blocks/{id}` |
| Sessões | `POST /sessions` · `GET /sessions` · `GET/PUT/DELETE /sessions/{id}` · `POST /sessions/{id}/confirm`, `/reschedule`, `/cancel`, `/complete`, `/no-show` |
| Pagamentos | `GET /payments` · `GET/PUT /payments/{id}` · `POST /payments/{id}/pay`, `/cancel` |
| Recorrência | `POST /recurrences` · `GET /recurrences` · `POST /recurrences/{id}/extend`, `/end` |
| Documentos (PDF) | `GET /documents/receipts/{paymentId}` · `/documents/attendance/{sessionId}` · `/documents/sessions-report` · `/documents/feedback-report` |
| Laudos | `POST/GET /psychological-reports` · `GET/PUT/DELETE /psychological-reports/{id}` · `POST .../{id}/finalize` · `GET .../{id}/pdf` |
| Prontuários | `POST/GET /medical-records` · `GET/PUT/DELETE /medical-records/{id}` · `POST .../{id}/attachments` · `GET/DELETE .../attachments/{attachmentId}` · `GET .../{id}/access-log` |
| IA | `GET/PUT /ai/settings` · `POST /ai/suggestions/session-notes`, `/patient-analysis`, `/next-steps` |
| Painel | `GET /dashboard` |
| Saúde | `GET /health` (fora de `/api/v1`) |

---

## Configuração e segredos

| Chave | Descrição |
|-------|-----------|
| `ConnectionStrings:Postgres` | Conexão com o PostgreSQL |
| `Jwt:Key` | Chave de assinatura (mín. 32 caracteres). **Obrigatória**: a API não sobe sem ela. |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpirationMinutes` | Emissor, audiência e validade do token (padrão 120 min) |
| `Cors:AllowedOrigins` | Origens do front permitidas |
| `Database:MigrateOnStartup` | Aplica migrations ao subir |
| `Storage:Path` | Pasta dos anexos de prontuário (no compose, volume `api-storage`) |
| `Ai:Claude:ApiKey` / `Ai:OpenAi:ApiKey` / `Ai:Gemini:ApiKey` | Chaves dos provedores de IA (opcionais; só os configurados aparecem para as clínicas). No compose: `ANTHROPIC_API_KEY`, `OPENAI_API_KEY`, `GEMINI_API_KEY` no `.env` |
| `Ai:<Provedor>:Model` | Modelo de cada provedor (padrões: `claude-opus-5`, `gpt-5`, `gemini-2.5-pro`) |
| `Ai:MaxOutputTokens` / `Ai:TimeoutSeconds` | Limite da resposta (16000) e tempo máximo por sugestão (120 s) |
| `RateLimiting:Auth` / `RateLimiting:Ai` | `PermitLimit` e `WindowSeconds` do limite de requisições (login/registro por IP: 10/min; sugestões de IA por usuário: 20/min) |

- Nada de produção é versionado. Em produção, configure por variáveis de ambiente (`Jwt__Key`, `ConnectionStrings__Postgres`, …).
- `appsettings.Development.json` e `.env.example` contêm apenas valores de desenvolvimento local.
- ⚠️ A versão anterior do projeto versionava `env.appsettings.json` com chave JWT e senhas: esses valores estão no
  histórico do git e **devem ser considerados comprometidos** — nunca os reutilize.

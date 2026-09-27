# 01 — Visão geral, arquitetura e convenções

## Produto

- **Nome:** Psycheflow (nos docs de requisitos também "ERP Psicologia").
- **Contexto:** TCC — Centro Universitário FAG, Cascavel/PR. Autores: Jhonattan, Matheus Augusto, Matheus Mantovani.
- **Usuário principal:** o psicólogo (autônomo ou dentro de uma clínica).
- **Multiempresa (multi-tenant):** tudo pertence a uma `Company`. Um psicólogo autônomo é uma company com um único psicólogo.
- **Perfis (roles):** `Admin`, `Manager` (gestão), `Psychologist`, `Patient` (reservado, sem uso — D-01).
  - Quem cria a conta vira **Admin + Psychologist** da nova empresa. Admin/Manager cadastram os demais usuários.

## Estrutura do repositório

```
psycheflow/                      # monorepo github.com/jhondev123/psycheflow_v2
├─ README.md                     # contexto do projeto
├─ CLAUDE.md                     # ponteiro para esta pasta
├─ .github/workflows/api-ci.yml  # CI da API
├─ docs/
│  ├─ *.docx, DER_psycheflow.png # documentos originais
│  └─ ai/                        # esta documentação
├─ Psycheflow.Api/               # backend — ver README.md dele (histórico importado de jhondev123/Psycheflow.Api)
│  ├─ Psycheflow.slnx, global.json, Directory.Build.props, Directory.Packages.props, .editorconfig
│  ├─ docker-compose.yml, Dockerfile, .env.example
│  ├─ src/Psycheflow.Api/        # Common/ + Features/<Módulo>/<CasoDeUso>/
│  └─ tests/                     # UnitTests + IntegrationTests (Testcontainers)
└─ Psycheflow.Front/             # frontend React
   └─ src/
      ├─ App.tsx, main.tsx
      ├─ components/ (layout/, ui/, RequireAuth.tsx)
      ├─ data/seed.ts                   # base de demonstração
      ├─ lib/                           # calendar, domain, format, scheduling, time, validation
      ├─ pages/                         # Login, Register, Dashboard, Agenda, Patients, Sessions, Documents, WorkingHours, Profile
      ├─ store/AppStore.tsx             # "banco" em memória + localStorage
      ├─ styles/                        # CSS próprio
      └─ types/index.ts                 # tipos (ainda no contrato da API antiga — ver DT-01)
```

## Stack

### Backend — `Psycheflow.Api`
| Item | Tecnologia |
|------|-----------|
| Runtime | **.NET 10 (LTS)**, ASP.NET Core **Minimal APIs** |
| ORM / banco | **EF Core 10** + Npgsql · **PostgreSQL 17** (Docker) · nomes em snake_case (`EFCore.NamingConventions`) |
| Auth | ASP.NET Identity (`User : IdentityUser<Guid>`) + JWT próprio (2h) · lockout após 5 tentativas |
| Validação | FluentValidation (endpoint filter → 422) |
| Erros | `Result<T>`/`Error` → ProblemDetails (RFC 9457) |
| Docs da API | OpenAPI nativo + **Scalar** (`/scalar`, só em Development) |
| Testes | xUnit v3 · Shouldly · Bogus · **Testcontainers** (Postgres) · Respawn · coverlet |
| CI | GitHub Actions: build (warnings = erro), migrations pendentes, testes, cobertura ≥ 80% |
| Documentos | QuestPDF (licença Community) — o FastReport foi removido |
| IA | SDKs oficiais Anthropic, OpenAI e Google.GenAI atrás de `IAiTextGenerator` |

### Frontend — `Psycheflow.Front`
| Item | Tecnologia |
|------|-----------|
| Framework | React 18 + TypeScript (strict) |
| Build | Vite 6 (alias `@` → `src/`) |
| Rotas | react-router-dom 6 |
| Datas | date-fns 4 (locale pt-BR) |
| Ícones | lucide-react |
| UI | CSS próprio, sem framework; tema claro/escuro |
| Dados | **Mock**: `AppStore` guarda tudo no `localStorage` (`psycheflow.db.v1`). **Não chama a API.** |

## Arquitetura

```
[React SPA] --(ainda mock, DT-01)--> [Minimal APIs /api/v1] → Handler → AppDbContext (EF Core) → [PostgreSQL]
```

### API: Vertical Slice + núcleo compartilhado
- **`Features/<Módulo>/<CasoDeUso>/`**: cada caso de uso é um slice com `Request`, `Validator`, `Handler` e `Endpoint`.
  Módulos: `Auth`, `Users`, `Companies`, `Psychologists`, `Patients`, `Scheduling`, `Sessions`, `Payments`, `Recurrences`,
  `Documents`, `PsychologicalReports`, `MedicalRecords`, `Ai`, `Dashboard`.
- **Entidades e value objects** ficam na raiz do módulo (`Features/Patients/Patient.cs`, `Cpf.cs`…), com construtor
  privado e métodos que protegem as regras (`Session.Complete`, `Psychologist.SetWorkingHours`…).
- **`Common/`**: só o que é transversal — `Result/Error`, ProblemDetails, filtro de validação, JWT/`ICurrentUser`/policies,
  `AppDbContext` com filtros globais e auditoria, `ClinicClock`, OpenAPI, `IFileStorage` (anexos) e limite de requisições.
- **Um slice não chama outro.** Regra compartilhada vai para o domínio ou para um serviço do módulo
  (`ScheduleAvailability`, `SessionAccess`, `AccessTokenIssuer`).
- `Program.cs` só compõe: `AddCommon()`, `AddPersistence()`, `AddFeatures()`, `MapFeatures()`.

### Regras transversais garantidas pela infraestrutura
- **Isolamento por empresa (RN-01):** filtro global nomeado (`Tenant`) em toda entidade `ITenantEntity`; o `CompanyId` é
  preenchido automaticamente na inserção. Exceção: `User` (o login precisa achá-lo) — filtrado explicitamente.
- **Exclusão lógica (RN-02):** filtro global (`SoftDelete`) + `Remove()` vira UPDATE de `deleted_at`.
- **Auditoria (RN-03):** `created_at`/`updated_at` em UTC preenchidos pelo interceptor.
- **Autorização segura por padrão:** a política de fallback exige usuário interno; rotas públicas usam `AllowAnonymous()`.

### Front
- Um único contexto React (`AppStore`) expõe o "banco" e as ações (`addPatient`, `createSchedule`, …).
- **Integração com a API: inexistente** (DT-01). A API nova mudou contratos (camelCase, enums como texto, rotas `/api/v1`).

## Como rodar (dev)

```bash
# Backend (ver Psycheflow.Api/README.md)
cd Psycheflow.Api && cp .env.example .env
docker compose up -d --build        # Postgres + API em http://localhost:8080 (Scalar em /scalar)
# ou: docker compose up -d db && dotnet run --project src/Psycheflow.Api   (http://localhost:5240)
dotnet test                          # precisa do Docker rodando
# Logins demo (Development): ana@psycheflow.dev / Psycheflow@123 (ver DevData.cs)

# Front
cd Psycheflow.Front && npm install && npm run dev  # http://localhost:5173
```

## Convenções

### C# (API)
- **C# moderno:** file-scoped namespaces, primary constructors para DI, `record` para Request/Response, `sealed` por padrão,
  `var` quando o tipo é evidente, nullable ativo. **Warnings são erros** (`TreatWarningsAsErrors` + `.editorconfig`).
- **Pacotes** com versão central em `Directory.Packages.props`.
- **Nomes:** código em inglês; mensagens ao usuário em **português**. Rotas `/api/v1/<recurso-plural-kebab>`,
  JSON **camelCase** (enums como texto), banco **snake_case**.
- **Handler** = classe `*Handler` com `Handle(...)` retornando `Result<T>` (registro automático pelo sufixo).
  **Regras de negócio não lançam exceção**: retornam `Error` (`Validation`→422, `NotFound`→404, `Conflict`→409,
  `Forbidden`→403, `Unauthorized`→401, `Locked`→423). Exceção = erro inesperado (500).
- **Validação de formato** em `*Validator` (FluentValidation) + `.WithRequestValidation<T>()` no endpoint;
  **regras de domínio** nas entidades/value objects.
- **Erros de domínio** centralizados em `<Módulo>Errors` com código estável (`patient.cpf_already_registered`).
- **Datas:** agenda em `DateOnly`/`TimeOnly` locais da clínica; auditoria em `DateTimeOffset` UTC. Nunca `DateTime.Now`:
  use `TimeProvider`/`ClinicClock`.
- **Escritas em várias tabelas** numa transação (`BeginTransactionAsync`); escritas na agenda com `LockAgendaAsync`.

### Testes (obrigatório — TDD)
- Para cada slice: **teste de integração** (HTTP real + Postgres via Testcontainers) e **teste unitário** das regras puras,
  escritos **antes** da implementação.
- Nome: `Metodo_Cenario_Resultado`. AAA. Shouldly. Um comportamento por teste.
- Todo recurso novo precisa de teste de **isolamento entre empresas** e de **permissão**.
- Se um teste falhar por bug no código, corrija o código — não o teste.

### TypeScript/React
- Componentes funcionais, um por página em `pages/`; componentes reutilizáveis em `components/ui`.
- Regras puras em `lib/` (sem React). Tipos em `types/index.ts` (atualizar para o contrato novo na integração).
- Datas como string ISO `yyyy-MM-dd`, horas como `HH:mm`. Textos de UI em pt-BR, curtos.

### Princípio geral (preferência do dev)
Projeto de TCC com padrão de mercado: arquitetura clara, testada e fácil de manter — sem cerimônia desnecessária
(sem repositórios genéricos, sem MediatR, sem camadas vazias).

## Glossário

| Termo (pt) | Código | Significado |
|------------|--------|-------------|
| Empresa / Clínica | `Company` (+ `CompanySettings`) | Tenant; tudo pertence a uma. Configurações: duração/valor padrão da sessão, fuso |
| Usuário | `User` | Login (e-mail). Pode ter perfil de psicólogo |
| Psicólogo | `Psychologist` | Perfil profissional: CRP (`LicenseNumber`), abordagem, telefone, expediente |
| Expediente / horários de trabalho | `WorkingHoursRange` (owned) | Faixas por dia da semana em que o psicólogo atende |
| Paciente | `Patient` | Pessoa atendida (CPF, contato, endereço, status) |
| Item de agenda | `Schedule` | Slot na agenda: `Session` (atendimento) ou `Block` (bloqueio) |
| Intervalo | `TimeSlot` | Data + início + fim (value object) |
| Sessão / Atendimento | `Session` | Atendimento em si; raiz do agregado que contém seu `Schedule` |
| Bloqueio | `Schedule` com `ScheduleType.Block` | Reserva de horário que impede agendamentos |
| Anotações | `Session.Notes` | Markdown; sigilosas (só o psicólogo da sessão vê) |
| Feedback | `Session.FeedbackScore` (+ `FeedbackComment`) | Nota 0–10 dada na conclusão |
| Pagamento, Recorrência, Recibo, Laudo, Prontuário | (ainda não existem) | Fases seguintes — ver `07-status-e-backlog.md` |

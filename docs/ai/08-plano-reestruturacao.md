# 08 — Plano de reestruturação da API (Clean + Vertical Slice)

> Status: **Fases 0–5 concluídas** em 27/09/2026, seguidas pelas fases 6–9 (Financeiro/Recorrência, Documentos, Prontuários e IA). O trabalho foi feito na branch `refactor/vertical-slices` do repo `Psycheflow.Api` e depois importado, com histórico, para o monorepo `jhondev123/psycheflow_v2` (pasta `Psycheflow.Api/`). Próximas fases no backlog de `07-status-e-backlog.md`.

## Progresso

| Fase | Status | Observações |
|------|--------|-------------|
| 0 — Ambiente e esqueleto | ✅ Concluída | .NET 10, CPM, `.editorconfig`, `Common/` (Result/Error → ProblemDetails, validação, exception handler, JWT, policies, tenant/soft-delete filters, auditoria, ClinicClock, OpenAPI+Scalar, health, CORS), testes unit + integração (Testcontainers/Respawn), Docker (compose db+api), CI (GitHub Actions + cobertura 80%). |
| 1 — Auth, Empresas, Usuários, Configurações | ✅ Concluída | Register (empresa+Admin/Psicólogo em transação), Login com lockout (423), ChangePassword (troca obrigatória da senha temporária), Me, CreateUser/ListUsers (Admin/Manager), GET/PUT Settings. Entidade `Psychologist` + VOs `LicenseNumber`/`Phone` já criados aqui (o registro cria o perfil). |
| 2 — Psicólogos e horários | ✅ Concluída | List/Get/Me/UpdateProfile, Get/Set working hours. Expediente como coleção owned (`psychologist_working_hours`) com regra de sobreposição e `WorksAt` (RN-33) no domínio. |
| 3 — Pacientes | ✅ Concluída | Create/List/Get/Update. VO `Cpf`, endereço owned (colunas `address_*`), CPF único por empresa (índice filtrado + tratamento de corrida), busca por nome/e-mail (ILIKE) ou CPF completo, filtro por status, paginação. Filtro por última sessão entra na Fase 4. |
| 4 — Agenda e sessões | ✅ Concluída | Agenda (`GET /agenda`), bloqueios por horário ou dias inteiros (tudo-ou-nada), sessões com ciclo de vida completo (criar, listar, detalhar, editar, confirmar, reagendar, cancelar, concluir, falta, excluir). `TimeSlot` + `ScheduleAvailability` (RN-30/33/34/38/40) com advisory lock do Postgres contra dupla marcação concorrente. Sigilo: anotações/feedback só para o psicólogo da sessão. Filtro de pacientes por última sessão. Cobertura de linhas: 92%. |
| 5 — Corte (cutover) | ✅ Concluída | Projeto antigo, `.sln`, testes vazios e `env.appsettings.json` removidos (ficam no histórico do git); migration única `InitialCreate`; README do repo da API e arquivo `.http`; docs `01`, `02`, `03`, `04`, `05`, `06`, `07` e `CLAUDE.md` atualizados. Segredos antigos continuam no histórico do git e devem ser considerados comprometidos. |
| 6 — Financeiro e recorrência | ✅ Concluída | `Payment` nasce Pendente com a sessão (D-08); pagar só sessão concluída, editar só pendente, cancelar/estornar com motivo; cancelar a sessão cancela o pagamento. `Recurrence` semanal/mensal com `RecurrencePattern` puro, janelas de 3 meses (D-05), datas indisponíveis puladas e devolvidas com o motivo, estender/encerrar. |
| 7 — Documentos (QuestPDF) | ✅ Concluída | `Features/Documents` com layout comum (clínica, psicólogo, CRP): recibo (valor por extenso), declaração de comparecimento, relatório de sessões e de feedback. Laudos/relatórios psicológicos (CFP 06/2019) com rascunho → finalizado e PDF com marca d'água no rascunho. |
| 8 — Prontuários | ✅ Concluída | Registros por paciente acessíveis só ao autor, busca por período/palavra-chave, anexos PDF/JPG/PNG ≤ 10 MB validados por assinatura e gravados via `IFileStorage` (volume, D-06). |
| 9 — IA | ✅ Concluída | Porta `IAiTextGenerator` com Claude (Anthropic, `claude-opus-5` com fallback no servidor), OpenAI e Gemini via SDKs oficiais; `ai_settings` por clínica com aceite; contexto só com dados liberados e do psicólogo logado; `Pseudonymizer`; prompt padrão pt-BR; auditoria `ai_usage_logs`; testes com provedor falso (integração) e HTTP simulado (SDKs). |

**Resultado:** 203 testes unitários + 153 de integração verdes · cobertura de linhas 93,7% · build sem warnings · `docker compose up` sobe banco + API com dados demo.

> Decisões tomadas em sessão de perguntas com o dev (grill-me). Durante a execução, decisões de arquitetura adicionais ficaram a cargo da IA (autorizado pelo dev) e estão registradas em A-29 em diante.

## 1. Decisões (ADR resumido)

| # | Tema | Decisão |
|---|------|---------|
| A-01 | Projetos | **1 projeto** `Psycheflow.Api` com `Features/<Módulo>/<CasoDeUso>/` + `Common/`. Testes em `Psycheflow.Api.UnitTests` e `Psycheflow.Api.IntegrationTests`. |
| A-02 | Execução do slice | **Minimal APIs** + classe `Handler` simples por slice, injetada no endpoint. Sem MediatR/Wolverine/FastEndpoints. |
| A-03 | Respostas | HTTP puro: sucesso devolve o DTO (200/201/204); erros em **ProblemDetails** (400/401/403/404/409/422). Handlers retornam `Result<T>`; exceção só para erro inesperado (500). `GenericResponseDto` deixa de existir. |
| A-04 | Validação | **FluentValidation** por request + endpoint filter genérico → 422. Regras de negócio (conflito, CPF duplicado) ficam no domínio/handler. |
| A-05 | Runtime | **.NET 10 LTS**, EF Core 10, Npgsql 10, OpenAPI nativo + **Scalar** (substitui Swashbuckle). |
| A-06 | Acesso a dados | Handler usa **`AppDbContext` direto** (sem repositórios). Configuração EF (`IEntityTypeConfiguration`) mora no módulo da entidade. |
| A-07 | Banco de testes | **Testcontainers.PostgreSql** + `WebApplicationFactory` + **Respawn**. Exige Docker. |
| A-08 | Migrations | **Recomeçar do zero**: apagar as 3 migrations antigas; uma `InitialCreate` limpa (migrations intermediárias da branch são consolidadas antes do merge). |
| A-09 | Estilo C# | Moderno: primary constructors, `record` para Request/Response, file-scoped namespace, `sealed` por padrão, `var` quando o tipo é óbvio, nullable + `TreatWarningsAsErrors`, `.editorconfig`, Central Package Management. |
| A-10 | Nomes | Rotas `/api/v1/<recurso-plural-kebab>`; JSON **camelCase**; banco **snake_case** (`EFCore.NamingConventions`). Código em inglês, mensagens ao usuário em pt-BR. |
| A-11 | Multiempresa | **Global query filter** automático: `ITenantEntity` (CompanyId do usuário atual) + `ISoftDeletable` (`DeletedAt == null`). Interceptor preenche `CompanyId`, `CreatedAt`, `UpdatedAt` e converte `Remove` em soft delete. `IgnoreQueryFilters()` só em login/registro/seed. |
| A-12 | Auth | ASP.NET Identity + **JWT próprio** (sem refresh token), **lockout ligado**. Registro público cria **empresa + usuário Admin/Psychologist + perfil psicólogo** numa transação. |
| A-13 | Escopo | Migrar o que existe **corrigindo as DT-xx** + completar o core (Fases 0–2 do backlog antigo: pacientes, agenda, sessões). Financeiro, documentos, prontuário, IA = fases seguintes. |
| A-14 (D-01) | Login do paciente | **Não agora, mas preparado**: `Patient` tem dados próprios (M-01), `UserId` opcional e role `Patient` mantida, sem endpoints para paciente. |
| A-15 (D-02) | Visibilidade | Pacientes: visíveis a **toda a empresa**. Agenda/sessões: `Psychologist` vê/edita **só as próprias**. Admin/Manager veem todas as agendas, **sem** o conteúdo das anotações/feedback. |
| A-16 (D-04) | Cancelamento | `SessionStatus = Scheduled(0), Completed(1), Cancelled(2), NoShow(3)` (sai `InProgress`). Cancelar seta Session=Cancelled **e** Schedule=Cancelled (libera horário) + motivo. |
| A-17 (D-03) | CRP | Formato `^\d{2}/\d{4,6}$` (ex.: `06/12345`). |
| A-18 | Datas | Agenda em horário local da clínica: `Date` (`date`), `StartTime`/`EndTime` (`time`). Auditoria em UTC (`timestamptz`). "Agora" via `TimeProvider` + fuso da empresa (`America/Sao_Paulo`) — nunca `DateTime.Now`. |
| A-19 | Criação na agenda | Sessão: `date + startTime + durationMinutes?` (padrão da config, mín. 15). Bloqueio em endpoint próprio: por horário **ou** por dias inteiros, com motivo, **ignora expediente** (RN-40). Sessão exige expediente e ausência de conflito. |
| A-20 | Estratégia | **Projeto novo ao lado do antigo**, numa branch, módulo a módulo, testes verdes a cada módulo; projeto antigo apagado no final. |
| A-21 | Testes | **xUnit v3 + Shouldly + Bogus + NSubstitute (só se preciso)**; TDD por slice (testes antes do código); cobertura mínima **80%** (coverlet) no CI. |
| A-22 | Documentos | **Remover tudo de FastReport** (pacotes, `.frx`, `Documents`/`DocumentFields`, conexão SqlServer, `ExportFormatEnum`). Documentos futuros serão classes **QuestPDF** fixas em `Features/Documents` (fase própria). |
| A-23 | Docker | `docker-compose.yml` com **postgres:17** (healthcheck + volume) **e a API** (Dockerfile multi-stage; aplica migrations + seed de roles ao subir). Segredos em `.env` (fora do git) / user-secrets; `.env.example` versionado. |
| A-24 | Configurações | Tabela tipada **`company_settings`** (1:1 com empresa): `session_duration_minutes` (50, ≥15), `session_default_price`, `time_zone`. `Config`/`ConfigAi` removidas (IA terá tabela própria na fase de IA). |
| A-25 | Usuários da empresa | Admin/Manager criam usuário → **senha temporária** devolvida uma única vez + `MustChangePassword`; enquanto não trocar, só `POST /auth/change-password` é permitido. |
| A-26 | CI | **GitHub Actions** desde a Fase 0: build (warnings = erro), testes unit + integração, gate de cobertura 80%. |
| A-27 | Payment | Entidade `Payment` atual **não é portada**; volta redesenhada (M-05) na fase Financeiro. |
| A-29 | Decisões da execução | `Result<T>.Success(...)` explícito quando `T` é interface; `ICurrentUser` via claims (`sub`, `company_id`, `psychologist_id`); política de fallback = Staff (seguro por padrão); `User` sem filtro automático de tenant (login); expediente como coleção owned; endereço como owned na tabela `patients`; `Session` é raiz do agregado e cria seu `Schedule`; `ScheduleAvailability` com `pg_advisory_xact_lock` contra dupla marcação; bloqueios tudo-ou-nada; concluir/falta só após o início da sessão; sessão concluída não pode ser excluída; notas nulas no PUT mantêm as atuais; validação de token com o `TimeProvider` da aplicação; ProblemDetails com `code` estável e `traceId`. |
| A-28 | Dados iniciais | **Fim do esquema de seeders** (`Seeders/`, `ISeeder`, `DatabaseSeeder`, flag `ENV`). Dados **essenciais** (roles) via `HasData` → entram na `InitialCreate` e existem em qualquer ambiente. Dados de **demonstração** (empresa, psicólogos, pacientes, horários, sessões) via `UseAsyncSeeding` do EF Core em `Common/Persistence/DevData.cs`: idempotente e executado **só em Development** (inclui o compose). Testes de integração **não** usam dados demo: cada teste cria o que precisa (builders + Bogus); Respawn limpa tudo exceto `__EFMigrationsHistory` e a tabela de roles. |
| A-30 | Financeiro | Um `Payment` por sessão (índice único filtrado), criado Pendente junto com a sessão (`SessionAccess.AddWithPayment`); valor: informado → recorrência → padrão da empresa. Ciclo no domínio (`Pay`, `ChangeAmount`, `Cancel`). |
| A-31 | Recorrência | Cálculo de datas puro (`RecurrencePattern`, testável sem banco) + `RecurrenceGenerator` que reaproveita as regras de agenda (`ScheduleAvailability`) e **pula** datas indisponíveis em vez de falhar tudo. |
| A-32 | Documentos | Um documento QuestPDF por slice, modelo montado no handler e renderizado por uma classe `IDocument`; resposta `application/pdf` com nome de arquivo. Licença QuestPDF Community (projeto acadêmico). |
| A-33 | Arquivos | `IFileStorage` em `Common/Storage` com implementação local (`Storage:Path`, proteção contra path traversal); volume `api-storage` no compose. |
| A-34 | IA | Porta `IAiTextGenerator` (Ports & Adapters) com um adaptador por SDK oficial; provedor disponível = chave configurada no servidor; `AiAssistant` concentra autorização, pseudonimização, timeout, recusa/falha (`ErrorType.Unavailable` → 503) e auditoria; SDKs recebem `HttpClient` do `IHttpClientFactory` (testáveis com handler simulado). |
| A-35 | Endurecimento | Limite de requisições com o middleware nativo (`AddRateLimiter`, janela fixa configurável, 429 em ProblemDetails); trilha de acesso aos prontuários gravada pelo `MedicalRecordAccess` (leitura, download e negação); painel calculado no banco com consultas agregadas (sem cache). |

Decisões D-05 a D-09 confirmadas com o dev em 27/09/2026 (ver `07`). Notificações ficaram para depois (D-11).

## 2. Estrutura alvo

```
Psycheflow.Api/                         # raiz do repositório git
├─ Psycheflow.slnx
├─ global.json                          # fixa SDK 10.0.x
├─ Directory.Build.props                # net10.0, nullable, TreatWarningsAsErrors, analyzers
├─ Directory.Packages.props             # Central Package Management
├─ .editorconfig
├─ docker-compose.yml  Dockerfile  .env.example  .dockerignore
├─ .github/workflows/ci.yml
├─ src/Psycheflow.Api/
│  ├─ Program.cs                        # só composição: AddCommon(), AddFeatures(), MapFeatures()
│  ├─ Common/
│  │  ├─ Auth/            CurrentUser, ICurrentUser, JwtOptions, TokenService, Roles, Policies
│  │  ├─ Domain/          Entity, ITenantEntity, ISoftDeletable, Result, Result<T>, Error (Validation/NotFound/Conflict/Forbidden)
│  │  ├─ Endpoints/       ValidationFilter<T>, ResultExtensions.ToHttpResult(), EndpointGroups
│  │  ├─ Errors/          GlobalExceptionHandler (IExceptionHandler → ProblemDetails 500)
│  │  ├─ Persistence/     AppDbContext (roles via HasData), AuditAndTenantInterceptor, Migrations/, DevData.cs (UseAsyncSeeding, só Development)
│  │  └─ Time/            ClinicClock (TimeProvider + fuso da empresa)
│  └─ Features/
│     ├─ Auth/            Register/, Login/, ChangePassword/, Me/
│     ├─ Companies/       Company.cs, CompanySettings.cs, GetSettings/, UpdateSettings/
│     ├─ Users/           User.cs, CreateUser/, ListUsers/
│     ├─ Psychologists/   Psychologist.cs, LicenseNumber.cs, WorkingHours.cs,
│     │                   ListPsychologists/, GetPsychologist/, GetMyProfile/, UpdateProfile/,
│     │                   GetWorkingHours/, SetWorkingHours/
│     ├─ Patients/        Patient.cs, Cpf.cs, Phone.cs, Address.cs (complex type), PatientStatus.cs,
│     │                   CreatePatient/, ListPatients/, GetPatient/, UpdatePatient/
│     ├─ Scheduling/      Schedule.cs, ScheduleType.cs, ScheduleStatus.cs,
│     │                   ScheduleAvailability.cs (regras compartilhadas: expediente, conflito, passado),
│     │                   CreateBlock/, DeleteBlock/, GetAgenda/
│     └─ Sessions/        Session.cs, SessionStatus.cs,
│                         CreateSession/, ListSessions/, GetSession/, UpdateSession/, ConfirmSession/,
│                         RescheduleSession/, CancelSession/, CompleteSession/, MarkNoShow/, DeleteSession/
└─ tests/
   ├─ Psycheflow.Api.UnitTests/         # espelha Features/: domínio, VOs, validators, ScheduleAvailability (lógica pura)
   └─ Psycheflow.Api.IntegrationTests/
      ├─ Infrastructure/  ApiFactory (Testcontainers + Respawn), AuthHelper (token por role/empresa), TestData (Bogus)
      └─ Features/<Módulo>/<Slice>Tests.cs
```

### Anatomia de um slice

```
Features/Patients/CreatePatient/
  CreatePatientRequest.cs     public sealed record CreatePatientRequest(...)
  CreatePatientResponse.cs    (ou reusa PatientResponse do módulo)
  CreatePatientValidator.cs   AbstractValidator<CreatePatientRequest>
  CreatePatientHandler.cs     public sealed class CreatePatientHandler(AppDbContext db, ICurrentUser user)
                              Task<Result<PatientResponse>> Handle(CreatePatientRequest, CancellationToken)
  CreatePatientEndpoint.cs    static Map(RouteGroupBuilder g) → g.MapPost("/", ...).WithRequestValidation<...>()
```

Regras de organização:
- Um slice **não chama** handler de outro slice. Lógica compartilhada vai para o domínio do módulo (entidade/VO) ou um serviço do módulo (ex.: `ScheduleAvailability`).
- Cada módulo tem `<Módulo>Endpoints.cs` que cria o `RouteGroup` (`/api/v1/patients`, tag OpenAPI, policy) e chama o `Map` de cada slice. `Program.cs` só chama `app.MapFeatures()`.
- Handlers e validators registrados por varredura do assembly (reflection simples em `AddFeatures()`, sem Scrutor).
- Entidades com fábrica/métodos que validam invariantes e retornam `Result` (ex.: `Schedule.Create(...)`, `session.Cancel(reason)`), sem setters públicos para estado.

## 3. Fluxo obrigatório por slice (TDD)

1. **Especificar** em teste de integração os cenários HTTP (sucesso + cada erro com status esperado + isolamento de empresa/permissão).
2. **Especificar** em teste unitário as regras puras (entidade/VO/validator/`ScheduleAvailability`).
3. Rodar → **vermelho** (compila com stubs mínimos).
4. Implementar o mínimo até **verde**.
5. Refatorar; `dotnet format` + build sem warnings.
6. Atualizar `02`/`04`/`06`/`07` quando o requisito/regra mudar de status.

**Definition of done do módulo:** todos os slices com testes unit + integração verdes no CI, cobertura ≥ 80%, teste "empresa A não enxerga dados da empresa B" para cada recurso, OpenAPI com exemplos, docs atualizados.

## 4. Fases de execução

### Fase 0 — Ambiente e esqueleto
Pré-requisitos na máquina do dev: **.NET 10 SDK** e **Docker Desktop** (hoje nenhum dos dois está no PATH).

1. Branch `refactor/vertical-slices` no repo `Psycheflow.Api`.
2. Criar `src/` e `tests/` ao lado do projeto antigo; `Psycheflow.slnx` com os 3 projetos novos (antigo fica fora da solução nova).
3. `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`.
4. Pacotes: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `FluentValidation`(+DI), `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, `AspNetCore.HealthChecks.NpgSql`. Testes: `xunit.v3`, `Shouldly`, `Bogus`, `Testcontainers.PostgreSql`, `Respawn`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet.collector`.
5. `Common/`: `Result`/`Error` → ProblemDetails, `ValidationFilter`, `GlobalExceptionHandler`, `ClinicClock`, OpenAPI + Scalar (dev), CORS `http://localhost:5173` (dev), `GET /health` (com check do banco), JSON camelCase + enums como string.
6. `AppDbContext` (Identity) com snake_case, `ApplyConfigurationsFromAssembly`, query filters por convenção, interceptor de auditoria/tenant/soft delete.
7. `docker-compose.yml` (postgres:17 + api), `Dockerfile`, `.env.example`; API aplica migrations ao subir (roles vêm da `InitialCreate`); `DevData` (`UseAsyncSeeding`) só em Development. Credenciais demo documentadas em `DevData.cs`/README do repo da API (nunca senhas reais). Cada fase seguinte acrescenta ao `DevData` os dados do seu módulo.
8. `ApiFactory` de integração + `AuthHelper`.
9. `.github/workflows/ci.yml`.

Testes primeiro desta fase:
- `GET /health` → 200 com banco acessível.
- Endpoint de teste que lança exceção → 500 ProblemDetails sem vazar stack.
- Request inválido num endpoint de teste → 422 com `errors` por campo (camelCase).
- Unit: `Result`/`Error` → status HTTP corretos; `ClinicClock` usa o fuso configurado.
- Integração: interceptor seta `CreatedAt`/`UpdatedAt` em UTC e soft delete oculta registro; query filter isola empresas (entidade de teste).
- Integração: banco migrado do zero contém as 4 roles; `DevData` roda duas vezes sem duplicar dados; em ambiente `Testing`/`Production` o `DevData` não roda.

### Fase 1 — Auth, Empresas, Usuários, Configurações
| Slice | Rota | Acesso | Testes-chave |
|-------|------|--------|--------------|
| Register | `POST /api/v1/auth/register` | público | cria company + settings padrão + user (Admin+Psychologist) + psicólogo numa transação; e-mail duplicado → 409; CRP inválido → 422; senha fraca → 422; retorna token |
| Login | `POST /api/v1/auth/login` | público | credencial inválida → 401 genérico; 5 falhas → lockout (423/401); `MustChangePassword` → token restrito |
| ChangePassword | `POST /api/v1/auth/change-password` | autenticado | senha atual errada → 422; limpa `MustChangePassword` |
| Me | `GET /api/v1/auth/me` | autenticado | dados do usuário + roles + `psychologistId` |
| CreateUser | `POST /api/v1/users` | Admin, Manager | cria na empresa do solicitante (ignora qualquer companyId); Manager não cria Admin → 403; senha temporária retornada 1x |
| ListUsers | `GET /api/v1/users` | Admin, Manager | só usuários da empresa |
| Get/UpdateSettings | `GET/PUT /api/v1/settings` | leitura: todos; escrita: Admin, Manager | duração < 15 → 422 |

Modelo: `User` (+`FullName`, `MustChangePassword`; sai `BirthDate`), `Company`, `CompanySettings`. Corrige: DT-02, DT-09 (roles na migration, sem seeder), DT-12, DT-15, DT-16, DT-20, RN-12, bug de rollback do register e da `ConfigKey`.

### Fase 2 — Psicólogos e horários
| Slice | Rota | Acesso | Testes-chave |
|-------|------|--------|--------------|
| ListPsychologists | `GET /api/v1/psychologists` | todos | só da empresa |
| GetPsychologist | `GET /api/v1/psychologists/{id}` | todos | outra empresa → 404 |
| GetMyProfile | `GET /api/v1/psychologists/me` | Psychologist | usuário sem perfil → 404 |
| UpdateProfile | `PUT /api/v1/psychologists/{id}` | o próprio, Admin, Manager | nome, telefone, CRP (`06/12345`), abordagem; psicólogo editando outro → 403 |
| GetWorkingHours | `GET /api/v1/psychologists/{id}/working-hours` | todos | |
| SetWorkingHours | `PUT /api/v1/psychologists/{id}/working-hours` | o próprio, Admin, Manager | substitui tudo; faixas sobrepostas no mesmo dia → 422; início ≥ fim → 422 |

Corrige: DT-13, DT-19, RN-68, verificação de empresa do psicólogo.

### Fase 3 — Pacientes (RF001–RF003)
| Slice | Rota | Testes-chave |
|-------|------|--------------|
| CreatePatient | `POST /api/v1/patients` | obrigatórios (nome, CPF, telefone, e-mail) → 422; CPF inválido → 422; CPF duplicado na empresa → 409 (mesmo CPF em outra empresa → 201); nasce `Active` |
| ListPatients | `GET /api/v1/patients?search=&status=&lastSessionFrom=&lastSessionTo=&page=&pageSize=` | filtros; paginação; excluídos/outra empresa não aparecem |
| GetPatient | `GET /api/v1/patients/{id}` | outra empresa → 404 |
| UpdatePatient | `PUT /api/v1/patients/{id}` | CPF não editável (campo ausente no request); `UpdatedAt` atualizado |

Modelo (M-01): `Patient` com `Name`, `Cpf` (VO, só dígitos), `Email`, `Phone` (VO), `BirthDate` (`date`), `Status`, `Address` (complex type: zipCode, street, number, complement, neighborhood, city, state), `Notes`, `UserId?`. Índice único `(company_id, cpf) WHERE deleted_at IS NULL`. CEP → endereço continua no front (ViaCEP).

### Fase 4 — Agenda e sessões (RF004–RF009, RF017, RF018)
`ScheduleAvailability` (unit-testado exaustivamente): dentro do expediente (intervalo cabe inteiro numa faixa), sem sobreposição com agendamento **não cancelado e não excluído** do mesmo psicólogo (ignorando o próprio no reagendamento), não no passado (data **e** hora, via `ClinicClock`), `End > Start`, duração ≥ 15.

| Slice | Rota | Testes-chave |
|-------|------|--------------|
| GetAgenda | `GET /api/v1/agenda?psychologistId=&from=&to=` | sessões + bloqueios + expediente no período; psicólogo só a própria; Admin/Manager sem anotações |
| CreateBlock | `POST /api/v1/schedule-blocks` | por horário ou por dias (1 bloqueio/dia 00:00–23:59); conflito com sessão → 409; fora do expediente permitido |
| DeleteBlock | `DELETE /api/v1/schedule-blocks/{id}` | soft delete libera horário |
| CreateSession | `POST /api/v1/sessions` | duração padrão da config; paciente de outra empresa → 404; conflito → 409; fora do expediente → 422; passado → 422 |
| ListSessions | `GET /api/v1/sessions?from=&to=&patientId=&status=&psychologistId=` | `from` obrigatório; visibilidade A-15 |
| GetSession | `GET /api/v1/sessions/{id}` | Admin/Manager recebem sem `notes`/`feedback` |
| UpdateSession | `PUT /api/v1/sessions/{id}` | anotações (Markdown) e paciente; sessão concluída/cancelada → 409 |
| ConfirmSession | `POST /api/v1/sessions/{id}/confirm` | Schedule → Confirmed |
| RescheduleSession | `POST /api/v1/sessions/{id}/reschedule` | data, hora, duração?, **motivo** obrigatório; reaplica disponibilidade ignorando o próprio slot |
| CancelSession | `POST /api/v1/sessions/{id}/cancel` | **motivo** obrigatório; Session e Schedule → Cancelled; horário liberado |
| CompleteSession | `POST /api/v1/sessions/{id}/complete` | anotações obrigatórias + `feedbackScore` 0–10 (+ comentário opcional); já concluída/cancelada → 409 |
| MarkNoShow | `POST /api/v1/sessions/{id}/no-show` | só sessão agendada cuja hora já passou |
| DeleteSession | `DELETE /api/v1/sessions/{id}` | soft delete de Session **e** Schedule |

Modelo: `Schedule` (`Date`, `StartTime`, `EndTime`, `Type`, `Status`, `BlockReason?`), `Session` (`Notes`, `FeedbackScore?` CHECK 0–10, `FeedbackComment?`, `Status`, `CancellationReason?`, `RescheduleReason?`). Corrige: DT-04, DT-06, DT-08, DT-10, DT-11, RN-30..RN-47.

Endurecimento opcional (se sobrar tempo): exclusion constraint no Postgres para impedir sobreposição em corrida concorrente.

### Fase 5 — Corte (cutover)
1. Consolidar migrations em `InitialCreate`.
2. Apagar o projeto antigo (`Psycheflow.Api/Psycheflow.Api`, `Psycheflow.Api.Tests`, `Psycheflow.Api.sln`, `env.appsettings.json`) e o `docker-compose.yml` antigo.
3. **Trocar a JWT key e a senha do banco** (as atuais estão no histórico do git — DT-03).
4. Reescrever `01-visao-geral.md` (arquitetura/convenções), `05-modelo-de-dados.md`, `06-api.md`; atualizar status em `02`, `03`, `04`, `07`; atualizar `CLAUDE.md`.
5. PR `refactor/vertical-slices` → `main` com CI verde.

### Fases seguintes (6–9 concluídas; ordem original mantida para histórico)
6. **Financeiro + recorrência** (RF010–RF013; M-05, M-06; decidir D-05, D-08).
7. **Documentos com QuestPDF** (RF014–RF016): `Features/Documents/` com `Common/PdfLayout` (cabeçalho com clínica, psicólogo, CRP) e um slice por documento — `SessionReceipt` (`GET /sessions/{id}/receipt`), `SessionsReport`, `FeedbackReport`, `PsychologicalReport` (entidade de laudo M-08). Testes: PDF gerado não vazio + dados do documento montados corretamente (unit no "model" do documento).
8. **Prontuários** (RF019–RF020; D-06).
9. **IA** (RF021; D-07).
10. **Notificações** (RF024–RF026).
11. **Integração do front** com a API (DT-01) — pode começar em paralelo a partir do fim da Fase 3.

## 5. Mapa API antiga → nova

| Antigo | Novo |
|--------|------|
| `POST /Company` + `POST /User/register` | `POST /api/v1/auth/register` |
| `POST /User/login` | `POST /api/v1/auth/login` |
| `POST /Admin/create/user` | `POST /api/v1/users` |
| `GET /Psychologist/{id}` | `GET /api/v1/psychologists/{id}` + `GET /api/v1/agenda` |
| `POST /Psychologist/working/hours` | `PUT /api/v1/psychologists/{id}/working-hours` |
| `GET /Psychologist/{id}/unavaliable/hours` | `GET /api/v1/agenda?from&to` |
| `POST /Schedule` (type 0/1) | `POST /api/v1/sessions` / `POST /api/v1/schedule-blocks` |
| `GET /Schedule/{id}` | removido (coberto por agenda/sessão) |
| `GET /Session/{id}` | `GET /api/v1/sessions/{id}` |
| `POST /Session/complete` | `POST /api/v1/sessions/{id}/complete` |
| `PUT /Session/{id}` | `PUT /api/v1/sessions/{id}` |
| `DELETE /Session/{id}` | `DELETE /api/v1/sessions/{id}` |
| `/Document*` (FastReport) | removido; volta na fase Documentos (QuestPDF) |
| `GET /Api/healthcheck` | `GET /health` |

## 6. Riscos e cuidados

| Risco | Mitigação |
|-------|-----------|
| Docker/.NET 10 ausentes na máquina | Passo 0 da Fase 0; CI roda independentemente |
| Query filter com tenant depende do usuário atual (DbContext scoped) | `ICurrentUser` resolvido por request; seed/login usam `IgnoreQueryFilters()`; teste de integração cobrindo cada recurso |
| Front mock usa tipos/enums antigos (`SessionStatus` com InProgress, JSON snake_case) | Registrar no plano de integração do front (DT-01); `types/index.ts` muda quando o front for integrado |
| Segredos no histórico do git | Rotacionar na Fase 5 |
| Escopo grande para o prazo do TCC | Cada fase é mergeável/entregável sozinha; priorizar até a Fase 4 |

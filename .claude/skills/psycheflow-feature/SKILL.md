---
name: psycheflow-feature
description: Implementa uma funcionalidade no Psycheflow (API .NET 10 em Vertical Slices + front React) seguindo TDD, as convenções do projeto e a atualização dos docs. Use ao criar ou alterar caso de uso, endpoint, entidade ou tela.
---

# Implementar feature no Psycheflow

## 1. Entender antes de codar
1. Leia `openspec/README.md` e localize o item (RF-xxx, UC-xx, RN-xx, DT-xx, RC-xx) em `openspec/projeto/rastreabilidade.md`, que aponta a capacidade.
2. Leia a spec da capacidade (`openspec/specs/<capacidade>/spec.md`): requisitos e cenários são o contrato de comportamento.
3. Se depender de uma decisão `D-xx` ainda aberta em `openspec/projeto/status-e-backlog.md`, **pare e pergunte** ao dev.
4. Mudança de comportamento? Ela deve existir como mudança do OpenSpec (`openspec/changes/<nome>/`, criada com `/opsx:propose`); implemente seguindo o `tasks.md` dela.
5. Plano curto: testes (a partir dos cenários) → domínio/migration → slice → front → docs.

## 2. API (`Psycheflow.Api/src/Psycheflow.Api`)
- **TDD obrigatório:** escreva primeiro o teste de integração (`tests/Psycheflow.Api.IntegrationTests/Features/<Módulo>/`) e os unitários de domínio/validator. Veja a skill `psycheflow-tests`.
- **Slice:** `Features/<Módulo>/<CasoDeUso>/` com `XxxRequest` (record), `XxxValidator` (FluentValidation, mensagens pt-BR), `XxxHandler` (classe `sealed`, injeta `AppDbContext`/`ICurrentUser`/serviços do módulo, retorna `Result<T>`) e `XxxEndpoint` (`Map(IEndpointRouteBuilder)` com `WithName`, `WithSummary`, `WithRequestValidation<T>()`). Handlers terminados em `Handler` são registrados por convenção.
- **Sem** MediatR, repositórios, AutoMapper ou controllers. Um slice não chama outro: regra compartilhada vai para o domínio ou para um serviço do módulo (ex.: `ScheduleAvailability`, `SessionAccess`), registrado em `FeatureSetup`.
- **Domínio:** entidades com construtor privado e métodos que protegem invariantes; value objects (`Cpf`, `Phone`, `TimeSlot`…); erros como `static readonly Error` em `<Módulo>Errors` com `code` estável (`modulo.motivo`).
- **Erros:** `Result`/`Error` → ProblemDetails (`ToProblem()`): Validation 422, NotFound 404, Conflict 409, Forbidden 403, Unauthorized 401, Locked 423, Unavailable 503. Exceção só para bug (500).
- **Multiempresa:** entidades de negócio implementam `ITenantEntity` e `ISoftDeletable`; os filtros globais do EF cuidam de `CompanyId` e `DeletedAt` — não use `IgnoreQueryFilters` sem motivo.
- **Autorização:** política padrão = Staff; use `.RequireAuthorization(Policies.Management)` para Admin/Manager. Sigilo clínico: anotações, prontuários e laudos só para o psicólogo autor/da sessão.
- **Datas:** agenda em `DateOnly`/`TimeOnly` locais da clínica; "agora" sempre via `ClinicClock`/`TimeProvider`.
- **C#:** tipos explícitos (use `var` só com `new` óbvio ou tipo anônimo), primary constructors, collection expressions; warnings = erro.
- **Banco:** mudou o modelo → regenere a migration e confira; nomes em snake_case.
- Rode `dotnet build` e `dotnet test` (Docker ligado).

## 3. Front (`Psycheflow.Front`)
- Chamadas HTTP em `src/lib/api.ts` (Bearer + ProblemDetails → mensagens por campo); tipos em `src/types/` espelhando o contrato da API (camelCase, enums como texto).
- Reaproveite `PageHeader`, `Modal`, `EmptyState`, `Toasts` e as classes CSS existentes; sem framework de UI. Textos curtos em pt-BR.
- Rode `npm run typecheck` e `npm run build`.

## 4. Fechar
1. Em `openspec/projeto/`: status (✅/🟡/❌) em `rastreabilidade.md` e backlog em `status-e-backlog.md`; endpoint novo em `api.md`; tabela nova em `modelo-de-dados.md`; decisão de arquitetura em `decisoes-de-arquitetura.md`. Marque as tarefas do `tasks.md` e arquive a mudança com `/opsx:archive` (aplica o delta em `openspec/specs/`); rode `openspec validate --all --strict`.
2. Commit com mensagem em pt-BR no padrão `feat(api): …` / `feat(front): …`.
3. Resuma ao dev: o que foi feito, como testar (request do `.http` ou passo na tela) e o que ficou pendente.

---
name: psycheflow-tests
description: Escreve testes unitários e de integração da API Psycheflow (xUnit v3, Shouldly, Testcontainers/Postgres, Respawn) antes da implementação. Use ao criar/alterar regra de negócio, endpoint ou ao investigar bug.
---

# Testes da API Psycheflow

Projetos: `Psycheflow.Api/tests/Psycheflow.Api.UnitTests` e `Psycheflow.Api/tests/Psycheflow.Api.IntegrationTests`.

## Princípios
- **Testes antes do código** (TDD). Cobrir as regras `RN-xx` de `docs/ai/04-regras-de-negocio.md`, não o framework.
- Um teste = um comportamento. Nome `Metodo_Cenario_ResultadoEsperado` (ex.: `Create_SlotAlreadyTaken_Returns409`). Arrange / Act / Assert.
- Asserções com **Shouldly**; dados com **Bogus** quando o valor não importa.
- Se um teste falhar por bug no código, não "conserte" o teste: diga qual RN está violada e corrija o código.

## Unitários
- Domínio puro: entidades, value objects (`Cpf`, `Phone`, `TimeSlot`, `LicenseNumber`), cálculos (`RecurrencePattern`, `AmountInWords`), validators, `Pseudonymizer`/prompts da IA.
- Integrações externas (SDKs de IA) com `HttpClient` + handler simulado (`StubHttpHandler`).

## Integração (HTTP de ponta a ponta)
- Herde de `IntegrationTest` (ou `SchedulingTest` para agenda/sessões). `ApiFactory` sobe a API real contra Postgres do Testcontainers (`postgres:17-alpine`), aplica as migrations e limpa o banco antes de cada teste (Respawn).
- Crie os dados pela própria API: `RegisterAccountAsync`, `CreateStaffAsync(admin, Roles.X)`, `CreatePracticeAsync`, `CreatePatientAsync`, `CreateSessionAsync`, `SetDefaultPriceAsync`…
- Relógio controlado: "agora" = segunda 05/10/2026 09:00 (São Paulo). Use `TravelTo(data, hora)` + `RefreshAsync(conta)` (o token expira em 2h).
- Leia respostas com `ReadAsync<T>` / `ReadProblemAsync` e confira `code` do ProblemDetails e os campos de erro (camelCase).
- IA: `Factory.Ai` (`FakeAiTextGenerator`) substitui os provedores; verifique os prompts recebidos e simule `Fail`/`Refuse`.
- Sempre cubra: sucesso, validação (422), recurso de outra empresa (404), permissão (403) e regra de negócio (409/422).
- Consulta direta ao banco: `QueryDbAsync(db => db.X.IgnoreQueryFilters()...)` (sem usuário logado os filtros de empresa escondem tudo).

## Rodar
```bash
cd Psycheflow.Api
dotnet test                                   # precisa do Docker ligado
dotnet test --filter "FullyQualifiedName~Sessions"
```
Cobertura mínima no CI: 80% de linhas (`coverlet.runsettings`).

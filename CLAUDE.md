# Psycheflow

ERP para clínicas de psicologia (TCC – FAG). Backend **.NET 10 + Minimal APIs (Vertical Slice) + EF Core 10/PostgreSQL** em `Psycheflow.Api/` (ver `Psycheflow.Api/README.md`); frontend React + TS + Vite em `Psycheflow.Front/`, integrado à API por `src/lib/api.ts`.

O projeto segue **Spec-Driven Development com OpenSpec**. **Antes de qualquer tarefa, leia `openspec/README.md`** e:
- as specs da capacidade envolvida em `openspec/specs/<capacidade>/spec.md` (requisitos + cenários, com origem RF/UC/RN);
- em `openspec/projeto/`: visão geral e convenções (`visao-geral.md`), modelo de dados, API, decisões de arquitetura (`decisoes-de-arquitetura.md`), rastreabilidade e status/backlog (`status-e-backlog.md`).

## Regras de trabalho
- **Mudança de comportamento passa pelo OpenSpec:** `/opsx:propose` (proposal, design, tasks e delta das specs) → `/opsx:apply` → `/opsx:archive`. Não edite `openspec/specs/` direto; a spec muda ao arquivar a mudança. Valide com `openspec validate --all --strict`.
- Siga a arquitetura e as convenções de `openspec/projeto/visao-geral.md`: slices em `Features/<Módulo>/<CasoDeUso>/` (Request, Validator, Handler, Endpoint), `Result<T>`/`Error` → ProblemDetails, FluentValidation, C# moderno, mensagens em pt-BR, warnings = erro.
- **TDD obrigatório:** escreva os testes de integração (Testcontainers) e unitários antes da implementação — os cenários das specs são a base dos testes; `dotnet test` verde antes de concluir.
- Isolamento por empresa (`CompanyId`) e exclusão lógica (`DeletedAt`) são filtros globais do EF — não contorne sem motivo.
- Ao concluir algo, atualize `openspec/projeto/rastreabilidade.md` e `openspec/projeto/status-e-backlog.md` (e `api.md`/`modelo-de-dados.md` se mudaram).
- Skills do projeto em `.claude/skills/` (`psycheflow-feature`, `psycheflow-tests`, `psycheflow-frontend`) descrevem o fluxo atual; prefira-as às cópias antigas do plugin. As skills `openspec-*` e os comandos `/opsx:*` foram gerados pelo `openspec init` (atualize com `openspec update`).
- Decisões marcadas como `D-xx` em `openspec/projeto/status-e-backlog.md` devem ser confirmadas com o dev antes de implementar.

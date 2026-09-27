# Psycheflow

ERP para clínicas de psicologia (TCC – FAG). Backend **.NET 10 + Minimal APIs (Vertical Slice) + EF Core 10/PostgreSQL** em `Psycheflow.Api/` (ver `Psycheflow.Api/README.md`); frontend React + TS + Vite em `Psycheflow.Front/` (hoje em modo mock, sem chamar a API).

**Antes de qualquer tarefa, leia `docs/ai/README.md`** e os arquivos que ele indica:
requisitos (`02`), casos de uso (`03`), regras de negócio (`04`), modelo de dados (`05`), API (`06`), status/backlog (`07`) e decisões de arquitetura (`08`).

## Regras de trabalho
- Siga a arquitetura e as convenções de `docs/ai/01-visao-geral.md`: slices em `Features/<Módulo>/<CasoDeUso>/` (Request, Validator, Handler, Endpoint), `Result<T>`/`Error` → ProblemDetails, FluentValidation, C# moderno, mensagens em pt-BR, warnings = erro.
- **TDD obrigatório:** escreva os testes de integração (Testcontainers) e unitários antes da implementação; `dotnet test` verde antes de concluir.
- Isolamento por empresa (`CompanyId`) e exclusão lógica (`DeletedAt`) são filtros globais do EF — não contorne sem motivo.
- Ao concluir algo, atualize o status em `docs/ai/02-requisitos.md` e `docs/ai/07-status-e-backlog.md`.
- Skills do projeto em `.claude/skills/` (`psycheflow-feature`, `psycheflow-tests`) descrevem o fluxo atual; prefira-as às cópias antigas do plugin.
- Decisões marcadas como `D-xx` em `07-status-e-backlog.md` devem ser confirmadas com o dev antes de implementar.

# Psycheflow — Documentação para IA

> Pasta pensada para ser lida por assistentes de IA (Claude, Copilot etc.) antes de mexer no código.
> Consolida os documentos originais (`docs/*.docx`, `docs/DER_psycheflow.png`) com o que **de fato existe no código** (atualizado em 27/09/2026, após a reestruturação da API).

## O que é o Psycheflow

TCC (FAG – Cascavel/PR) de um "ERP" para clínicas de psicologia e psicólogos autônomos: pacientes, agenda, registro de sessões, pagamentos, documentos (recibos, laudos, relatórios), prontuários e, opcionalmente, sugestões por IA.

## Ordem de leitura

| # | Arquivo | Para quê |
|---|---------|----------|
| 1 | [01-visao-geral.md](01-visao-geral.md) | Stack, arquitetura, estrutura de pastas, convenções de código |
| 2 | [02-requisitos.md](02-requisitos.md) | Requisitos funcionais, não funcionais e de domínio **com status** |
| 3 | [03-casos-de-uso.md](03-casos-de-uso.md) | Fluxos de cada caso de uso **com status** |
| 4 | [04-regras-de-negocio.md](04-regras-de-negocio.md) | Regras/validações atômicas (RN-xx) referenciadas pelos outros docs |
| 5 | [05-modelo-de-dados.md](05-modelo-de-dados.md) | Entidades atuais, enums, relacionamentos e o que falta no banco |
| 6 | [06-api.md](06-api.md) | Endpoints existentes (contratos) e endpoints que faltam |
| 7 | [07-status-e-backlog.md](07-status-e-backlog.md) | Matriz feito × faltando, bugs/dívidas técnicas e backlog priorizado |
| 8 | [08-plano-reestruturacao.md](08-plano-reestruturacao.md) | Plano e **registro de decisões (ADR)** da reestruturação da API para Vertical Slice + Minimal APIs + .NET 10 + TDD (Fases 0–5 concluídas). |

## Legenda de status (usada em todos os arquivos)

| Símbolo | Significado |
|---------|-------------|
| ✅ | Implementado e funcional para o escopo do requisito |
| 🟡 | Parcial — existe algo, mas falta parte do requisito ou tem bug |
| 🧪 | Só no front como **mock** (localStorage), sem API |
| ❌ | Não iniciado |

Status é sempre dado **por camada**: `API` (Psycheflow.Api) e `Front` (Psycheflow.Front).

## Fontes consolidadas

| Fonte | Conteúdo | Observação |
|-------|----------|------------|
| `Documento requisitos Atualizado.docx` | RF001–RF023 + notificações, RNF001–004, RD001–002 | **Fonte oficial dos requisitos.** IDs preservados. |
| `Documento casos de uso Atualizado.docx` | UC 1–24 | Fonte oficial dos casos de uso. |
| `Requisitos-Psycheflow.docx` | RF01–RF29 "Pronto" | Documento do trabalho de front (mock). Descreve o que o **front** faz; várias afirmações "Pronto" são só mock. Usado como evidência, não como requisito. |
| `Modelo-de-Dados-Psycheflow.docx` + `DER_psycheflow.png` | Tabelas, relacionamentos, enums | Bate com as migrations atuais. |
| Código `Psycheflow.Api` e `Psycheflow.Front` | Implementação real | Prevalece sobre os documentos quando descreve o "estado atual". |

## Regras para a IA que for trabalhar no projeto

1. **Requisito é o que está em `02-requisitos.md`**; o código é o estado atual. Se divergirem, o requisito manda — a não ser que o dev diga o contrário.
2. Siga a arquitetura e as convenções de `01-visao-geral.md` (Vertical Slice, `Result`/ProblemDetails, FluentValidation). Sem abstrações antecipadas (sem repositórios genéricos, sem MediatR).
3. Siga as convenções já usadas no código (ver `01-visao-geral.md#convenções`).
4. Isolamento por empresa e exclusão lógica são garantidos por filtros globais do EF — não os desligue (`IgnoreQueryFilters`) sem motivo explícito.
5. **TDD:** todo caso de uso novo tem teste de integração e unitário escritos antes da implementação.
6. Ao concluir um item, atualize o status nos arquivos desta pasta.

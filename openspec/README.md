# Psycheflow — Especificação (SDD com OpenSpec)

> Pasta lida por pessoas e por assistentes de IA antes de mexer no código. O projeto segue **Spec-Driven Development**
> com o [OpenSpec](https://github.com/Fission-AI/OpenSpec): a especificação vem antes do código, toda mudança nasce como
> proposta com delta de spec e, ao ser concluída, é arquivada e incorporada às specs vigentes.
> Substitui a antiga pasta `docs/` (documentos `.docx` do TCC + `docs/ai/`), migrada em 12/10/2026.

## O que é o Psycheflow

TCC (Centro Universitário FAG – Cascavel/PR) de um "ERP" para clínicas de psicologia e psicólogos autônomos: pacientes,
agenda, sessões, pagamentos, documentos (recibos, relatórios, laudos), prontuários e sugestões por IA.

## Estrutura

```
openspec/
├─ config.yaml        # contexto do projeto e regras injetados nos artefatos gerados pelos comandos /opsx
├─ specs/             # FONTE DA VERDADE: o que o sistema faz hoje, uma pasta por capacidade
│  └─ <capacidade>/spec.md   # requisitos (SHALL/MUST) + cenários (WHEN/THEN), com a origem RF/UC/RN
├─ changes/           # mudanças propostas (proposal, design, tasks, delta de specs) e archive/ das concluídas
└─ projeto/           # documentação de apoio, que não é spec
   ├─ visao-geral.md               # produto, stack, arquitetura, estrutura de pastas, convenções, glossário
   ├─ modelo-de-dados.md           # tabelas, enums, relacionamentos (+ der-psycheflow.png)
   ├─ api.md                       # contratos HTTP: rotas, acesso, formatos, códigos de erro
   ├─ decisoes-de-arquitetura.md   # ADR (A-xx) e histórico da reestruturação da API
   ├─ rastreabilidade.md           # RF/RC/RNF/RD/UC/RN → capacidade, com status por camada
   ├─ status-e-backlog.md          # dívidas técnicas, decisões do dev (D-xx) e backlog
   └─ fontes/                      # os .docx originais do TCC convertidos para Markdown (registro histórico)
```

## Capacidades (`specs/`)

| Capacidade | Cobre | IDs principais |
|------------|-------|----------------|
| [autenticacao](specs/autenticacao/spec.md) | Criação de conta, login, bloqueio por tentativas, senha temporária, logout | RF022, RF023, RC-01, RN-10–15 |
| [usuarios](specs/usuarios/spec.md) | Perfis de acesso e equipe da clínica | RC-02, D-01 |
| [configuracoes-da-clinica](specs/configuracoes-da-clinica/spec.md) | Duração e valor padrão da sessão, fuso horário | RN-32, RN-51 |
| [psicologos](specs/psicologos/spec.md) | Perfil profissional, CRP e expediente semanal | RC-03, RC-04, RN-14, RN-68 |
| [pacientes](specs/pacientes/spec.md) | Cadastro, edição, inativação e busca de pacientes | RF001–RF003, RN-20–27 |
| [agenda](specs/agenda/spec.md) | Visão da agenda, bloqueios e regras de disponibilidade | RF017, RF018, RN-30–40 |
| [sessoes](specs/sessoes/spec.md) | Ciclo de vida da sessão e sigilo das anotações | RF004–RF009, RN-41–47 |
| [recorrencia](specs/recorrencia/spec.md) | Séries semanais/mensais de sessões | RF010, RN-48 |
| [financeiro](specs/financeiro/spec.md) | Pagamentos: pendente, lançamento, edição, estorno, consulta | RF011–RF013, RN-50–56 |
| [documentos](specs/documentos/spec.md) | Recibo, declaração e relatórios em PDF | RF014, RF015, RF015-B |
| [laudos](specs/laudos/spec.md) | Laudos e relatórios psicológicos (CFP 06/2019) | RF016, RN-62 |
| [prontuarios](specs/prontuarios/spec.md) | Registros, anexos, busca e trilha de acesso | RF019, RF020, RD001 |
| [assistente-ia](specs/assistente-ia/spec.md) | Sugestões por IA com consentimento e pseudonimização | RF021, RN-67 |
| [painel](specs/painel/spec.md) | Resumo do dia, da semana e financeiro | RC-06 |
| [plataforma](specs/plataforma/spec.md) | Regras transversais: empresa, exclusão lógica, erros, paginação, saúde | RN-01–04, RNF001, RNF002, RC-08 |
| [interface-web](specs/interface-web/spec.md) | Comportamento comum do front: idioma, tema, rotas, erros | RNF003, RC-07 |

Fora das specs (ainda não implementado): notificações RF024–RF026 (D-11) e disponibilidade/backup RNF004 (deploy) —
ver [`projeto/status-e-backlog.md`](projeto/status-e-backlog.md).

## Fluxo de trabalho (SDD)

1. **Explorar** (opcional): `/opsx:explore` para pensar o problema com base nas specs.
2. **Propor**: `/opsx:propose <o que fazer>` cria `changes/<nome>/` com `proposal.md`, `design.md`, `tasks.md` e o
   **delta** das specs afetadas (`## ADDED / MODIFIED / REMOVED Requirements`).
3. **Implementar**: `/opsx:apply` segue o `tasks.md` com TDD (testes antes do código) e as skills do projeto
   (`.claude/skills/psycheflow-*`).
4. **Arquivar**: `/opsx:archive` move a mudança para `changes/archive/` e aplica o delta em `specs/`.
   Atualize também `projeto/rastreabilidade.md` e `projeto/status-e-backlog.md`.

Comandos úteis da CLI (`npm install -g @fission-ai/openspec`):

```bash
openspec list --specs                 # capacidades e nº de requisitos
openspec show pacientes --type spec   # requisitos de uma capacidade
openspec validate --all --strict      # valida specs e mudanças
```

## Convenções das specs

- Texto em pt-BR; títulos estruturais (`## Purpose`, `## Requirements`, `### Requirement:`, `#### Scenario:`) e as
  palavras SHALL/MUST/MAY ficam em inglês, como pede o OpenSpec.
- Cada requisito tem a linha `**Origem**:` com os IDs que o originaram (RF, RC, UC, RN, D, A, DT).
- Cenários citam o status HTTP e o `code` de erro estável da API quando houver — servem de base para os testes.
- A spec descreve comportamento observável; detalhes de implementação ficam em `projeto/` e no `design.md` das mudanças.

## Regras para quem for trabalhar no projeto

1. **Requisito é o que está em `specs/`**; o código é o estado atual. Se divergirem, a spec manda — a não ser que o dev
   diga o contrário. Mudou comportamento? Faça por uma mudança em `changes/`, não editando a spec direto.
2. Siga a arquitetura e as convenções de [`projeto/visao-geral.md`](projeto/visao-geral.md) (Vertical Slice,
   `Result`/ProblemDetails, FluentValidation). Sem abstrações antecipadas (sem repositórios genéricos, sem MediatR).
3. Isolamento por empresa e exclusão lógica são garantidos por filtros globais do EF — não os desligue
   (`IgnoreQueryFilters`) sem motivo explícito.
4. **TDD:** todo caso de uso novo tem teste de integração e unitário escritos antes da implementação.
5. Decisões `D-xx` em aberto em `projeto/status-e-backlog.md` devem ser confirmadas com o dev antes de implementar.

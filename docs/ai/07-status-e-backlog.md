# 07 — Status, dívidas técnicas e backlog

Retrato em 27/09/2026, após a reestruturação da API (`08-plano-reestruturacao.md`). Legenda em `README.md`.

## Resumo

- **Backend:** reescrito em .NET 10 com Vertical Slices + Minimal APIs, testado (127 testes unitários + 94 de integração
  contra Postgres real, **92% de cobertura de linhas**). Cobre o **core** completo: conta/login, usuários, configurações,
  psicólogos e expediente, **pacientes**, **agenda**, **bloqueios** e todo o **ciclo de vida das sessões**.
  Ainda não existem: financeiro, recorrência, documentos (recibo/relatórios/laudo), prontuários e IA.
- **Frontend:** UI bem acabada, mas **100% mock em localStorage** — não fala com a API. Os tipos do front ainda seguem o
  contrato **antigo** (enums numéricos, `SessionStatus.InProgress`, rotas sem `/api/v1`).
- **Maior gap agora:** integrar o front com a API (DT-01). Depois: Financeiro → Documentos → Prontuários → IA.

### Cobertura dos requisitos oficiais (RF001–RF026, 27 itens)

| Situação | API | Ponta a ponta (API + Front integrados) |
|---|---|---|
| ✅ Completo | 13 — RF001–RF009, RF017, RF018, RF022, RF023 (RF004/RF007 sem a parte de pagamento, que é do Financeiro) | 0 (front não integrado) |
| ❌ Nada | 14 — RF010–RF016, RF015-B, RF019–RF021, RF024–RF026 | — |

## Matriz por módulo

| Módulo | API | Front | O que falta (resumo) |
|--------|-----|-------|----------------------|
| Conta / login / usuários | ✅ | 🧪 | Front usar JWT e o registro novo (cria empresa) |
| Configurações da empresa | ✅ | ❌ | Tela de configurações (duração/valor padrão, fuso) |
| Perfil do psicólogo + expediente | ✅ | 🧪 | Integrar; front aceitar várias faixas por dia |
| Pacientes | ✅ | 🧪🟡 | Integrar; endereço com CEP (ViaCEP), status, CPF somente leitura na edição |
| Agenda e bloqueios | ✅ | 🧪✅ | Integrar (`GET /agenda`); bloqueio por dias inteiros e motivo na tela |
| Sessões | ✅ | 🧪🟡 | Integrar; reagendar/cancelar com motivo, nota 0–10, Markdown, duração padrão |
| Financeiro + recorrência | ❌ | ❌ | Tudo (M-05, M-06; D-05, D-08) |
| Documentos (recibo, relatórios, laudo) | ❌ | 🧪🟡 | QuestPDF + telas (M-08) |
| Prontuários | ❌ | ❌ | Tudo (M-07; D-06) |
| IA | ❌ | ❌ | Tudo (M-09; D-07) |
| Notificações | ❌ | ❌ | Desejável |
| Testes | ✅ | ❌ | Front: Vitest nas funções de `lib/` |

## Divergências entre documentos e código

| # | Documento diz | Código | Tratamento |
|---|---------------|--------|------------|
| V-01 | `Requisitos-Psycheflow.docx`: RF01–RF29 "Pronto" | É verdade **só no front mock** | Considerar como 🧪 |
| V-02 | Idem RF25/RF26 "Criar modelos de documento com campos" pronto | Modelos de documento foram removidos da API | Documentos voltam como QuestPDF (fase Documentos) |
| V-03 | Idem RF01 "Criar conta (empresa + responsável)" | API ✅ cria empresa; front mock não | Resolver na integração |
| V-04 | Requisito: feedback = nota 0–10 | ✅ `feedback_score` 0–10 (+ comentário opcional) | Resolvido |
| V-05 | Status de sessão Agendada/Concluída/Cancelada | ✅ `SessionStatus = Scheduled, Completed, Cancelled, NoShow` | Resolvido (D-04) |
| V-06 | "CPF validado na tela, sem coluna" | ✅ coluna `cpf` com VO | Resolvido (M-01) |
| V-07 | UC4: pagamento pendente criado com a sessão; UC11: pagamento lançado após conclusão | Sem pagamentos ainda | D-08 (fase Financeiro) |

## Dívidas técnicas e bugs

### Abertas
| ID | Severidade | Onde | Problema | Correção sugerida |
|----|-----------|------|----------|-------------------|
| DT-01 | Alta | Front inteiro | Não consome a API; tipos no contrato antigo | `src/lib/api.ts` (fetch + token + ProblemDetails), atualizar `types/index.ts` para o contrato novo (camelCase, enums texto), trocar as ações do `AppStore` uma a uma |
| DT-22 | Média | Deploy | Sem ambiente publicado (RNF004); segredos de produção por variável de ambiente | Publicar a imagem Docker + Postgres gerenciado com backup |
| DT-23 | Baixa | API | Sem rate limiting no login (há lockout por conta) | `AddRateLimiter` por IP em `/auth/*` |
| DT-24 | Baixa | LGPD | Sem trilha de auditoria de acesso a dados clínicos nem criptografia em repouso | Avaliar na fase Prontuários (RD001) |
| DT-25 | Baixa | Skills do Claude (`psycheflow-feature`, `psycheflow-tests`) | Descrevem as convenções antigas (controllers, `GenericResponseDto`, SQLite) | Atualizar para as convenções de `01-visao-geral.md` |

### Resolvidas na reestruturação (27/09/2026)
DT-02 (endpoints públicos/registro inseguro) · DT-03 (segredos versionados — valores antigos continuam no histórico e devem ser
considerados comprometidos) · DT-04 (filtro por empresa) · DT-05 (FastReport/SQL Server removidos) · DT-06 (soft delete global e
exclusão de sessão libera o horário) · DT-07 (`updated_at`) · DT-08 (edição de sessão) · DT-09 (roles na migration + CORS) ·
DT-10 (validação de intervalo e hora passada) · DT-11 (bloqueio fora do expediente) · DT-12 (abordagem e nome do usuário) ·
DT-13 (CRP) · DT-14 (ordem de campos — motor removido) · DT-15 (senha temporária com troca obrigatória) · DT-16 (401/403 em
ProblemDetails) · DT-17 (`Payment` removido até a fase Financeiro) · DT-18 (pacotes sem uso) · DT-19 (perfil sem carregar a agenda
inteira; agenda paginada por período) · DT-20 (claims do token) · DT-21 (testes).
Bugs encontrados na análise e eliminados: soft delete gravando `DateTime.Now` em `timestamptz` (500), seeder engolindo exceção,
`ConfigService` comparando `ConfigKey.ToString()`, rollback sem transação no registro, API sem `appsettings` num clone limpo,
agendamento sem checar se o psicólogo existe/é da empresa.

## Decisões

| ID | Pergunta | Situação |
|----|----------|----------|
| D-01 | O paciente vai ter login no sistema? | ✅ Não agora, mas preparado (`patients.user_id` opcional, role Patient reservada). |
| D-02 | Quem vê o quê numa clínica com vários psicólogos? | ✅ Pacientes: toda a empresa. Agenda/sessões: o próprio psicólogo; Admin/Manager veem todas, **sem** anotações/feedback. |
| D-03 | Formato do CRP? | ✅ `^\d{2}/\d{4,6}$` (ex.: `06/12345`). |
| D-04 | "Cancelada" vira valor de `SessionStatus`? | ✅ `Scheduled, Completed, Cancelled, NoShow`, setado junto com o Schedule. |
| D-10 | Estrutura da API | ✅ Vertical Slice + Minimal APIs + .NET 10 + TDD (`08`). |
| D-05 | Recorrência gera sessões até quando? | ⏳ Sugestão: 3 meses à frente; botão "estender" depois. |
| D-06 | Onde guardar anexos do prontuário? | ⏳ Sugestão: volume/pasta configurável (`Storage:Path`) fora do wwwroot; S3-compatível no deploy. |
| D-07 | Qual provedor de IA? | ⏳ Sugestão: um só, via HTTP, com consentimento por tipo de dado. |
| D-08 | Pagamento nasce pendente ao criar a sessão (UC4) e é "lançado"/pago depois (UC11)? | ⏳ Sugestão: sim. |
| D-09 | O front mantém o modo mock (exigência do Projeto Web) ou é integrado? | ⏳ Sugestão: integrar mantendo o mock atrás de `VITE_USE_MOCK` só se o professor exigir. |

## Backlog priorizado

> Fases 0–5 (reestruturação + core) concluídas — ver `08-plano-reestruturacao.md`.

### Fase 6 — Integração do front (DT-01, D-09)
- [ ] `lib/api.ts` (base URL por env, Bearer, tratamento de ProblemDetails → mensagens por campo)
- [ ] Tipos novos em `types/index.ts`; login/registro reais; troca de senha obrigatória
- [ ] Pacientes, Agenda, Sessões, Horários e Perfil via API; tela de Configurações
- [ ] Reagendar/cancelar com motivo, conclusão com nota 0–10 e Markdown, bloqueio por dias inteiros

### Fase 7 — Financeiro e recorrência (RF010–RF013; D-05, D-08)
- [ ] M-05 `payments` + criação pendente na sessão (RN-50), pagar/editar/cancelar (RN-52 a RN-56)
- [ ] Cancelamento da sessão cancela o pagamento; pago bloqueia o cancelamento (RN-43/44)
- [ ] M-06 recorrência semanal/mensal gerando sessões com as regras de disponibilidade

### Fase 8 — Documentos com QuestPDF (RF014–RF016)
- [ ] `Features/Documents` com layout comum (clínica, psicólogo, CRP)
- [ ] Recibo (RN-57), relatório de sessões (RN-63), relatório de feedback (RN-64), laudo (M-08, RN-62)

### Fase 9 — Prontuários (RF019–RF020, RD001, RD002; D-06)
- [ ] M-07 + upload de anexos + busca; auditoria de acesso (DT-24)

### Fase 10 — IA (RF021; D-07)
- [ ] Consentimento e dados liberados; sugestões para anotações de sessão, depois laudos/relatórios

### Fase 11 — Desejáveis (RF024–RF026, RNF004)
- [ ] Lembretes por e-mail; painel (RC-06); deploy + backup (DT-22); rate limiting (DT-23)

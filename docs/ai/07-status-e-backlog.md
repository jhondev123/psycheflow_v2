# 07 — Status, dívidas técnicas e backlog

Retrato em 27/09/2026, após a fase de IA da API. Legenda em `README.md`.

## Resumo

- **Backend:** .NET 10 com Vertical Slices + Minimal APIs, testado (209 testes unitários + 161 de integração contra Postgres real,
  **94% de cobertura de linhas**). Cobre **todos os requisitos funcionais obrigatórios** (RF001–RF023): core (conta, usuários,
  configurações, psicólogos, pacientes, agenda, sessões), **financeiro e recorrência**, **documentos em PDF** (recibo, declaração,
  relatórios, laudos/relatórios psicológicos), **prontuários com anexos** e **assistente de IA** (Claude, OpenAI ou Gemini).
  Também tem o painel (RC-06), limite de requisições e trilha de acesso aos prontuários. Faltam só as notificações
  (RF024–RF026, adiadas pelo dev).
- **Frontend:** integrado à API (mock removido, D-09): todas as telas do escopo, incluindo financeiro, recorrência, documentos,
  laudos, prontuários com anexos, usuários, configurações e IA. Roda com `npm run dev` ou pelo compose (serviço `web`).
- **Pendente:** só os desejáveis adiados (notificações) e o deploy.

### Cobertura dos requisitos oficiais (RF001–RF026, 27 itens)

| Situação | API | Ponta a ponta (API + Front integrados) |
|---|---|---|
| ✅ Completo | 24 — RF001–RF023 (com RF015-B) | 24 — RF001–RF023 (com RF015-B) |
| ❌ Nada | 3 — RF024–RF026 (notificações, desejáveis — adiadas) | — |

## Matriz por módulo

| Módulo | API | Front | O que falta (resumo) |
|--------|-----|-------|----------------------|
| Conta / login / usuários | ✅ | ✅ | — |
| Configurações da empresa | ✅ | ✅ | — |
| Perfil do psicólogo + expediente | ✅ | ✅ | — |
| Pacientes | ✅ | ✅ | — |
| Agenda e bloqueios | ✅ | ✅ | — |
| Sessões | ✅ | ✅ | — |
| Financeiro + recorrência | ✅ | ✅ | — |
| Documentos (recibo, declaração, relatórios, laudos) | ✅ | ✅ | — |
| Prontuários | ✅ | ✅ | — |
| IA | ✅ | ✅ | Testar com uma chave real de provedor |
| Painel (RC-06) | ✅ | ✅ | — |
| Notificações | ❌ | ❌ | Desejável — adiada pelo dev |
| Testes | ✅ | 🟡 | Front: sem testes automatizados (validado manualmente no navegador); candidatos: Vitest nas funções de `lib/` |

## Divergências entre documentos e código

| # | Documento diz | Código | Tratamento |
|---|---------------|--------|------------|
| V-01 | `Requisitos-Psycheflow.docx`: RF01–RF29 "Pronto" | Era verdade só no mock; hoje o front está integrado à API | Resolvido |
| V-02 | Idem RF25/RF26 "Criar modelos de documento com campos" pronto | Modelos de documento foram removidos da API | Documentos voltam como QuestPDF (fase Documentos) |
| V-03 | Idem RF01 "Criar conta (empresa + responsável)" | ✅ API e front criam a clínica e o responsável | Resolvido |
| V-04 | Requisito: feedback = nota 0–10 | ✅ `feedback_score` 0–10 (+ comentário opcional) | Resolvido |
| V-05 | Status de sessão Agendada/Concluída/Cancelada | ✅ `SessionStatus = Scheduled, Completed, Cancelled, NoShow` | Resolvido (D-04) |
| V-06 | "CPF validado na tela, sem coluna" | ✅ coluna `cpf` com VO | Resolvido (M-01) |
| V-07 | UC4: pagamento pendente criado com a sessão; UC11: pagamento lançado após conclusão | ✅ as duas coisas (D-08) | Resolvido |
| V-08 | RF021: opt-in **do psicólogo**, sugestões também em laudos/relatórios | Consentimento **da clínica** (Admin/Manager); sugestões em anotações, análise e próximos passos | Decisão do dev (D-07) |

## Dívidas técnicas e bugs

### Abertas
| ID | Severidade | Onde | Problema | Correção sugerida |
|----|-----------|------|----------|-------------------|
| DT-22 | Média | Deploy | Sem ambiente publicado (RNF004); segredos de produção por variável de ambiente | Publicar a imagem Docker + Postgres gerenciado com backup |
| DT-24 | Baixa | LGPD | Trilha de acesso aos prontuários e auditoria da IA feitas; falta **criptografia em repouso** | Criptografia do disco/volume e do Postgres gerenciado no deploy (DT-22) |
| DT-26 | Baixa | IA | Terceiros citados nas anotações só são mascarados quando o prenome está na lista de nomes comuns (~320) | Orientar nos termos de uso a evitar nomes completos de terceiros nas anotações |

### Resolvidas depois da reestruturação
DT-01 (front integrado à API; mock, seed e validações duplicadas removidos; CORS expõe `Content-Disposition` para os downloads) · DT-23 (limite de requisições em login/registro por IP e nas sugestões de IA por usuário) · DT-24, parcial (trilha de acesso aos
prontuários) · DT-25 (skills atualizadas em `.claude/skills/` do repositório; as cópias do plugin `anthropic-skills` continuam
antigas e devem ser atualizadas no claude.ai ou removidas) · DT-26, parcial (prenomes comuns de terceiros viram `[pessoa]`).

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
| D-05 | Recorrência gera sessões até quando? | ✅ Janelas de 3 meses + "estender"; datas indisponíveis são puladas e informadas. |
| D-06 | Onde guardar anexos do prontuário? | ✅ Disco/volume (`Storage:Path`) atrás da abstração `IFileStorage` (trocável por S3 no deploy). |
| D-07 | Qual provedor de IA? | ✅ Claude, OpenAI e Gemini (SDKs oficiais) atrás de `IAiTextGenerator`; chaves no servidor; a clínica habilita, escolhe provedor e dados liberados; dados pseudonimizados; usos: anotações da sessão, análise do paciente e próximos passos. |
| D-08 | Pagamento nasce pendente ao criar a sessão (UC4) e é "lançado"/pago depois (UC11)? | ✅ Sim. |
| D-09 | O front mantém o modo mock (exigência do Projeto Web) ou é integrado? | ✅ Remover o mock e integrar com a API. |
| D-11 | Notificações (RF024–RF026)? | ⏸️ Deixadas para depois pelo dev. |

## Backlog priorizado

> Fases 0–5 (reestruturação + core) e as fases de Financeiro/Recorrência, Documentos, Prontuários e IA estão concluídas na API —
> ver o progresso em `08-plano-reestruturacao.md`.

### Painel e endurecimento da API
- [x] `GET /dashboard` (RC-06): contadores do dia/semana, agenda de hoje, próximos atendimentos e financeiro
- [x] Limite de requisições por IP em login/registro e por usuário nas sugestões de IA (DT-23)
- [x] Trilha de acesso aos prontuários (DT-24) e mascaramento de terceiros na IA (DT-26)

### Integração do front (DT-01, D-09)
- [x] `lib/api.ts` (base URL por env, Bearer, ProblemDetails → mensagens por campo) e remoção do mock
- [x] Tipos novos em `types/index.ts`; login/registro reais; troca de senha obrigatória
- [x] Pacientes (com ficha), Agenda, Sessões, Horários, Perfil, Clínica e Usuários via API
- [x] Pagamentos, recorrência, documentos (PDFs), laudos, prontuários (com anexos e trilha de acesso) e IA (configuração + sugestões)
- [x] Front no compose (serviço `web`, Nginx) para rodar sem Node instalado
- [ ] Testes automatizados do front (Vitest nas funções de `lib/`) — opcional

### Desejáveis
- [ ] Notificações (RF024–RF026) — adiadas (D-11)
- [ ] Deploy + backup (DT-22)

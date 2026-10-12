# Rastreabilidade — requisitos, casos de uso e regras

Liga cada ID dos documentos do TCC à capacidade em [`openspec/specs/`](../specs/) que o especifica, com o status por camada.
O texto vigente de cada requisito, com cenários, está na spec; esta página é o índice e o retrato do que está pronto.

Legenda: ✅ implementado · 🟡 parcial · ❌ não iniciado · — não se aplica. Status atualizado em 28/09/2026
(API reestruturada e front integrado à API, mock removido — D-09).

## Ajustes de numeração

O documento oficial (`fontes/documento-de-requisitos.md`) tem IDs repetidos. Na consolidação:

- Havia dois `RF015`. O de relatório de feedback virou **RF015-B**.
- Os três `RF000` (notificações) viraram **RF024, RF025, RF026**.
- O que existe no código mas não no documento oficial virou **RC-xx** (requisito complementar).
- Os casos de uso UC01–UC24 vêm de `fontes/documento-de-casos-de-uso.md`; UC25–UC29 foram acrescentados a partir do código.
  Erros de cópia do original foram corrigidos nas specs (UC07 e UC08 diziam "reagendamento"; UC10 dizia "sessão editada").

## Requisitos funcionais

| ID | Requisito | Prioridade | Capacidade | API | Front | UC |
|----|-----------|-----------|------------|-----|-------|----|
| RF001 | Cadastro de pacientes | Prioritário | [pacientes](../specs/pacientes/spec.md) | ✅ | ✅ | UC01 |
| RF002 | Visualização e filtros de pacientes | Prioritário | [pacientes](../specs/pacientes/spec.md) | ✅ | ✅ | UC03 |
| RF003 | Edição de pacientes | Prioritário | [pacientes](../specs/pacientes/spec.md) | ✅ | ✅ | UC02 |
| RF004 | Cadastro de sessões | Prioritário | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ | UC04 |
| RF005 | Visualização de sessões (calendário + filtros) | Prioritário | [sessoes](../specs/sessoes/spec.md), [agenda](../specs/agenda/spec.md) | ✅ | ✅ | UC05, UC18 |
| RF006 | Reagendamento de sessões | Prioritário | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ | UC06 |
| RF007 | Cancelamento de sessões | Prioritário | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ | UC07 |
| RF008 | Edição de sessões | Prioritário | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ | UC09 |
| RF009 | Conclusão da sessão | Prioritário | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ | UC08 |
| RF010 | Recorrência de sessão | Prioritário | [recorrencia](../specs/recorrencia/spec.md) | ✅ | ✅ | UC10 |
| RF011 | Lançamento de pagamento | Prioritário | [financeiro](../specs/financeiro/spec.md) | ✅ | ✅ | UC11 |
| RF012 | Visualização de pagamentos | Prioritário | [financeiro](../specs/financeiro/spec.md) | ✅ | ✅ | UC12 |
| RF013 | Edição de pagamentos | Prioritário | [financeiro](../specs/financeiro/spec.md) | ✅ | ✅ | UC13 |
| RF014 | Recibos de sessões | Prioritário | [documentos](../specs/documentos/spec.md) | ✅ | ✅ | UC14 |
| RF015 | Relatório de sessões | Prioritário | [documentos](../specs/documentos/spec.md) | ✅ | ✅ | UC15 |
| RF015-B | Relatório de feedback de pacientes | Prioritário | [documentos](../specs/documentos/spec.md) | ✅ | ✅ | UC16 |
| RF016 | Geração de laudos | Prioritário | [laudos](../specs/laudos/spec.md) | ✅ | ✅ | UC17 |
| RF017 | Gerenciamento da agenda | Prioritário | [agenda](../specs/agenda/spec.md) | ✅ | ✅ | UC18 |
| RF018 | Bloqueio de agenda | Prioritário | [agenda](../specs/agenda/spec.md) | ✅ | ✅ | UC19 |
| RF019 | Criação de prontuários | Prioritário | [prontuarios](../specs/prontuarios/spec.md) | ✅ | ✅ | UC20 |
| RF020 | Gerenciamento de prontuários | Prioritário | [prontuarios](../specs/prontuarios/spec.md) | ✅ | ✅ | UC21 |
| RF021 | Sugestões com IA | Prioritário\* | [assistente-ia](../specs/assistente-ia/spec.md) | ✅ | ✅ | UC22 |
| RF022 | Login do psicólogo | Prioritário | [autenticacao](../specs/autenticacao/spec.md) | ✅ | ✅ | UC23 |
| RF023 | Logout | Prioritário | [autenticacao](../specs/autenticacao/spec.md) | ✅ | ✅ | UC24 |
| RF024 | Notificação de sessões para o paciente | Desejável | — (backlog, D-11) | ❌ | ❌ | — |
| RF025 | Notificação de sessões para o psicólogo | Desejável | — (backlog, D-11) | ❌ | ❌ | — |
| RF026 | Notificação de conflitos de agendamento | Desejável | — (backlog, D-11) | ❌ | ❌ | — |

\* RF021 é prioritário no documento, mas depende de quase tudo (sessões, prontuários); foi implementado por último.
Divergência aceita pelo dev (D-07): o consentimento é **da clínica** (Admin/Manager), não de cada psicólogo, e as
sugestões cobrem anotações da sessão, análise do paciente e próximos passos — laudos e relatórios ficaram de fora.

RF024–RF026 não têm descrição no documento original. Sugestão mínima registrada no backlog: e-mail de lembrete D-1
para o paciente (RF024), resumo diário para o psicólogo (RF025) e aviso de conflito ao agendar (RF026 — já coberto pela
mensagem `scheduling.conflict`).

## Requisitos complementares (existem no código, não no documento oficial)

| ID | Requisito | Capacidade | API | Front | UC |
|----|-----------|------------|-----|-------|----|
| RC-01 | Criação de conta (empresa + usuário responsável) | [autenticacao](../specs/autenticacao/spec.md) | ✅ | ✅ | UC25 |
| RC-02 | Cadastro de usuários da equipe por Admin/Gerente | [usuarios](../specs/usuarios/spec.md) | ✅ | ✅ | UC26 |
| RC-03 | Horários de trabalho do psicólogo | [psicologos](../specs/psicologos/spec.md) | ✅ | ✅ | UC27 |
| RC-04 | Perfil profissional (nome, CRP, abordagem, telefone) | [psicologos](../specs/psicologos/spec.md) | ✅ | ✅ | UC28 |
| RC-05 | Modelos de documento editáveis | — (removido, substituído por [documentos](../specs/documentos/spec.md) fixos em PDF) | ❌ | ❌ | UC29 |
| RC-06 | Painel inicial (resumo do dia e da semana) | [painel](../specs/painel/spec.md) | ✅ | ✅ | — |
| RC-07 | Tema claro/escuro | [interface-web](../specs/interface-web/spec.md) | — | ✅ | — |
| RC-08 | Verificação de saúde da API | [plataforma](../specs/plataforma/spec.md) | ✅ | — | — |
| — | Configurações da clínica (duração, valor padrão, fuso) | [configuracoes-da-clinica](../specs/configuracoes-da-clinica/spec.md) | ✅ | ✅ | — |

## Requisitos não funcionais e de domínio

| ID | Requisito | Capacidade | Status | Situação / o que falta |
|----|-----------|------------|--------|------------------------|
| RNF001 | Segurança: criptografia, autenticação, LGPD | [plataforma](../specs/plataforma/spec.md), [autenticacao](../specs/autenticacao/spec.md), [prontuarios](../specs/prontuarios/spec.md), [assistente-ia](../specs/assistente-ia/spec.md) | 🟡 | Feito: rotas autenticadas por padrão, isolamento por empresa, bloqueio por tentativas, limite de requisições, senha temporária, sigilo clínico, trilha de acesso aos prontuários, IA pseudonimizada e auditada. Falta: criptografia em repouso e HTTPS, que dependem do deploy (DT-22, DT-24). |
| RNF002 | Desempenho: resposta < 2 s | [plataforma](../specs/plataforma/spec.md) | 🟡 | Listas paginadas, agenda limitada a 62 dias, índices por (psicólogo, data), (empresa, CPF) e (paciente, status). Sem medição formal. |
| RNF003 | Usabilidade: interface intuitiva e responsiva | [interface-web](../specs/interface-web/spec.md) | ✅ | Front responsivo em pt-BR, tema claro/escuro, avisos e estados vazios. |
| RNF004 | Disponibilidade 99,9% e backups automáticos | — (backlog, DT-22) | ❌ | Sem deploy. |
| RD001 | Resolução CFP nº 013/2015 (prontuário eletrônico) | [prontuarios](../specs/prontuarios/spec.md) | 🟡 | Prontuário sigiloso com guarda (exclusão lógica) e trilha de acesso; falta o dev conferir o texto das normas do CFP sobre registro documental. |
| RD002 | Confidencialidade: só profissionais autorizados | [plataforma](../specs/plataforma/spec.md), [sessoes](../specs/sessoes/spec.md), [prontuarios](../specs/prontuarios/spec.md), [laudos](../specs/laudos/spec.md) | ✅ | Isolamento por empresa; psicólogo só acessa a própria agenda; anotações e feedback só para o psicólogo da sessão (D-02); prontuários e laudos só para o autor. |

Requisitos não funcionais do front da época do mock (`fontes/requisitos-do-front-mock.md`, RNF01–RNF12, aqui
RNF-F01–F12) continuam valendo e estão em [interface-web](../specs/interface-web/spec.md), **exceto RNF-F02/F03**
(dados simulados e uso offline), que deixaram de valer com a integração à API.

## Casos de uso

| UC | Nome | Capacidade | API | Front |
|----|------|------------|-----|-------|
| UC01 | Cadastrar paciente | [pacientes](../specs/pacientes/spec.md) | ✅ | ✅ |
| UC02 | Editar paciente | [pacientes](../specs/pacientes/spec.md) | ✅ | ✅ |
| UC03 | Visualizar pacientes | [pacientes](../specs/pacientes/spec.md) | ✅ | ✅ |
| UC04 | Cadastrar sessão | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ |
| UC05 | Visualizar sessões | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ |
| UC06 | Reagendar sessão | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ |
| UC07 | Cancelar sessão | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ |
| UC08 | Concluir sessão (e marcar falta) | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ |
| UC09 | Editar sessão | [sessoes](../specs/sessoes/spec.md) | ✅ | ✅ |
| UC10 | Criar recorrência de sessões | [recorrencia](../specs/recorrencia/spec.md) | ✅ | ✅ |
| UC11 | Lançar pagamento | [financeiro](../specs/financeiro/spec.md) | ✅ | ✅ |
| UC12 | Visualizar pagamentos | [financeiro](../specs/financeiro/spec.md) | ✅ | ✅ |
| UC13 | Editar pagamento | [financeiro](../specs/financeiro/spec.md) | ✅ | ✅ |
| UC14 | Gerar recibo de sessão | [documentos](../specs/documentos/spec.md) | ✅ | ✅ |
| UC15 | Gerar relatório de sessões | [documentos](../specs/documentos/spec.md) | ✅ | ✅ |
| UC16 | Gerar relatório de feedback | [documentos](../specs/documentos/spec.md) | ✅ | ✅ |
| UC17 | Gerar laudo | [laudos](../specs/laudos/spec.md) | ✅ | ✅ |
| UC18 | Gerenciar agenda | [agenda](../specs/agenda/spec.md) | ✅ | ✅ |
| UC19 | Lançar bloqueio de agenda | [agenda](../specs/agenda/spec.md) | ✅ | ✅ |
| UC20 | Criar prontuário | [prontuarios](../specs/prontuarios/spec.md) | ✅ | ✅ |
| UC21 | Gerenciar prontuários | [prontuarios](../specs/prontuarios/spec.md) | ✅ | ✅ |
| UC22 | Usar sugestões de IA | [assistente-ia](../specs/assistente-ia/spec.md) | ✅ | ✅ |
| UC23 | Login | [autenticacao](../specs/autenticacao/spec.md) | ✅ | ✅ |
| UC24 | Logout | [autenticacao](../specs/autenticacao/spec.md) | ✅ | ✅ |
| UC25 | Criar conta (empresa + responsável) | [autenticacao](../specs/autenticacao/spec.md) | ✅ | ✅ |
| UC26 | Admin/Gerente cadastra usuário | [usuarios](../specs/usuarios/spec.md) | ✅ | ✅ |
| UC27 | Definir horários de trabalho | [psicologos](../specs/psicologos/spec.md) | ✅ | ✅ |
| UC28 | Editar perfil profissional | [psicologos](../specs/psicologos/spec.md) | ✅ | ✅ |
| UC29 | Gerenciar e gerar modelos de documento | — (removido com RC-05) | ❌ | ❌ |

## Regras de negócio

Cada RN aparece na linha `**Origem**` dos requisitos da capacidade indicada. "Onde" aponta o código que a garante hoje.

| RN | Regra (resumo) | Capacidade | Onde |
|----|----------------|------------|------|
| RN-01 | Dados isolados por empresa | [plataforma](../specs/plataforma/spec.md) | Filtro global `Tenant` (`AppDbContext`) |
| RN-02 | Exclusão lógica | [plataforma](../specs/plataforma/spec.md) | Filtro global `SoftDelete` + `AuditingInterceptor` |
| RN-03 | `updatedAt` em toda alteração | [plataforma](../specs/plataforma/spec.md) | `AuditingInterceptor` |
| RN-04 | Erros claros e sem gravação parcial | [plataforma](../specs/plataforma/spec.md) | `Result`/`Error` → ProblemDetails; transações |
| RN-10 | Login por e-mail e senha | [autenticacao](../specs/autenticacao/spec.md) | `Features/Auth/Login` |
| RN-11 | Senha forte (6+, maiúscula, minúscula, número, símbolo) | [autenticacao](../specs/autenticacao/spec.md) | Opções do Identity em `CommonServices`; front `lib/validation.ts` |
| RN-12 | Bloqueio de 15 min após 5 erros | [autenticacao](../specs/autenticacao/spec.md) | Lockout do Identity (423) |
| RN-13 | E-mail único | [autenticacao](../specs/autenticacao/spec.md) | Identity (`auth.email_in_use`) |
| RN-14 | CRP obrigatório no formato `06/12345` | [psicologos](../specs/psicologos/spec.md) | VO `LicenseNumber` (D-03) |
| RN-15 | Logout descarta o token | [autenticacao](../specs/autenticacao/spec.md) | Front (`lib/api.ts`); JWT sem estado |
| RN-20 | Obrigatórios do paciente: nome, CPF, telefone, e-mail | [pacientes](../specs/pacientes/spec.md) | `PatientDataValidator` |
| RN-21 | CPF válido | [pacientes](../specs/pacientes/spec.md) | VO `Cpf` |
| RN-22 | CPF único por empresa | [pacientes](../specs/pacientes/spec.md) | Índice único filtrado (company_id, cpf) + checagem (409) |
| RN-23 | E-mail válido | [pacientes](../specs/pacientes/spec.md) | `PatientDataValidator` |
| RN-24 | Telefone BR válido | [pacientes](../specs/pacientes/spec.md) | Validação `ValidPhone` |
| RN-25 | Paciente nasce Ativo | [pacientes](../specs/pacientes/spec.md) | `Patient.Create` |
| RN-26 | CEP primeiro, preenche o endereço | [pacientes](../specs/pacientes/spec.md) | Front (ViaCEP); API valida CEP e UF |
| RN-27 | CPF não editável | [pacientes](../specs/pacientes/spec.md) | `UpdatePatientRequest` sem CPF |
| RN-30 | Não agendar no passado | [agenda](../specs/agenda/spec.md) | `TimeSlot` + `ScheduleAvailability` (`ClinicClock`) |
| RN-31 | Fim depois do início, no mesmo dia | [agenda](../specs/agenda/spec.md) | `TimeSlot`; CHECK no banco |
| RN-32 | Duração padrão da configuração, 15–240 min | [sessoes](../specs/sessoes/spec.md), [configuracoes-da-clinica](../specs/configuracoes-da-clinica/spec.md) | `company_settings.session_duration_minutes` |
| RN-33 | Sessão dentro do expediente | [agenda](../specs/agenda/spec.md) | `Psychologist.WorksAt` |
| RN-34 | Sem sobreposição com itens não cancelados | [agenda](../specs/agenda/spec.md) | `ScheduleAvailability` + advisory lock do Postgres |
| RN-35 | Sessão exige paciente cadastrado e ativo | [sessoes](../specs/sessoes/spec.md), [pacientes](../specs/pacientes/spec.md) | `CreateSessionHandler` |
| RN-36 | Toda sessão nasce de um item de agenda (1:1) | [sessoes](../specs/sessoes/spec.md) | `Session` cria o próprio `Schedule` |
| RN-37 | Bloqueio não vira sessão e impede agendamentos | [agenda](../specs/agenda/spec.md) | `ScheduleType.Block` |
| RN-38 | Bloqueio não conflita com sessões | [agenda](../specs/agenda/spec.md) | `ScheduleAvailability` (tudo ou nada) |
| RN-39 | Bloqueio por dias inteiros ou horários | [agenda](../specs/agenda/spec.md) | `CreateBlock` |
| RN-40 | Bloqueio ignora o expediente | [agenda](../specs/agenda/spec.md) | `CreateBlock` |
| RN-41 | Sessão encerrada não muda | [sessoes](../specs/sessoes/spec.md) | `Session.IsOpen` (409 `session.not_open`) |
| RN-42 | Reagendar exige motivo e reaplica as regras | [sessoes](../specs/sessoes/spec.md) | `RescheduleSession` |
| RN-43 | Cancelar exige motivo e cancela o pagamento | [sessoes](../specs/sessoes/spec.md) | `SessionAccess.Cancel` |
| RN-44 | Pagamento recebido bloqueia o cancelamento | [sessoes](../specs/sessoes/spec.md), [financeiro](../specs/financeiro/spec.md) | `SessionAccess.Cancel` |
| RN-45 | Concluir exige anotações e nota 0–10 | [sessoes](../specs/sessoes/spec.md) | `Session.Complete`; CHECK no banco |
| RN-46 | Anotações em Markdown | [sessoes](../specs/sessoes/spec.md) | `sessions.notes`; front renderiza |
| RN-47 | Falta (NoShow) | [sessoes](../specs/sessoes/spec.md) | `MarkNoShow` |
| RN-48 | Recorrência semanal/mensal respeitando a agenda | [recorrencia](../specs/recorrencia/spec.md) | `RecurrencePattern` + `RecurrenceGenerator` |
| RN-50 | Sessão nasce com pagamento pendente | [financeiro](../specs/financeiro/spec.md) | `SessionAccess.AddWithPayment` |
| RN-51 | Valor: informado → recorrência → padrão → 0 | [financeiro](../specs/financeiro/spec.md) | `SessionAccess` |
| RN-52 | Métodos: crédito, débito, Pix | [financeiro](../specs/financeiro/spec.md) | `PaymentMethod` |
| RN-53 | Data do pagamento não anterior a hoje | [financeiro](../specs/financeiro/spec.md) | `Payment.Pay` |
| RN-54 | Situações: Pendente, Pago, Cancelado | [financeiro](../specs/financeiro/spec.md) | `PaymentStatus` |
| RN-55 | Lançar exige sessão concluída | [financeiro](../specs/financeiro/spec.md) | `PayPayment` |
| RN-56 | Pagamento pago não é editado | [financeiro](../specs/financeiro/spec.md) | `Payment.ChangeAmount` |
| RN-57 | Recibo só de pagamento recebido | [documentos](../specs/documentos/spec.md) | `GetReceiptHandler` |
| RN-60, RN-61 | Modelos de documento com campos | — (removido com RC-05) | — |
| RN-62 | Laudo exige paciente, modelo e finalidade | [laudos](../specs/laudos/spec.md) | `PsychologicalReports/CreateReport` |
| RN-63 | Filtros do relatório de sessões | [documentos](../specs/documentos/spec.md) | `SessionsReportQuery` |
| RN-64 | Filtros do relatório de feedback | [documentos](../specs/documentos/spec.md) | `FeedbackReportQuery` |
| RN-65 | Prontuário com texto e anexos PDF/imagem | [prontuarios](../specs/prontuarios/spec.md) | `AttachmentPolicy` (10 MB, assinatura) |
| RN-66 | Busca e acesso exclusivo do autor | [prontuarios](../specs/prontuarios/spec.md) | `MedicalRecordAccess` |
| RN-67 | IA com consentimento e dados liberados | [assistente-ia](../specs/assistente-ia/spec.md) | `ai_settings`, `AiAssistant`, `Pseudonymizer` |
| RN-68 | Expediente: início < fim; salvar substitui tudo | [psicologos](../specs/psicologos/spec.md) | `Psychologist.SetWorkingHours` |

## IDs do documento do front (mock)

`fontes/requisitos-do-front-mock.md` usava IDs próprios. Correspondência:

| Front (mock) | Oficial / capacidade |
|--------------|----------------------|
| RF01 | RC-01 — [autenticacao](../specs/autenticacao/spec.md) |
| RF02, RF04 | RF022 — [autenticacao](../specs/autenticacao/spec.md) |
| RF03 | RF023 — [autenticacao](../specs/autenticacao/spec.md) |
| RF05 | RN-11 — [autenticacao](../specs/autenticacao/spec.md) |
| RF06, RF07, RF08, RF10 | RF001–RF003 — [pacientes](../specs/pacientes/spec.md) |
| RF09 (excluir paciente) | Substituído por inativação — [pacientes](../specs/pacientes/spec.md) |
| RF11–RF14 | RF017 — [agenda](../specs/agenda/spec.md) |
| RF15, RF21 | RF004 — [sessoes](../specs/sessoes/spec.md) |
| RF16 | RF018 — [agenda](../specs/agenda/spec.md) |
| RF17, RF18 | RF007, RF008 e confirmação — [sessoes](../specs/sessoes/spec.md) |
| RF19, RF20 | RC-03 — [psicologos](../specs/psicologos/spec.md), [agenda](../specs/agenda/spec.md) |
| RF22 | RF009 — [sessoes](../specs/sessoes/spec.md) |
| RF23 | Situações da sessão (sem "em andamento", D-04) — [sessoes](../specs/sessoes/spec.md) |
| RF24 | RN-47 — [sessoes](../specs/sessoes/spec.md) |
| RF25, RF26 | RC-05 (removido) |
| RF27 | RC-04 — [psicologos](../specs/psicologos/spec.md) |
| RF28 | RC-07 — [interface-web](../specs/interface-web/spec.md) |
| RF29 | RC-06 — [painel](../specs/painel/spec.md) |

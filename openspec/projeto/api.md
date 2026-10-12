# 06 — API (contratos)

Base: `Psycheflow.Api` · prefixo **`/api/v1`** · documentação interativa em **`/scalar`** (Development) e OpenAPI em `/openapi/v1.json`.
Auth: `Authorization: Bearer <jwt>` (de `POST /auth/login` ou `/auth/register`, validade 2h). Todas as rotas exigem
usuário interno (Admin/Manager/Psychologist) com senha já trocada, exceto quando indicado.

## Formato

- **Sucesso:** o próprio recurso em JSON (sem envelope). `201 Created` com `Location` em criações; `204` em exclusões.
- **JSON:** camelCase; enums como texto (`"Scheduled"`, `"Monday"`); datas `yyyy-MM-dd`; horas `HH:mm:ss` (aceita `HH:mm`).
- **Erros:** `application/problem+json` (RFC 9457):

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Dados inválidos.",
  "status": 422,
  "detail": "Um ou mais campos não passaram na validação.",
  "errors": { "cpf": ["CPF inválido."], "address.zipCode": ["CEP inválido. Informe os 8 dígitos."] },
  "code": "validation_failed",
  "traceId": "00-…"
}
```

| Status | Quando |
|--------|--------|
| 400 | JSON malformado / parâmetro com tipo inválido |
| 401 | Sem token, token inválido/expirado, credenciais inválidas |
| 403 | Sem permissão (perfil, agenda de outro psicólogo, dados clínicos de outro psicólogo, senha temporária pendente) |
| 404 | Não existe **na empresa do usuário** (dados de outras empresas nunca aparecem) |
| 409 | Conflito de estado: e-mail/CPF já cadastrado, horário ocupado, sessão já concluída/cancelada |
| 422 | Validação de campos (`errors` por campo) ou regra de negócio ligada a um campo (fora do expediente, no passado…) |
| 423 | Conta bloqueada por excesso de tentativas |

`code` é estável e pode ser usado pelo front (ex.: `scheduling.conflict`, `patient.cpf_already_registered`).

---

## Auth
| Método | Rota | Acesso | Descrição |
|--------|------|--------|-----------|
| POST | `/auth/register` | público | Cria empresa + usuário (Admin + Psychologist) + perfil de psicólogo → `201 AuthResponse` |
| POST | `/auth/login` | público | `{ email, password }` → `AuthResponse`; 5 erros → 423 por 15 min |
| POST | `/auth/change-password` | autenticado (inclusive senha temporária) | `{ currentPassword, newPassword }` → novo `AuthResponse` |
| GET | `/auth/me` | autenticado (inclusive senha temporária) | `{ id, fullName, email, roles[], companyId, companyName, psychologistId?, mustChangePassword }` |

```json
// POST /auth/register
{ "companyName": "Clínica Viver", "fullName": "Ana Souza", "email": "ana@clinica.com", "password": "Senha@123",
  "licenseNumber": "06/12345", "approach": "CognitiveBehavioral", "phone": "(45) 99999-1234" }
// AuthResponse
{ "accessToken": "eyJ…", "expiresAt": "2026-10-05T14:00:00+00:00", "mustChangePassword": false }
```
Claims do token: `sub`, `email`, `name`, `role` (1..n), `company_id`, `psychologist_id?`, `must_change_password?`.

## Usuários (Admin/Manager)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/users` | `{ fullName, email, role (Admin, Manager ou Psychologist), licenseNumber?, approach? }` → `201 { id, fullName, email, role, psychologistId?, temporaryPassword }`. Só Admin cria Admin. |
| GET | `/users` | Usuários da empresa: `{ id, fullName, email, roles[], psychologistId?, mustChangePassword, isLockedOut }[]` |

## Configurações
| Método | Rota | Acesso | Descrição |
|--------|------|--------|-----------|
| GET | `/settings` | todos | `{ sessionDurationMinutes, sessionDefaultPrice?, timeZone }` |
| PUT | `/settings` | Admin/Manager | mesmo formato; duração 15–240, fuso IANA |

## Psicólogos
| Método | Rota | Acesso | Descrição |
|--------|------|--------|-----------|
| GET | `/psychologists` | todos | lista da empresa com expediente |
| GET | `/psychologists/me` | psicólogo | perfil do usuário logado (404 se não tiver) |
| GET | `/psychologists/{id}` | todos | `PsychologistResponse` |
| PUT | `/psychologists/{id}` | o próprio ou Admin/Manager | `{ fullName, licenseNumber, approach, phone? }` |
| GET | `/psychologists/{id}/working-hours` | todos | `[{ dayOfWeek, startTime, endTime }]` ordenado |
| PUT | `/psychologists/{id}/working-hours` | o próprio ou Admin/Manager | `{ hours: [...] }` substitui tudo; sem sobreposição no mesmo dia |

`PsychologistResponse`: `{ id, userId, fullName, email, licenseNumber, approach, phone?, workingHours[] }`.

## Pacientes
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/patients` | `{ fullName, cpf, email, phone, birthDate?, address?, notes? }` → 201; CPF duplicado na empresa → 409 |
| GET | `/patients?search=&status=&lastSessionFrom=&lastSessionTo=&page=&pageSize=` | `PagedResponse<PatientListItem>`; `search` = parte do nome/e-mail ou CPF completo |
| GET | `/patients/{id}` | `PatientResponse` |
| PUT | `/patients/{id}` | `{ fullName, email, phone, status, birthDate?, address?, notes? }` (CPF não editável) |

`address`: `{ zipCode, street, number, complement?, neighborhood, city, state }` (CEP 8 dígitos, UF válida).
`PagedResponse<T>`: `{ items[], page, pageSize, totalCount, totalPages }` (pageSize padrão 20, máx. 100).
`PatientListItem`: `{ id, fullName, cpf, email, phone, status, lastSessionDate? }`.

## Agenda e bloqueios
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/agenda?from=&to=&psychologistId=` | Até 62 dias. Psicólogo vê só a própria; Admin/Manager sem `psychologistId` veem todos. → `{ from, to, workingHours[{ psychologistId, fullName, hours[] }], items[AgendaItem] }` |
| POST | `/schedule-blocks` | `{ startDate, endDate?, startTime?, endTime?, reason?, psychologistId? }` → `201 BlockResponse[]` (um por dia; sem horários = dia inteiro; máx. 31 dias; ignora expediente; conflito → 409 e nada é criado) |
| DELETE | `/schedule-blocks/{id}` | 204 |

`AgendaItem`: `{ scheduleId, psychologistId, type, status, date, startTime, endTime, blockReason?, sessionId?, patientId?, patientName?, sessionStatus? }`.

## Sessões
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/sessions` | `{ patientId, date, startTime, durationMinutes?, psychologistId?, notes?, price? }` → 201. Duração padrão da empresa; 15–240 min. Regras: não no passado, dentro do expediente, sem conflito, paciente ativo. Cria o pagamento **pendente** com `price` ou o valor padrão da empresa (D-08). |
| GET | `/sessions?from=&to=&timeFrom=&timeTo=&patientId=&psychologistId=&status=&page=&pageSize=` | `from` obrigatório. `PagedResponse<SessionListItem>` |
| GET | `/sessions/{id}` | `SessionResponse` (anotações/feedback só para o psicólogo da sessão) |
| PUT | `/sessions/{id}` | `{ patientId, notes? }` (`notes` nulo mantém; só o psicólogo da sessão altera notas) |
| POST | `/sessions/{id}/confirm` | confirma o horário |
| POST | `/sessions/{id}/reschedule` | `{ date, startTime, reason, durationMinutes? }` (mantém a duração se omitida) |
| POST | `/sessions/{id}/cancel` | `{ reason }` — libera o horário e cancela o pagamento pendente |
| POST | `/sessions/{id}/complete` | `{ notes, feedbackScore (0–10), feedbackComment? }` — só o psicólogo da sessão, após o início |
| POST | `/sessions/{id}/no-show` | falta, após o início |
| DELETE | `/sessions/{id}` | exclusão lógica (não permitida para concluída) |

Sessão concluída, cancelada ou com falta não aceita mais alterações (409). Psicólogo só acessa as próprias sessões (403);
Admin/Manager acessam todas da empresa, sem ver anotações/feedback.

`SessionResponse`: `{ id, scheduleId, psychologistId, psychologistName, patientId, patientName, date, startTime, endTime, durationMinutes,
status, scheduleStatus, notes?, feedbackScore?, feedbackComment?, cancellationReason?, rescheduleReason?, clinicalNotesVisible, recurrenceId?,
payment?: { id, amount, status }, createdAt, updatedAt? }`.

## Financeiro — pagamentos (RF011–RF013)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/payments?from=&to=&patientId=&psychologistId=&status=&page=&pageSize=` | `PagedResponse<PaymentResponse>` (período = data da sessão). Psicólogo vê só os das próprias sessões. |
| GET | `/payments/{id}` | `PaymentResponse` |
| PUT | `/payments/{id}` | `{ amount }` — só pendente (pago → 409) |
| POST | `/payments/{id}/pay` | `{ method (CreditCard, DebitCard, Pix), paidAt?, amount?, notes? }` — só de sessão **concluída**; `paidAt` padrão = hoje, não pode ser anterior a hoje (RN-53) |
| POST | `/payments/{id}/cancel` | `{ reason }` — cancela o pendente ou estorna o pago |

`PaymentResponse`: `{ id, sessionId, patientId, patientName, psychologistId, sessionDate, sessionStartTime, sessionStatus, amount, status (Pending, Paid,
Cancelled), method?, paidAt?, notes?, cancellationReason? }`.

## Recorrência (RF010)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/recurrences` | `{ patientId, type (Weekly, Monthly), startDate, startTime, endDate?, durationMinutes?, price?, psychologistId? }` → `201 RecurrenceGenerationResponse`. Gera as sessões dos próximos **3 meses** (D-05); datas indisponíveis são **puladas** e devolvidas com o motivo. |
| GET | `/recurrences?patientId=&activeOnly=` | `RecurrenceResponse[]` |
| POST | `/recurrences/{id}/extend` | gera mais 3 meses a partir de `generatedUntil` |
| POST | `/recurrences/{id}/end` | `{ reason? }` — encerra e cancela as sessões futuras ainda agendadas |

`RecurrenceGenerationResponse`: `{ recurrence: { id, patientId, psychologistId, type, startDate, endDate?, startTime, durationMinutes, price,
generatedUntil, isActive, endReason? }, scheduled: date[], skipped: [{ date, code, reason }] }`.

## Documentos em PDF (RF014, RF015, RF015-B)
Respostas `application/pdf` (download com nome de arquivo). Cabeçalho comum: clínica, psicólogo e CRP.

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/documents/receipts/{paymentId}` | Recibo de pagamento **pago** (valor por extenso, RN-57) |
| GET | `/documents/attendance/{sessionId}` | Declaração de comparecimento de sessão concluída |
| GET | `/documents/sessions-report?from=&to=&sessionStatus=&paymentStatus=&psychologistId=` | Relatório de sessões com totais (RN-63) |
| GET | `/documents/feedback-report?patientId=&from=&to=` | Feedbacks 0–10 do paciente com o psicólogo logado, com média (RN-64) |

## Laudos e relatórios psicológicos (RF016)
Somente o psicólogo autor acessa. Modelos da Resolução CFP 06/2019: `PsychologicalReport` (laudo) e `PsychologicalStatement` (relatório).

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/psychological-reports` | `{ patientId, template, purpose, demand?, procedure?, analysis?, conclusion?, includeSessionSummary }` → 201 (rascunho) |
| GET | `/psychological-reports?patientId=` | lista do psicólogo logado |
| GET | `/psychological-reports/{id}` | `PsychologicalReportResponse` |
| PUT | `/psychological-reports/{id}` | edita o rascunho (finalizado → 409) |
| POST | `/psychological-reports/{id}/finalize` | exige todas as seções; depois não pode ser editado |
| DELETE | `/psychological-reports/{id}` | exclui rascunho |
| GET | `/psychological-reports/{id}/pdf` | PDF (rascunho sai com marca d'água "RASCUNHO") |

## Prontuários (RF019, RF020)
Somente psicólogos; cada registro é acessível apenas ao psicólogo autor (sigilo, D-02). Leituras, downloads e tentativas
negadas (403) ficam registrados em `medical_record_access_logs` (DT-24).

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/medical-records` | `{ patientId, title, content }` → 201 |
| GET | `/medical-records?patientId=&search=&from=&to=&page=&pageSize=` | busca por paciente, período e palavra-chave (título/conteúdo) |
| GET | `/medical-records/{id}` | registro + anexos |
| PUT | `/medical-records/{id}` | `{ title, content }` |
| DELETE | `/medical-records/{id}` | exclusão lógica |
| POST | `/medical-records/{id}/attachments` | `multipart/form-data`, campo `file`: PDF/JPG/PNG até 10 MB (validado pela assinatura do arquivo) → 201 |
| GET | `/medical-records/{id}/attachments/{attachmentId}` | download |
| DELETE | `/medical-records/{id}/attachments/{attachmentId}` | remove o anexo |
| GET | `/medical-records/{id}/access-log` | trilha de acessos (mais recentes primeiro): `[{ at, userId, userName, action (Viewed, AttachmentDownloaded, Denied), attachmentId? }]` |

## Assistente de IA (RF021)
Provedores: **Claude, OpenAI e Gemini**, com chaves só no servidor (`Ai__Claude__ApiKey`, `Ai__OpenAi__ApiKey`, `Ai__Gemini__ApiKey`).
Os dados enviados são **pseudonimizados** (sem nome, CPF, e-mail, telefone ou endereço; idade no lugar da data de nascimento).

| Método | Rota | Quem | Descrição |
|--------|------|------|-----------|
| GET | `/ai/settings` | todos | `AiSettingsResponse` (sem registro = desabilitada) |
| PUT | `/ai/settings` | Admin/Manager | `{ isEnabled, provider (Claude, OpenAi, Gemini), shareSessionNotes, shareFeedbacks, shareMedicalRecords, acceptTerms }`. Habilitar exige `acceptTerms = true` (422), ao menos um dado liberado (`ai.no_data_shared`) e provedor com chave no servidor (422 em `provider`). Desabilitar revoga o aceite. |
| POST | `/ai/suggestions/session-notes` | psicólogo da sessão | `{ sessionId, draft? }` → registro de evolução organizado a partir do rascunho (ou das notas salvas); usa as 3 sessões anteriores como contexto |
| POST | `/ai/suggestions/patient-analysis` | psicólogo | `{ patientId }` → análise do acompanhamento |
| POST | `/ai/suggestions/next-steps` | psicólogo | `{ patientId }` → sugestões de próximos passos |

`AiSettingsResponse`: `{ isEnabled, provider, shareSessionNotes, shareFeedbacks, shareMedicalRecords, consentAcceptedAt?, availableProviders[] }`.
`AiSuggestionResponse`: `{ suggestion (Markdown), provider, model, generatedAt, disclaimer }`.

Contexto das sugestões: só sessões concluídas e prontuários **do psicólogo logado** com o paciente, e só os tipos de dado liberados pela
clínica (últimas 10 sessões e 10 registros; textos cortados em 4.000 caracteres). Erros com `code`: `ai.disabled` (403),
`ai.only_psychologists` (403), `ai.not_your_session` (403), `ai.data_not_shared` (403), `ai.empty_draft` (422),
`ai.insufficient_data` (422), `ai.refused` (422, recusa do provedor), `ai.provider_failed` / `ai.provider_unavailable` (503).
Cada sugestão gerada fica registrada em `ai_usage_logs` (sem o conteúdo).

## Painel (RC-06)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/dashboard?psychologistId=` | Psicólogo: a própria agenda. Admin/Manager: a clínica inteira ou o psicólogo informado (outro psicólogo → 403). |

`DashboardResponse`: `{ today, weekStart, weekEnd (segunda a domingo), activePatients, sessionsToday, confirmedToday, sessionsThisWeek,
pendingConfirmations, finance: { pendingPayments, pendingAmount (sessões concluídas não pagas), receivedThisMonth },
todayItems: [{ scheduleId, psychologistId, type, status, startTime, endTime, blockReason?, sessionId?, patientId?, patientName?, sessionStatus? }],
upcoming: [{ sessionId, psychologistId, patientId, patientName, date, startTime, endTime, scheduleStatus }] (até 6, a partir de agora) }`.

## Limite de requisições (DT-23)
Janela fixa configurável em `RateLimiting:<Política>` (`PermitLimit`, `WindowSeconds`). Ao estourar: **429** em ProblemDetails com
`code: "rate_limit.exceeded"` e cabeçalho `Retry-After` (segundos).

| Política | Onde | Chave | Padrão |
|----------|------|-------|--------|
| `auth` | `POST /auth/login`, `POST /auth/register` | IP | 10 por minuto |
| `ai` | `POST /ai/suggestions/*` | usuário | 20 por minuto |

Atrás de proxy reverso, configure os cabeçalhos encaminhados (`X-Forwarded-For`) para que o IP real seja usado.

## Infra
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | público; `Healthy` quando a API e o banco respondem |

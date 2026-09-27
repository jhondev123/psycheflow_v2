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
| POST | `/sessions` | `{ patientId, date, startTime, durationMinutes?, psychologistId?, notes? }` → 201. Duração padrão da empresa; 15–240 min. Regras: não no passado, dentro do expediente, sem conflito, paciente ativo. |
| GET | `/sessions?from=&to=&timeFrom=&timeTo=&patientId=&psychologistId=&status=&page=&pageSize=` | `from` obrigatório. `PagedResponse<SessionListItem>` |
| GET | `/sessions/{id}` | `SessionResponse` (anotações/feedback só para o psicólogo da sessão) |
| PUT | `/sessions/{id}` | `{ patientId, notes? }` (`notes` nulo mantém; só o psicólogo da sessão altera notas) |
| POST | `/sessions/{id}/confirm` | confirma o horário |
| POST | `/sessions/{id}/reschedule` | `{ date, startTime, reason, durationMinutes? }` (mantém a duração se omitida) |
| POST | `/sessions/{id}/cancel` | `{ reason }` — libera o horário |
| POST | `/sessions/{id}/complete` | `{ notes, feedbackScore (0–10), feedbackComment? }` — só o psicólogo da sessão, após o início |
| POST | `/sessions/{id}/no-show` | falta, após o início |
| DELETE | `/sessions/{id}` | exclusão lógica (não permitida para concluída) |

Sessão concluída, cancelada ou com falta não aceita mais alterações (409). Psicólogo só acessa as próprias sessões (403);
Admin/Manager acessam todas da empresa, sem ver anotações/feedback.

`SessionResponse`: `{ id, scheduleId, psychologistId, psychologistName, patientId, patientName, date, startTime, endTime, durationMinutes,
status, scheduleStatus, notes?, feedbackScore?, feedbackComment?, cancellationReason?, rescheduleReason?, clinicalNotesVisible, createdAt, updatedAt? }`.

## Infra
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | público; `Healthy` quando a API e o banco respondem |

---

## Endpoints previstos (fases seguintes)

| Fase | Método | Rota | Requisito |
|------|--------|------|-----------|
| Financeiro | GET | `/payments?patientId=&from=&to=&status=` | RF012 |
| Financeiro | POST | `/payments/{id}/pay` · `/payments/{id}/cancel` | RF011, RN-44 |
| Financeiro | PUT | `/payments/{id}` | RF013 |
| Financeiro | POST | `/recurrences` | RF010 |
| Documentos | GET | `/sessions/{id}/receipt` (PDF) | RF014 |
| Documentos | GET | `/reports/sessions?…` · `/reports/feedback?patientId=&from=&to=` (PDF) | RF015, RF015-B |
| Documentos | POST/GET | `/psychological-reports` (+ `/{id}/pdf`) | RF016 |
| Prontuários | CRUD | `/medical-records` (+ anexos) | RF019, RF020 |
| IA | GET/PUT/POST | `/ai/settings`, `/ai/suggestions` | RF021 |
| Painel | GET | `/dashboard` (resumo do dia/semana) | RC-06 |

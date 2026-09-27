# 04 — Regras de negócio (RN)

Regras atômicas extraídas dos requisitos e casos de uso. Os outros documentos referenciam estes IDs.
Status por camada (coluna API atualizada em 27/09/2026, após a reestruturação): **API** / **Front** (legenda em `README.md`). "Onde" aponta o arquivo que implementa hoje.

## Transversais

| ID | Regra | Origem | API | Front | Onde / observação |
|----|-------|--------|-----|-------|-------------------|
| RN-01 | Todo dado pertence a uma empresa; o usuário só lê/escreve dados da própria `CompanyId` | RD002, RNF001 | ✅ | 🟡 | Filtro global `Tenant` em toda entidade de negócio + testes de isolamento por recurso. |
| RN-02 | Exclusão é lógica (`DeletedAt`); registros excluídos não aparecem em listas nem contam em validações | Modelo de dados | ✅ | ❌ | Filtro global `SoftDelete`; `Remove()` vira UPDATE de `deleted_at` (`AuditingInterceptor`). |
| RN-03 | `UpdatedAt` é gravado em toda alteração | UC2 | ✅ | ❌ | `AuditingInterceptor` preenche `updated_at` (UTC) em toda alteração. |
| RN-04 | Mensagens de erro claras e operação cancelada (rollback) em caso de falha | UCs (fluxos alternativos) | ✅ | ✅ | `Result`/`Error` → ProblemDetails com mensagem pt-BR; escritas múltiplas em transação. |

## Autenticação e usuários

| ID | Regra | Origem | API | Front | Onde / observação |
|----|-------|--------|-----|-------|-------------------|
| RN-10 | Login por e-mail + senha | RF022 | ✅ | 🧪 | `UserController.Login` → JWT 2h. Front compara com `db.credentials`. |
| RN-11 | Senha: mín. 6 caracteres, com maiúscula, minúscula, número e símbolo | Identity default | ✅ | 🧪 | Front: `lib/validation.ts#passwordIssues`. |
| RN-12 | Após várias tentativas erradas, bloqueio temporário | UC23 | ✅ | ❌ | 5 tentativas → bloqueio de 15 min (HTTP 423). |
| RN-13 | E-mail único | Identity | ✅ | 🧪 | |
| RN-14 | Psicólogo exige número do CRP | RF (cadastro) | ✅ | 🧪 | CRP no formato `06/12345` (D-03), VO `LicenseNumber`. |
| RN-15 | Logout encerra a sessão e volta ao login | RF023 | ✅ | 🧪 | JWT é stateless: logout = front descartar o token. Não há endpoint (e não precisa, KISS). |

## Pacientes

| ID | Regra | Origem | API | Front | Onde / observação |
|----|-------|--------|-----|-------|-------------------|
| RN-20 | Obrigatórios: nome, CPF, telefone, e-mail | RF001 | ✅ | 🟡 | Front exige só nome e e-mail; CPF/telefone opcionais. API não tem CRUD de paciente. |
| RN-21 | CPF válido (11 dígitos, DV correto, não todos iguais) | RF001 | ✅ | ✅ | API: VO `Cpf` existe, **mas não é usado** (Patient não tem coluna CPF). |
| RN-22 | CPF único por empresa (paciente não pode ter "outra conta") | UC1 | ✅ | ❌ | Índice único (company_id, cpf) filtrado por não excluídos + checagem prévia (409). |
| RN-23 | E-mail válido | RF001/RF003 | ✅ | ✅ | API só valida via Identity (se for criado como User). |
| RN-24 | Telefone válido (padrão BR `(99) 99999-9999`) | RF001/RF003 | ✅ | ✅ | API: VO `Phone` existe, não usado. |
| RN-25 | Novo paciente nasce com status **Ativo** | RF001 | ✅ | ❌ | Não existe campo status. |
| RN-26 | Endereço: CEP é o 1º campo e preenche rua/bairro/cidade/estado automaticamente | RF001 | ✅ | ❌ | O front consulta o ViaCEP; a API valida CEP (8 dígitos) e UF. |
| RN-27 | CPF não é editável; editáveis: nome, telefone, e-mail, endereço, nascimento, status | RF003 | ✅ | 🟡 | Front permite editar CPF. |

## Agenda, sessões e bloqueios

| ID | Regra | Origem | API | Front | Onde / observação |
|----|-------|--------|-----|-------|-------------------|
| RN-30 | Data **e horário** do agendamento não podem ser anteriores ao momento atual | RF004 | ✅ | 🟡 | `TimeSlot.StartsBefore(agora local da clínica)` — data e hora. |
| RN-31 | Horário final > horário inicial | RF004 | ✅ | ✅ | `TimeSlot` exige fim > início; CHECK no banco. |
| RN-32 | Duração padrão vem de configuração; mínimo **15 min** | RF004 | ✅ | ❌ | `company_settings.session_duration_minutes` (padrão 50) + validação 15–240. |
| RN-33 | Sessão só pode ser marcada dentro do expediente do psicólogo (`PsychologistHours` do dia da semana; o intervalo precisa caber inteiro numa faixa) | RF017 / regra do código | ✅ | ✅ | `Psychologist.WorksAt` (o intervalo precisa caber inteiro numa faixa). |
| RN-34 | Não pode haver sobreposição com outro agendamento **não cancelado** do mesmo psicólogo | RF017/RF018 | ✅ | ✅ | `ScheduleAvailability` ignora cancelados/excluídos; advisory lock do Postgres contra corrida. |
| RN-35 | Sessão exige paciente já cadastrado | RF004 | ✅ | ✅ | Paciente precisa existir na empresa e estar ativo. |
| RN-36 | Toda sessão nasce de um agendamento (`Schedule` tipo SESSION) — relação 1:1 | Modelo | ✅ | ✅ | |
| RN-37 | Bloqueio não vira sessão; impede agendamentos no intervalo | RF018 | ✅ | ✅ | |
| RN-38 | Bloqueio não pode conflitar com sessões já agendadas | UC19 | ✅ | ✅ | Coberto por RN-34. |
| RN-39 | Bloqueio pode ser por **dias inteiros** ou **horários**; motivo opcional | RF018 | ✅ | 🟡 | Dias inteiros (00:00–23:59, um bloqueio por dia) ou faixa de horário; motivo opcional. |
| RN-40 | Bloqueio pode ficar fora do expediente? | divergência | ✅ | ✅ | Decidido: bloqueio ignora o expediente. |
| RN-41 | Reagendar, cancelar, editar e concluir só se a sessão **não** estiver concluída nem cancelada | UC6–UC9 | ✅ | 🟡 | `Session.IsOpen`: só sessões agendadas aceitam alterações (409). |
| RN-42 | Reagendamento exige nova data, horário e **motivo**; reaplica RN-30/33/34 | RF006 | ✅ | 🟡 | `POST /sessions/{id}/reschedule` com motivo; reaplica RN-30/33/34 ignorando o próprio horário. |
| RN-43 | Cancelamento exige **motivo**; cancela também o pagamento da sessão | RF007/UC7 | ✅ | 🟡 | Motivo obrigatório; o pagamento pendente da sessão é cancelado junto. |
| RN-44 | Se o pagamento já estiver **Pago**, é preciso cancelar o pagamento antes de cancelar a sessão | UC7 | ✅ | ❌ | Garantida: só sessões concluídas são pagas (RN-55) e concluídas não são canceladas (RN-41); o handler também bloqueia. |
| RN-45 | Concluir exige **anotações** e **feedback nota 0–10** | RF009 | ✅ | 🟡 | Anotações obrigatórias + `feedback_score` 0–10 (CHECK no banco). |
| RN-46 | Anotações da sessão em **Markdown** | RF004 | ✅ | ❌ | `sessions.notes` guarda Markdown; o front renderiza. |
| RN-47 | Paciente pode ser marcado como **falta** (NoShow) | Front RF24 | ✅ | 🧪 | `POST /sessions/{id}/no-show`, só após o início. |
| RN-48 | Recorrência: tipo **semanal** ou **mensal** + valor da sessão; gera as sessões futuras respeitando RN-33/34 | RF010 | ✅ | ❌ | `RecurrencePattern` (semanal/mensal, último dia do mês quando necessário) + `RecurrenceGenerator` aplicando RN-30/33/34. |

## Financeiro

| ID | Regra | Origem | API | Front | Observação |
|----|-------|--------|-----|-------|------------|
| RN-50 | Ao criar uma sessão, cria-se um pagamento **Pendente** | UC4 | ✅ | ❌ | `SessionAccess.AddWithPayment`: toda sessão (avulsa ou de recorrência) nasce com pagamento Pendente. |
| RN-51 | Valor da sessão vem da recorrência ou de configuração | RF011 | ✅ | ❌ | Valor informado → valor da recorrência → `company_settings.session_default_price` → 0. |
| RN-52 | Métodos: Cartão de crédito, Débito, Pix | RF011 | ✅ | ❌ | `PaymentMethod`: CreditCard, DebitCard, Pix. |
| RN-53 | Data do pagamento não pode ser menor que hoje | RF011 | ✅ | ❌ | `Payment.Pay` compara com a data de hoje no fuso da clínica. |
| RN-54 | Status do pagamento: Pendente, Pago, Cancelado | RF012 | ✅ | ❌ | `PaymentStatus`: Pending, Paid, Cancelled. |
| RN-55 | Lançar pagamento exige sessão **concluída** | UC11 | ✅ | ❌ | "Lançar" = `POST /payments/{id}/pay`, exige sessão Completed (409). |
| RN-56 | Pagamento com status Pago não pode ser editado (editáveis: valor e status) | RF013 | ✅ | ❌ | `PUT /payments/{id}` só altera valor de pendente (409 se pago). |
| RN-57 | Recibo só pode ser gerado se o pagamento estiver Pago | RF014 | ✅ | ❌ | `GET /documents/receipts/{paymentId}` → 409 se o pagamento não estiver Pago. |

## Documentos, prontuários e IA

| ID | Regra | Origem | API | Front | Observação |
|----|-------|--------|-----|-------|------------|
| RN-60 | Modelo de documento tem campos com ordem, obrigatoriedade e valor padrão; campo obrigatório sem valor impede a geração | Código (base de RF014/RF016) | ❌ (removido) | 🧪 | Motor FastReport removido; documentos serão classes QuestPDF (fase Documentos). |
| RN-61 | Modelos podem ser globais (`CompanyId = null`) ou da empresa | Código | ❌ (removido) | ❌ | Idem RN-60. |
| RN-62 | Laudo exige paciente, motivo e modelo; pode incluir dados de sessões e anotações | RF016 | ✅ | ❌ | Paciente, modelo e finalidade obrigatórios; `includeSessionSummary` resume as sessões concluídas no procedimento. |
| RN-63 | Relatório de sessões: filtros período, status da sessão, status do pagamento | RF015 | ✅ | ❌ | `GET /documents/sessions-report` com período (início obrigatório), status da sessão e do pagamento. |
| RN-64 | Relatório de feedback: paciente obrigatório + período (fim opcional) | RF015(b) | ✅ | ❌ | `GET /documents/feedback-report`: paciente e início obrigatórios; fim opcional. |
| RN-65 | Prontuário exige paciente; aceita texto e anexos (PDF/imagem) | RF019 | ❌ | ❌ | |
| RN-66 | Prontuários: busca por paciente, data de criação e palavra-chave; acesso só a profissionais autorizados | RF020, RD002 | ❌ | ❌ | |
| RN-67 | IA só funciona com consentimento explícito do psicólogo e sobre os tipos de dado que ele liberar (feedbacks, sessões, prontuários, laudos); revogar desativa | RF021 | ❌ | ❌ | Tabelas `Config`/`ConfigAi` já existem (`ConfigKey.EnableAI`). |
| RN-68 | Horários de trabalho: início < fim; salvar substitui todas as faixas do psicólogo | Código (RF017) | ✅ | 🧪 | `PsychologistController.StoreWorkingHours`. |

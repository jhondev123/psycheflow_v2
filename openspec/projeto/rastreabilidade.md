# 02 — Requisitos (consolidado com status)

Fonte oficial: `docs/Documento requisitos Atualizado.docx` (IDs preservados).
Status por camada — legenda em `README.md`. Regras detalhadas: `04-regras-de-negocio.md` (RN-xx).

> **Ajustes de numeração** feitos nesta consolidação (o .docx original tem IDs repetidos):
> - Havia dois `RF015`. O de relatório de feedback virou **RF015-B**.
> - Os três `RF000` (notificações) viraram **RF024, RF025, RF026**.
> - Funcionalidades que existem no código mas não estão no documento oficial viraram **RC-xx** (requisitos complementares).

## Resumo

| ID | Requisito | Prioridade | API | Front | UC |
|----|-----------|-----------|-----|-------|----|
| RF001 | Cadastro de pacientes | Prioritário | ✅ | ✅ | UC01 |
| RF002 | Visualização/filtro de pacientes | Prioritário | ✅ | ✅ | UC03 |
| RF003 | Edição de pacientes | Prioritário | ✅ | ✅ | UC02 |
| RF004 | Cadastro de sessões | Prioritário | ✅ | ✅ | UC04 |
| RF005 | Visualização de sessões (calendário + filtros) | Prioritário | ✅ | ✅ | UC05, UC18 |
| RF006 | Reagendamento de sessões | Prioritário | ✅ | ✅ | UC06 |
| RF007 | Cancelamento de sessões | Prioritário | ✅ | ✅ | UC07 |
| RF008 | Edição de sessões | Prioritário | ✅ | ✅ | UC09 |
| RF009 | Conclusão da sessão | Prioritário | ✅ | ✅ | UC08 |
| RF010 | Recorrência de sessão | Prioritário | ✅ | ✅ | UC10 |
| RF011 | Lançamento de pagamento | Prioritário | ✅ | ✅ | UC11 |
| RF012 | Visualização de pagamentos | Prioritário | ✅ | ✅ | UC12 |
| RF013 | Edição de pagamentos | Prioritário | ✅ | ✅ | UC13 |
| RF014 | Recibos de sessões | Prioritário | ✅ | ✅ | UC14 |
| RF015 | Relatório de sessões | Prioritário | ✅ | ✅ | UC15 |
| RF015-B | Relatório de feedback de pacientes | Prioritário | ✅ | ✅ | UC16 |
| RF016 | Geração de laudos | Prioritário | ✅ | ✅ | UC17 |
| RF017 | Gerenciamento da agenda | Prioritário | ✅ | ✅ | UC18 |
| RF018 | Bloqueio de agenda | Prioritário | ✅ | ✅ | UC19 |
| RF019 | Criação de prontuários | Prioritário | ✅ | ✅ | UC20 |
| RF020 | Gerenciamento de prontuários | Prioritário | ✅ | ✅ | UC21 |
| RF021 | Sugestões com IA | Prioritário* | ✅ | ✅ | UC22 |
| RF022 | Login do psicólogo | Prioritário | ✅ | ✅ | UC23 |
| RF023 | Logout | Prioritário | ✅ | ✅ | UC24 |
| RF024 | Notificação de sessões para o paciente | Desejável | ❌ | ❌ | — |
| RF025 | Notificação de sessões para o psicólogo | Desejável | ❌ | ❌ | — |
| RF026 | Notificação de conflitos de agendamento | Desejável | ❌ | ❌ | — |
| RC-01 | Cadastro de conta (empresa + usuário responsável) | Complementar | ✅ | ✅ | UC25 |
| RC-02 | Cadastro de usuários da empresa por Admin/Gerente | Complementar | ✅ | ✅ | UC26 |
| RC-03 | Horários de trabalho do psicólogo | Complementar | ✅ | ✅ | UC27 |
| RC-04 | Perfil do profissional (nome, CRP, abordagem, telefone) | Complementar | ✅ | ✅ | UC28 |
| RC-05 | Modelos de documento (cadastro e geração em PDF) | Complementar | ❌ (removido) | ❌ (removido) | UC29 |
| RC-06 | Painel inicial (resumo do dia/semana) | Complementar | ✅ | ✅ | — |
| RC-07 | Tema claro/escuro | Complementar | — | ✅ | — |
| RC-08 | Health check da API | Complementar | ✅ | ✅ | — |

> Colunas atualizadas em 28/09/2026: a API foi reestruturada (`08-plano-reestruturacao.md`) e o front passou a consumir a API — o mock em localStorage foi removido (D-09).

\* RF021 está marcado como prioritário no documento, mas depende de quase tudo (sessões, prontuários). Foi implementado por último na API (fase de IA, ver `08`).

---

## Requisitos funcionais — detalhe

### RF001 — Cadastro de pacientes
- **Campos:** Nome\*, CPF\*, Endereço (CEP primeiro → autopreenche Rua, Bairro, Cidade, Estado; + Número, Complemento), Data de nascimento, Telefone\*, E-mail\*, Status.
- **Regras:** RN-20, RN-21, RN-22, RN-23, RN-24, RN-25, RN-26.
- **API ✅** — `POST /api/v1/patients`: nome, CPF (VO com dígitos verificadores), e-mail e telefone obrigatórios; endereço opcional (CEP 8 dígitos, UF); nasce Ativo; CPF único por empresa (409).
- **Front ✅** — `components/PatientForm.tsx`: nome, CPF (validado), e-mail e telefone obrigatórios; endereço com CEP que preenche rua, bairro, cidade e UF (ViaCEP); erros da API aparecem no campo (ex.: CPF já cadastrado).

### RF002 — Visualização de pacientes
- **Filtros:** Status, data da última sessão (se houver), nome.
- **API ✅** — `GET /api/v1/patients` com busca (nome/e-mail/CPF), status, período da última sessão concluída e paginação.
- **Front ✅** — `pages/Patients.tsx`: busca (nome, e-mail ou CPF completo), filtros por situação e por data da última sessão, paginação; ficha do paciente em `/pacientes/:id`.

### RF003 — Edição de pacientes
- **Editáveis:** Nome, Telefone, E-mail, Endereço, Data de nascimento, Status. Regras: RN-23, RN-24, RN-27, RN-03.
- **API ✅** (`PUT /api/v1/patients/{id}`, CPF fora do request) · **Front ✅** — mesmo formulário, com CPF somente leitura e situação Ativo/Inativo (não há exclusão de paciente).

### RF004 — Cadastro de sessões
- **Campos:** Data\*, Horário\*, Duração\* (padrão de configuração), Paciente\*, Anotações (Markdown).
- **Regras:** RN-30, RN-31, RN-32, RN-33, RN-34, RN-35, RN-36, RN-46, RN-50.
- **API ✅** — `POST /api/v1/sessions`: data, horário, duração opcional (padrão da empresa, 15–240 min), paciente ativo, anotações em Markdown; valida passado, expediente e conflito. Cria o pagamento pendente (RN-50).
- **Front ✅** — modal “Novo agendamento” da Agenda: paciente, data, início, duração (padrão da clínica), valor e opções de já confirmar ou repetir (recorrência).

### RF005 — Visualização de sessões
- **Exibição:** calendário. **Filtros:** período (início obrigatório, fim opcional), horário, paciente. Menciona reagendar/cancelar e lembretes automáticos (lembretes → RF024).
- **API ✅** — `GET /api/v1/sessions` (período com início obrigatório, horário, paciente, psicólogo, status) e `GET /api/v1/agenda` (visão de calendário).
- **Front ✅** — Agenda (mês/semana/dia) e página Sessões com período, situação, paciente e psicólogo (gestão).

### RF006 — Reagendamento de sessões
- **Campos:** Nova data\*, Horário\*, Motivo\*. Regras: RN-41, RN-42.
- **API ✅** (`POST /api/v1/sessions/{id}/reschedule`, motivo obrigatório) · **Front ✅** — “Reagendar” no modal da sessão (nova data, horário, duração e motivo).

### RF007 — Cancelamento de sessões
- **Campos:** Motivo\*. Regras: RN-41, RN-43, RN-44.
- **API ✅** — `POST /api/v1/sessions/{id}/cancel` com motivo; libera o horário. Cancela o pagamento pendente (RN-43).
- **Front ✅** — “Cancelar” no modal da sessão pede o motivo.

### RF008 — Edição de sessões
- **Editáveis:** Anotações, Cancelamento, Exclusão, Data, Horário, Duração, Paciente. Regra: RN-41.
- **API ✅** — `PUT /api/v1/sessions/{id}` (paciente e anotações), reagendamento (data/horário/duração), cancelamento e `DELETE` lógico que libera o horário (sessão concluída não pode ser excluída).
- **Front ✅** — modal da sessão (`components/SessionModal.tsx`): editar anotações (psicólogo da sessão), confirmar, reagendar, cancelar, falta e excluir.

### RF009 — Conclusão da sessão
- **Campos:** Anotações\*, Feedback (nota 0–10)\*. Regras: RN-41, RN-45.
- **API ✅** — `POST /api/v1/sessions/{id}/complete`: anotações obrigatórias + nota 0–10; só o psicólogo da sessão e após o início. Também `POST /sessions/{id}/no-show`.
- **Front ✅** — “Concluir” com anotações em Markdown, nota de 0 a 10 e comentário do paciente; opção de organizar o rascunho com IA.

### RF010 — Recorrência de sessão
- **Campos:** Tipo (Semanal, Mensal)\*, valor das sessões. Acionada a partir do paciente. Regras: RN-48, RN-51.
- **API ✅** — `POST /api/v1/recurrences` (semanal/mensal, valor, data final opcional): gera 3 meses de sessões pulando datas indisponíveis e informando o motivo; `/extend` gera mais 3 meses; `/end` encerra e cancela as futuras. **Front ✅** (opção “Repetir” no agendamento; estender/encerrar na ficha do paciente).

### RF011 — Lançamento do pagamento
- **Campos:** Valor\* (de configuração/recorrência), Método\* (Crédito, Débito, Pix), Data (≥ hoje), Informações extras. Regras: RN-50 a RN-55.
- **API ✅** — pagamento nasce pendente com a sessão (valor da sessão/recorrência/empresa); `POST /api/v1/payments/{id}/pay` registra método (crédito, débito, Pix), data (≥ hoje) e valor, só após a sessão concluída. **Front ✅** (“Receber” no modal da sessão, na ficha do paciente e no Financeiro).

### RF012 — Visualização de pagamentos
- **Filtros:** Paciente, período (fim opcional), status. **API ✅** (`GET /api/v1/payments`) · **Front ✅** (página Financeiro).

### RF013 — Edição de pagamentos
- **Editáveis:** Valor, Status. Regra RN-56. **API ✅** (`PUT /payments/{id}` valor; status por `/pay` e `/cancel`; pago não edita) · **Front ✅** (alterar valor; cancelar/estornar com motivo).

### RF014 — Recibos de sessões
- **Entrada:** Sessão. Regra RN-57. Saída: recibo para download/impressão.
- **API ✅** — `GET /api/v1/documents/receipts/{paymentId}`: PDF (QuestPDF) com valor por extenso, só para pagamento recebido (RN-57). Extra: declaração de comparecimento `GET /documents/attendance/{sessionId}`. **Front ✅** (botões Recibo e Declaração).

### RF015 — Relatório de sessões
- **Conteúdo:** dados da sessão (data, horário, paciente, status, observações) + pagamento (valor, status, método). **Filtros:** período, status da sessão, status do pagamento. Regra RN-63.
- **API ✅** (`GET /api/v1/documents/sessions-report?from&to&sessionStatus&paymentStatus&psychologistId`, PDF com totais; observações só das sessões do próprio psicólogo) · **Front ✅** (Documentos e Financeiro).
- Nota: a sessão tem as situações Agendada, Concluída, Cancelada e Falta (`SessionStatus`, D-04).

### RF015-B — Relatório de feedback de pacientes
- **Conteúdo:** dados do paciente + feedbacks agrupados por sessão (data/horário). **Filtros:** Paciente\*, período (fim opcional). Regra RN-64. **API ✅** (`GET /api/v1/documents/feedback-report`, só sessões do psicólogo logado, com média) · **Front ✅** (Documentos e ficha do paciente).

### RF016 — Geração de laudos
- **Campos:** Paciente\*, Motivo\*, Modelo de laudo\* (sistema fornece modelos). Baseado nas anotações das sessões; formato personalizável. Regra RN-62.
- **API ✅** — `/api/v1/psychological-reports`: laudo ou relatório psicológico (modelos da Resolução CFP 06/2019) com rascunho editável, finalização (todas as seções obrigatórias) e PDF; opção de incluir resumo das sessões. Só o psicólogo autor acessa. **Front ✅** (Documentos: criar, editar rascunho, finalizar e baixar o PDF).

### RF017 — Gerenciamento da agenda
- Visualizar todas as atividades (sessões, bloqueios, horários livres), modos diário/semanal/mensal, clicar num horário para agendar ou bloquear.
- **API ✅** — `GET /api/v1/agenda?from&to&psychologistId` (até 62 dias): sessões, bloqueios e expediente; o psicólogo vê a própria agenda, a gestão vê todas.
- **Front ✅** — `pages/Agenda.tsx`: calendário mês/semana/dia com expediente destacado, clique no horário para agendar ou bloquear, cores por situação, sobreposição lado a lado (`lib/calendar.ts#packDay`) e seleção de psicólogo para a gestão.

### RF018 — Bloqueio de agenda
- **Campos:** Tipo de intervalo (dias ou horários)\*, valores do intervalo\*, motivo. Regras: RN-37 a RN-40.
- **API ✅** — `POST /api/v1/schedule-blocks`: dias inteiros ou faixa de horário num intervalo de até 31 dias, motivo opcional, ignora o expediente; `DELETE` libera o horário.
- **Front ✅** — bloqueio por faixa de horário ou por dias inteiros num intervalo de datas, com motivo; remoção pelo detalhe.

### RF019 — Criação de prontuários
- **Campos:** Paciente\*, informações (texto + anexos PDF/imagem). Regra RN-65. **API ✅** (`POST /api/v1/medical-records` + `POST /medical-records/{id}/attachments` — PDF/JPG/PNG até 10 MB validados pela assinatura do arquivo, gravados via `IFileStorage` em volume, D-06) · **Front ✅** (Prontuários: novo registro e anexos).

### RF020 — Gerenciamento de prontuários
- Armazenamento seguro, anexos, anotações, busca por paciente/data/palavra-chave; visualizar, editar, excluir. Regra RN-66. **API ✅** (busca, detalhe, edição, exclusão lógica, download/remoção de anexos; só o psicólogo autor acessa) · **Front ✅** (busca, edição, exclusão, anexos e trilha de acessos).

### RF021 — Sugestões com IA
- Opt-in do psicólogo + escolha dos dados permitidos (feedbacks, sessões, prontuários, laudos). Sugestões em: registro de sessões, laudos, relatórios, padrões de comportamento. Regra RN-67.
- **API ✅** — provedores **Claude, OpenAI e Gemini** atrás de uma porta genérica (`IAiTextGenerator`), chaves só no servidor. A clínica (Admin/Manager) habilita, escolhe o provedor e os dados liberados (anotações de sessão, feedbacks, prontuários) e registra o aceite (`ai_settings`, D-07). Sugestões para o psicólogo: **anotações da sessão** (registro de evolução a partir do rascunho), **análise do paciente** e **próximos passos** — prompt padrão em pt-BR, dados **pseudonimizados** (sem nome, CPF, e-mail, telefone, endereço; idade no lugar da data de nascimento) e auditoria em `ai_usage_logs`. Laudos/relatórios ficaram fora por decisão do dev. **Front ✅** (Configurações › Assistente de IA; botões na sessão e na ficha do paciente).

### RF022 — Login do psicólogo
- **Campos:** usuário (e-mail) e senha. Regras: RN-10, RN-11, RN-12.
- **API ✅** — `POST /api/v1/auth/login` → JWT, com bloqueio após 5 tentativas (RN-12).
- **Front ✅** — `pages/Login.tsx` com a API (JWT); troca obrigatória da senha temporária no primeiro acesso (`pages/ChangePassword.tsx`).

### RF023 — Logout
- **API ✅** (JWT stateless — não precisa endpoint). **Front ✅** — “Sair” descarta o token; token expirado leva ao login.

### RF024 / RF025 / RF026 — Notificações (Desejável)
- Sem descrição no documento original. Sugestão mínima (KISS): e-mail de lembrete D-1 para paciente (RF024), resumo diário para o psicólogo (RF025) e aviso na tela ao tentar agendar em conflito (RF026 — já coberto pela mensagem de erro de RN-34). **API ❌ · Front ❌.**

---

## Requisitos complementares (existem no código, não no documento oficial)

| ID | Descrição | API | Front | Evidência |
|----|-----------|-----|-------|-----------|
| RC-01 | Criar conta: empresa + usuário responsável | ✅ `POST /api/v1/auth/register` cria empresa + usuário Admin/Psicólogo + perfil numa transação | ✅ `Register.tsx` cria a clínica e o responsável pela API | |
| RC-02 | Admin/Gerente cria usuários da empresa (senha gerada se vazia) | ✅ `POST /api/v1/users` com senha temporária e troca obrigatória | ✅ `Users.tsx` (mostra a senha temporária uma única vez) | `PasswordGeneratorService` |
| RC-03 | Horários de trabalho por dia da semana (várias faixas) | ✅ `PUT /api/v1/psychologists/{id}/working-hours` (várias faixas, sem sobreposição) | ✅ `WorkingHours.tsx` (várias faixas por dia) | RN-68 |
| RC-04 | Perfil profissional: nome, telefone, CRP, abordagem | ✅ `PUT /api/v1/psychologists/{id}` (nome, telefone, CRP, abordagem) | ✅ `Profile.tsx` | |
| RC-05 | Modelos de documento com campos (obrigatório, padrão, ordem) e geração de PDF | ❌ removido (FastReport); documentos voltam com QuestPDF | ❌ removido (documentos fixos: `Documents.tsx`) | RN-60, RN-61 |
| RC-06 | Painel: atendimentos de hoje, pendentes, semana, próximos | ✅ `GET /api/v1/dashboard` (contadores do dia/semana, agenda de hoje, próximos atendimentos e financeiro a receber/recebido no mês) | ✅ `Dashboard.tsx` com `GET /dashboard` | |
| RC-07 | Tema claro/escuro persistido | — | ✅ | |
| RC-08 | Health check (`GET /Api/healthcheck`) | ✅ `GET /health` (verifica o banco) | — | |

---

## Requisitos não funcionais

| ID | Requisito | Status | Situação atual / gap |
|----|-----------|--------|----------------------|
| RNF001 | Segurança: criptografia, autenticação, LGPD | 🟡 | ✅ Resolvido na reestruturação: todas as rotas autenticadas por padrão (fallback policy), isolamento por empresa em filtro global, lockout, segredos fora do repositório, senha temporária com troca obrigatória, sigilo das anotações clínicas. Falta: criptografia em repouso de dados clínicos, trilha de auditoria/consentimento LGPD, HTTPS no deploy. |
| RNF002 | Desempenho: resposta < 2 s | 🟡 | Listas paginadas, agenda limitada a 62 dias, índices por (psicólogo, data), (empresa, CPF) e (paciente, status). Não medido formalmente. |
| RNF003 | Usabilidade: interface intuitiva e responsiva (desktop e mobile) | ✅ (front) | Layout responsivo, pt-BR, tema claro/escuro, toasts, estados vazios. |
| RNF004 | Disponibilidade 99,9%, backups automáticos | ❌ | Sem deploy/infra definida. Para TCC, documentar como "fora do escopo de implementação" ou usar backup do Postgres gerenciado. |
| RNF-F01..F12 | (doc do front) pt-BR, mock em localStorage, offline, responsivo, tema, calendário próprio, React+TS strict, Vite, CSS próprio, separação por empresa, validações BR, textos curtos | ✅ | Válidos para o front, exceto **RNF-F02/F03 (mock sem API), que deixaram de valer**: o front agora consome a API. |

## Requisitos de domínio

| ID | Requisito | Status | Observação |
|----|-----------|--------|------------|
| RD001 | Seguir a Resolução CFP nº 013/2015 (atuação do psicólogo e prontuários eletrônicos) | 🟡 | Recomenda-se o dev conferir o texto da resolução citada e das normas do CFP sobre registro documental/prontuário antes de implementar RF019/RF020 (guarda mínima, sigilo, acesso). |
| RD002 | Confidencialidade: só profissionais autorizados acessam dados do paciente | ✅ (API) | Isolamento por empresa em todas as consultas; psicólogo só acessa a própria agenda; anotações e feedback só para o psicólogo da sessão (D-02); prontuários e laudos só para o autor, com trilha de acessos (leituras, downloads e tentativas negadas — `GET /medical-records/{id}/access-log`). |

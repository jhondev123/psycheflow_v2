# 03 — Casos de uso (consolidado com status)

Fonte: `docs/Documento casos de uso Atualizado.docx` (UC01–UC24). UC25–UC29 foram adicionados a partir do código.
Ator principal em todos: **Psicólogo** (logado), exceto quando indicado.

> **Correções feitas na consolidação** (erros de copiar/colar no original):
> - UC07 (cancelamento) dizia "botão de reagendamento" e "reagendamento concluído" → corrigido para cancelamento.
> - UC08 (conclusão) dizia "reagendamento concluído" → corrigido para conclusão.
> - UC10 (recorrência) tinha pós-condição "Sessão editada" → corrigido para "recorrência criada e sessões geradas".
> - Campos e validações foram completados com o requisito correspondente (RF) e as regras (RN).

## Índice

> Status atualizados em 28/09/2026: API reestruturada (`08`) e front integrado à API. Contratos em `06-api.md`.

| UC | Nome | RF | API | Front |
|----|------|----|-----|-------|
| UC01 | Cadastrar paciente | RF001 | ✅ | ✅ |
| UC02 | Editar paciente | RF003 | ✅ | ✅ |
| UC03 | Visualizar pacientes | RF002 | ✅ | ✅ |
| UC04 | Cadastrar sessão | RF004 | ✅ | ✅ |
| UC05 | Visualizar sessões | RF005 | ✅ | ✅ |
| UC06 | Reagendar sessão | RF006 | ✅ | ✅ |
| UC07 | Cancelar sessão | RF007 | ✅ | ✅ |
| UC08 | Concluir sessão | RF009 | ✅ | ✅ |
| UC09 | Editar sessão | RF008 | ✅ | ✅ |
| UC10 | Criar recorrência de sessões | RF010 | ✅ | ✅ |
| UC11 | Lançar pagamento | RF011 | ✅ | ✅ |
| UC12 | Visualizar pagamentos | RF012 | ✅ | ✅ |
| UC13 | Editar pagamento | RF013 | ✅ | ✅ |
| UC14 | Gerar recibo de sessão | RF014 | ✅ | ✅ |
| UC15 | Gerar relatório de sessões | RF015 | ✅ | ✅ |
| UC16 | Gerar relatório de feedback | RF015-B | ✅ | ✅ |
| UC17 | Gerar laudo | RF016 | ✅ | ✅ |
| UC18 | Gerenciar agenda | RF017 | ✅ | ✅ |
| UC19 | Lançar bloqueio de agenda | RF018 | ✅ | ✅ |
| UC20 | Criar prontuário | RF019 | ✅ | ✅ |
| UC21 | Gerenciar prontuários | RF020 | ✅ | ✅ |
| UC22 | Usar sugestões de IA | RF021 | ✅ | ✅ |
| UC23 | Login | RF022 | ✅ | ✅ |
| UC24 | Logout | RF023 | ✅ | ✅ |
| UC25 | Criar conta (empresa + responsável) | RC-01 | ✅ | ✅ |
| UC26 | Admin/Gerente cadastra usuário | RC-02 | ✅ | ✅ |
| UC27 | Definir horários de trabalho | RC-03 | ✅ | ✅ |
| UC28 | Editar perfil profissional | RC-04 | ✅ | ✅ |
| UC29 | Gerenciar e gerar modelos de documento | RC-05 | ❌ (removido) | ❌ (removido) |

---

## Pacientes

### UC01 — Cadastrar paciente
- **Pré:** logado; não existe paciente com o mesmo CPF na empresa (RN-22).
- **Fluxo principal:**
  1. Menu → Pacientes → "Novo paciente".
  2. Informa CEP → sistema preenche endereço (RN-26); informa nome, CPF, telefone, e-mail, nascimento, número, complemento.
  3. Salva.
- **Alternativos:** campo obrigatório vazio ou inválido → alerta no campo, não salva (RN-20, RN-21, RN-23, RN-24). CPF já cadastrado → mensagem e não salva.
- **Pós:** paciente criado com status Ativo (RN-25).
- **Status:** API ✅ · Front ✅.

### UC02 — Editar paciente
- **Pré:** logado; paciente cadastrado.
- **Fluxo:** Pacientes → ícone editar na linha → mesmo formulário do cadastro (CPF somente leitura, RN-27) → Salvar.
- **Alternativos:** validação falha → alerta. Erro ao salvar → nada é alterado.
- **Pós:** dados atualizados; `UpdatedAt` gravado (RN-03).
- **Status:** API ✅ · Front ✅.

### UC03 — Visualizar pacientes
- **Pré:** logado.
- **Fluxo:** Pacientes → lista com filtros por nome, status e data da última sessão.
- **Alternativo:** nenhum resultado → estado vazio.
- **Status:** API ✅ · Front ✅.

## Sessões

### UC04 — Cadastrar sessão
- **Pré:** logado; horário livre; paciente cadastrado.
- **Fluxo:**
  1. Agenda (clique no horário) ou Sessões → "Nova sessão".
  2. Escolhe paciente, data, horário; duração vem da configuração (RN-32); anotações opcionais.
  3. Confirma.
- **Validações:** RN-30, RN-31, RN-32, RN-33, RN-34, RN-35.
- **Alternativo:** qualquer erro → mensagem e nada é gravado (transação).
- **Pós:** `Schedule`(SESSION, Pending) + `Session`(Scheduled) criados; **pagamento Pendente criado** (RN-50).
- **Status:** API ✅ · Front ✅.

### UC05 — Visualizar sessões
- **Pré:** logado.
- **Fluxo:** Sessões → lista/calendário com filtros por período, horário, paciente (e status).
- **Alternativo:** nenhum resultado → estado vazio.
- **Status:** API ✅ · Front ✅.

### UC06 — Reagendar sessão
- **Pré:** logado; sessão existe e não está concluída/cancelada (RN-41).
- **Fluxo:** Sessões/Agenda → abrir sessão → "Reagendar" → popup com nova data\*, horário\*, motivo\* → Confirmar.
- **Validações:** RN-30, RN-33, RN-34 (ignorando o próprio agendamento), RN-42.
- **Alternativo:** erro → mensagem; nada muda.
- **Pós:** agendamento com nova data/horário; motivo registrado.
- **Status:** API ✅ · Front ✅.

### UC07 — Cancelar sessão
- **Pré:** logado; sessão existe e não está concluída/cancelada; se o pagamento estiver Pago, precisa ser cancelado antes (RN-44).
- **Fluxo:** Sessões/Agenda → abrir sessão → "Cancelar" → popup pede motivo\* → Confirmar.
- **Alternativo:** erro → mensagem; nada muda.
- **Pós:** agendamento cancelado (libera o horário, RN-34); pagamento da sessão Cancelado (RN-43); motivo registrado.
- **Status:** API ✅ · Front ✅.

### UC08 — Concluir sessão
- **Pré:** logado; sessão existe e não está concluída/cancelada.
- **Fluxo:** Sessões → abrir sessão → "Concluir" → informa anotações\* (Markdown) e feedback\* (nota 0–10) → Confirmar.
- **Validações:** RN-41, RN-45, RN-46.
- **Alternativo:** erro → mensagem; nada muda.
- **Pós:** `SessionStatus = Completed`.
- **Extensão (existe no front):** "Marcar falta" → `SessionStatus = NoShow` (RN-47).
- **Status:** API ✅ · Front ✅.

### UC09 — Editar sessão
- **Pré:** logado; sessão não concluída/cancelada.
- **Fluxo:** Sessões → editar → altera anotações, data, horário, duração ou paciente → Salvar. Opções de cancelar (UC07) e excluir no mesmo lugar.
- **Validações:** se mudar data/horário → mesmas de UC06.
- **Pós:** sessão atualizada.
- **Status:** API ✅ · Front ✅.

### UC10 — Criar recorrência de sessões
- **Pré:** logado; paciente cadastrado.
- **Fluxo:** Pacientes → paciente → "Criar recorrência" → tipo (Semanal/Mensal)\*, dia/horário base, valor da sessão → Confirmar.
- **Validações:** RN-48; cada sessão gerada passa por RN-33/RN-34 (conflitos são reportados ao usuário).
- **Pós:** recorrência gravada; sessões futuras geradas com o valor definido.
- **Status:** API ✅ · Front ✅.

## Financeiro

### UC11 — Lançar pagamento
- **Pré:** logado; sessão agendada e concluída (RN-55).
- **Fluxo:** Sessões → sessão → "Lançar pagamento" → valor\* (preenchido pela config/recorrência), método\* (Crédito/Débito/Pix), data (≥ hoje), informações extras → Salvar.
- **Alternativos:** data inválida → erro (RN-53). Falha → nada gravado.
- **Pós:** pagamento da sessão com status **Pago**.
- **Status:** API ✅ · Front ✅.

### UC12 — Visualizar pagamentos
- **Fluxo:** Pagamentos → lista com filtros paciente, período (fim opcional), status (Pendente/Pago/Cancelado).
- **Alternativo:** nada encontrado → mensagem informativa.
- **Status:** API ✅ · Front ✅.

### UC13 — Editar pagamento
- **Pré:** pagamento com status diferente de Pago (RN-56).
- **Fluxo:** Pagamentos → selecionar → editar valor e/ou status → Salvar.
- **Alternativos:** status Pago → edição bloqueada. Erro → nada muda.
- **Status:** API ✅ · Front ✅.

## Documentos e relatórios

### UC14 — Gerar recibo de sessão
- **Pré:** pagamento da sessão Pago (RN-57).
- **Fluxo:** Sessões ou Pagamentos → sessão paga → "Gerar recibo" → confirmar → PDF para baixar/imprimir.
- **Status:** API ✅ · Front ✅.

### UC15 — Gerar relatório de sessões
- **Fluxo:** Relatórios → "Relatório de Sessões" → filtros período, status da sessão, status do pagamento → gerar (sessão: data, horário, paciente, status, observações; pagamento: valor, status, método).
- **Alternativo:** sem dados → mensagem informativa.
- **Pós:** relatório para visualizar/imprimir/exportar.
- **Status:** API ✅ · Front ✅.

### UC16 — Gerar relatório de feedback de pacientes
- **Fluxo:** Relatórios → "Relatório de Feedbacks" → paciente\*, período → gerar (dados do paciente + feedbacks por data/horário da sessão).
- **Alternativo:** sem feedbacks no período → mensagem.
- **Status:** API ✅ · Front ✅.

### UC17 — Gerar laudo
- **Pré:** paciente cadastrado.
- **Fluxo:** Laudos → "Criar novo laudo" → paciente\*, motivo\*, modelo\* → opcionalmente incluir dados/anotações das sessões → salvar → exportar.
- **Alternativo:** obrigatório vazio → alerta, não cria.
- **Pós:** laudo gravado e vinculado ao paciente.
- **Status:** API ✅ · Front ✅.

## Agenda

### UC18 — Gerenciar agenda
- **Fluxo:** Agenda → vê sessões, bloqueios e horários livres (expediente destacado) → alterna dia/semana/mês → navega entre datas → clica num horário para agendar sessão (UC04) ou criar bloqueio (UC19) → clica num item para ver detalhes/confirmar/cancelar/excluir.
- **Alternativo:** sem atividades → "sem registros".
- **Status:** API ✅ · Front ✅.

### UC19 — Lançar bloqueio de agenda
- **Fluxo:** Agenda → "Novo bloqueio" → tipo (dias inteiros ou horários)\*, intervalo\*, motivo → Confirmar.
- **Alternativo:** intervalo inválido ou conflitante com sessão → impede (RN-38).
- **Pós:** período bloqueado para novos agendamentos.
- **Status:** API ✅ · Front ✅.

## Prontuários

### UC20 — Criar prontuário
- **Pré:** paciente cadastrado.
- **Fluxo:** Prontuários → "Criar novo" → paciente\* → texto e anexos (PDF/imagem) → Salvar.
- **Alternativo:** paciente não informado → erro.
- **Pós:** prontuário vinculado ao paciente.
- **Status:** API ✅ · Front ✅.

### UC21 — Gerenciar prontuários
- **Fluxo:** Prontuários → busca por paciente, data de criação ou palavra-chave → visualizar/editar/excluir.
- **Alternativo:** nada encontrado → "Nenhum prontuário encontrado".
- **Status:** API ✅ · Front ✅.

## IA

### UC22 — Usar sugestões com IA
- **Pré:** a clínica habilitou a IA (Admin/Manager aceitou os termos, escolheu o provedor e os dados liberados — RN-67); usuário é psicólogo.
- **Fluxo:** Configurações → ativa IA → escolhe provedor (Claude, OpenAI ou Gemini) e dados permitidos (anotações, feedbacks, prontuários) → aceita os termos. O psicólogo passa a ver os botões de sugestão: ao concluir/editar a sessão (organizar anotações), e na ficha do paciente (análise do acompanhamento e próximos passos). A resposta vem em Markdown com aviso de revisão; o psicólogo copia o que quiser.
- **Alternativo:** nenhum dado selecionado → não habilita (422); desabilitar revoga o aceite (403 `ai.disabled` nas sugestões); sem dados clínicos autorizados → 422 `ai.insufficient_data`; provedor fora do ar → 503; recusa do provedor → 422 `ai.refused`.
- **Status:** API ✅ · Front ✅.

## Acesso

### UC23 — Login
- **Pré:** cadastro ativo.
- **Fluxo:** tela de login → e-mail e senha → sistema valida e libera acesso.
- **Alternativos:** credenciais inválidas → "Usuário ou senha inválidos". Várias tentativas → bloqueio temporário (RN-12).
- **Pós:** autenticado (JWT no front).
- **Status:** API ✅ · Front ✅.

### UC24 — Logout
- **Fluxo:** menu → "Sair" → sistema descarta a sessão e volta ao login.
- **Status:** API ✅ · Front ✅.

## Casos de uso complementares (vindos do código)

### UC25 — Criar conta (empresa + responsável)
- **Ator:** visitante. **Fluxo:** tela "Criar conta" → nome, e-mail, senha forte, CRP, abordagem → sistema cria empresa + usuário Psychologist (ou Admin) + perfil de psicólogo e já autentica.
- **Status:** API ✅ · Front ✅.
- **Sugestão KISS:** um único `POST /User/register` que cria a company quando `company_id` não vier, tudo na mesma transação.

### UC26 — Admin/Gerente cadastra usuário da empresa
- **Ator:** Admin ou Manager. **Fluxo:** informa e-mail, perfil, CRP (se psicólogo) e senha opcional → sistema gera senha se vazia e retorna.
- **Status:** API ✅ · Front ✅.

### UC27 — Definir horários de trabalho
- **Fluxo:** Configurações → Horários de atendimento → para cada dia, ativa e define faixa(s) → Salvar (substitui tudo, RN-68). Usado por RN-33.
- **Status:** API ✅ · Front ✅.

### UC28 — Editar perfil profissional
- **Fluxo:** Configurações → Perfil → nome, telefone, CRP, abordagem → Salvar.
- **Status:** API ✅ · Front ✅.

### UC29 — Gerenciar e gerar modelos de documento
- **Fluxo (cadastro):** informa nome, descrição, arquivo de template (`.frx` em `Documents/`) e campos (nome, ordem, obrigatório, valor padrão).
- **Fluxo (geração):** escolhe modelo → preenche parâmetros (pode puxar do paciente) → PDF.
- **Validações:** RN-60, RN-61; nº de parâmetros do template precisa bater com os campos cadastrados.
- **Status:** API ❌ (removido — substituído pelos documentos fixos em QuestPDF, UC14–UC17) · Front ❌ (removido).
- **Importância:** é a base de UC14 (recibo), UC15/UC16 (relatórios) e UC17 (laudo).

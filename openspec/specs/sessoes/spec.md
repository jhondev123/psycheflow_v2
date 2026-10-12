# Sessões — Especificação

## Purpose

Controlar o ciclo de vida dos atendimentos: agendar, consultar, confirmar, editar, reagendar, cancelar, concluir com
anotações e feedback, marcar falta e excluir, preservando o sigilo das anotações clínicas e o vínculo com o pagamento.

## Requirements

### Requirement: Agendamento de sessão
O sistema SHALL agendar sessões com paciente, data, horário, duração opcional (padrão da clínica, de 15 a 240 minutos),
anotações opcionais e valor opcional; a sessão, seu item de agenda e o pagamento pendente MUST ser gravados juntos, e a
sessão nasce Agendada.

**Origem**: RF004 · UC04 · RN-32 · RN-35 · RN-36 · RN-50

#### Scenario: Sessão agendada com a duração padrão
- **GIVEN** a duração padrão da clínica é 50 minutos
- **WHEN** o psicólogo agenda uma sessão sem informar a duração
- **THEN** o sistema responde 201 com a sessão `Scheduled` das 09:00 às 09:50 e o pagamento `Pending`

#### Scenario: Duração abaixo do mínimo
- **WHEN** o psicólogo agenda uma sessão de 10 minutos
- **THEN** o sistema responde 422 com erro no campo `durationMinutes`

#### Scenario: Paciente de outra empresa
- **WHEN** o psicólogo agenda uma sessão para um paciente de outra empresa
- **THEN** o sistema responde 404 e nada é gravado

#### Scenario: Gestão agenda sem indicar o psicólogo
- **WHEN** um Manager sem perfil de psicólogo agenda uma sessão sem `psychologistId`
- **THEN** o sistema responde 422 com `code = scheduling.psychologist_required`

#### Scenario: Erro em qualquer etapa
- **WHEN** a gravação do pagamento falha
- **THEN** nem a sessão nem o item de agenda são gravados e o usuário recebe a mensagem de erro

### Requirement: Consulta de sessões
O sistema SHALL listar as sessões de forma paginada filtrando por período (início obrigatório, fim opcional), faixa de
horário, paciente, psicólogo e situação, e SHALL detalhar uma sessão com paciente, horário, situação e pagamento.

**Origem**: RF005 · UC05

#### Scenario: Período só com a data inicial
- **WHEN** o psicólogo lista as sessões a partir de 01/10/2026 sem data final
- **THEN** o sistema devolve as sessões desde 01/10/2026

#### Scenario: Sem data inicial
- **WHEN** a lista é pedida sem `from`
- **THEN** o sistema responde 422 no campo `from`

#### Scenario: Nenhuma sessão encontrada
- **WHEN** nenhuma sessão atende aos filtros
- **THEN** o sistema devolve uma página vazia e a tela mostra o estado vazio

### Requirement: Visibilidade das sessões
O psicólogo MUST acessar apenas as próprias sessões; Admin e Manager SHALL acessar todas as sessões da empresa.

**Origem**: D-02 · A-15 · RD002

#### Scenario: Sessão de um colega
- **WHEN** um psicólogo abre a sessão de outro psicólogo
- **THEN** o sistema responde 403 com `code = session.cannot_access`

### Requirement: Sigilo das anotações e do feedback
As anotações, a nota e o comentário de feedback de uma sessão MUST ser vistos e alterados apenas pelo psicólogo da sessão;
Admin e Manager SHALL ver a sessão sem esse conteúdo.

**Origem**: RD002 · D-02 · RNF001

#### Scenario: Gerente abre uma sessão concluída
- **WHEN** um Manager consulta uma sessão concluída
- **THEN** a resposta não traz `notes`, `feedbackScore` nem `feedbackComment` e indica `clinicalNotesVisible = false`

#### Scenario: Gerente tenta registrar anotações
- **WHEN** um Manager tenta alterar as anotações de uma sessão
- **THEN** o sistema responde 403 com `code = session.clinical_data_restricted`

### Requirement: Anotações em Markdown
O sistema SHALL guardar as anotações da sessão como texto Markdown e o front MUST exibi-las formatadas.

**Origem**: RF004 · RN-46

#### Scenario: Anotação formatada
- **WHEN** o psicólogo salva anotações com títulos e listas em Markdown
- **THEN** o texto é guardado como enviado e exibido formatado no detalhe da sessão

### Requirement: Confirmação da sessão
O sistema SHALL permitir confirmar o horário de uma sessão agendada.

**Origem**: RF017 · UC18

#### Scenario: Sessão confirmada
- **WHEN** o psicólogo confirma uma sessão agendada
- **THEN** o item de agenda passa para `Confirmed` e a agenda mostra a nova cor

### Requirement: Sessão encerrada não muda
Uma sessão concluída, cancelada ou com falta MUST NOT aceitar edição, reagendamento, cancelamento, conclusão ou falta.

**Origem**: UC06–UC09 · RN-41

#### Scenario: Reagendar sessão concluída
- **GIVEN** a sessão está concluída
- **WHEN** o psicólogo tenta reagendá-la
- **THEN** o sistema responde 409 com `code = session.not_open`

### Requirement: Edição de sessão
O sistema SHALL permitir trocar o paciente e as anotações de uma sessão em aberto; anotações não enviadas MUST manter o
texto atual. Data, horário e duração mudam pelo reagendamento.

**Origem**: RF008 · UC09

#### Scenario: Troca de paciente
- **WHEN** o psicólogo troca o paciente de uma sessão agendada sem enviar anotações
- **THEN** o paciente muda e as anotações continuam as mesmas

### Requirement: Reagendamento
O sistema SHALL reagendar uma sessão em aberto para nova data e horário, mantendo a duração se ela não for informada; o
motivo MUST ser obrigatório e as regras de passado, expediente e conflito MUST ser reaplicadas, ignorando o próprio horário.

**Origem**: RF006 · UC06 · RN-42

#### Scenario: Reagendamento aceito
- **WHEN** o psicólogo reagenda a sessão para 15/10/2026 às 14:00 com o motivo "Paciente viajou"
- **THEN** a sessão passa para o novo horário e o motivo fica registrado em `rescheduleReason`

#### Scenario: Motivo ausente
- **WHEN** o reagendamento é enviado sem motivo
- **THEN** o sistema responde 422 no campo `reason`

#### Scenario: Deslocar sobre o próprio horário
- **GIVEN** a sessão é das 09:00 às 09:50
- **WHEN** o psicólogo a reagenda para as 09:30 do mesmo dia, sem outros itens no horário
- **THEN** o reagendamento é aceito

### Requirement: Cancelamento
O sistema SHALL cancelar uma sessão em aberto mediante motivo obrigatório, liberando o horário e cancelando o pagamento
pendente; se o pagamento já tiver sido recebido, o sistema MUST exigir o estorno antes.

**Origem**: RF007 · UC07 · RN-43 · RN-44

#### Scenario: Sessão cancelada
- **WHEN** o psicólogo cancela a sessão com o motivo "Paciente desmarcou"
- **THEN** a sessão e o item de agenda ficam `Cancelled`, o horário é liberado e o pagamento fica `Cancelled`

#### Scenario: Pagamento já recebido
- **GIVEN** o pagamento da sessão está `Paid`
- **WHEN** alguém tenta cancelar a sessão
- **THEN** o sistema responde 409 com `code = payment.paid_blocks_session_change`

### Requirement: Conclusão com anotações e feedback
O sistema SHALL concluir uma sessão em aberto, depois do horário de início, exigindo anotações e nota de feedback de 0 a 10
com comentário opcional; somente o psicólogo da sessão MUST poder concluí-la.

**Origem**: RF009 · UC08 · RN-45

#### Scenario: Sessão concluída
- **WHEN** o psicólogo conclui a sessão com anotações e nota 8
- **THEN** a sessão passa para `Completed` com `feedbackScore = 8`

#### Scenario: Nota fora da escala
- **WHEN** a conclusão é enviada com nota 11
- **THEN** o sistema responde 422 com `code = session.invalid_feedback_score`

#### Scenario: Sem anotações
- **WHEN** a conclusão é enviada sem anotações
- **THEN** o sistema responde 422 com `code = session.notes_required`

#### Scenario: Antes do início
- **WHEN** o psicólogo tenta concluir uma sessão que ainda não começou
- **THEN** o sistema responde 409 com `code = session.not_started`

### Requirement: Falta do paciente
O sistema SHALL permitir marcar falta numa sessão em aberto depois do horário de início, encerrando a sessão.

**Origem**: RN-47 · UC08

#### Scenario: Falta registrada
- **WHEN** o psicólogo marca falta numa sessão que já começou
- **THEN** a sessão passa para `NoShow` e não aceita mais alterações

### Requirement: Exclusão de sessão
O sistema SHALL permitir excluir logicamente uma sessão não concluída, liberando o horário; sessões concluídas MUST NOT ser
excluídas, pois fazem parte do registro clínico.

**Origem**: RF008 · RN-02

#### Scenario: Sessão agendada excluída
- **WHEN** o psicólogo exclui uma sessão agendada
- **THEN** o sistema responde 204, a sessão some das listas e o horário fica livre

#### Scenario: Sessão concluída
- **WHEN** alguém tenta excluir uma sessão concluída
- **THEN** o sistema responde 409 com `code = session.completed_cannot_be_deleted`

# Documentos em PDF — Especificação

## Purpose

Emitir em PDF os documentos do dia a dia da clínica — recibo de pagamento, declaração de comparecimento, relatório de
sessões e relatório de feedback do paciente — com cabeçalho padronizado e respeitando o sigilo dos dados clínicos.

## Requirements

### Requirement: Cabeçalho e formato dos documentos
Todo documento SHALL ser gerado em PDF para download ou impressão, com cabeçalho contendo a clínica, o psicólogo e o CRP, e
MUST ser entregue com um nome de arquivo descritivo.

**Origem**: RF014 · RF015 · A-32

#### Scenario: Download pelo front
- **WHEN** o psicólogo clica em "Recibo" num pagamento recebido
- **THEN** o navegador baixa um arquivo PDF com o nome enviado pela API

### Requirement: Recibo de pagamento
O sistema SHALL emitir o recibo de um pagamento com o valor em algarismos e por extenso; o recibo MUST ser emitido apenas
para pagamentos recebidos.

**Origem**: RF014 · UC14 · RN-57

#### Scenario: Recibo de pagamento recebido
- **GIVEN** o pagamento de 150,00 está `Paid`
- **WHEN** o psicólogo pede o recibo
- **THEN** o sistema devolve um PDF com o paciente e o CPF, "R$ 150,00 (cento e cinquenta reais)", a data e o horário da sessão, o método e a data do pagamento

#### Scenario: Pagamento ainda pendente
- **WHEN** alguém pede o recibo de um pagamento `Pending`
- **THEN** o sistema responde 409 com `code = document.receipt_requires_paid_payment`

### Requirement: Declaração de comparecimento
O sistema SHALL emitir declaração de comparecimento apenas para sessões concluídas.

**Origem**: RF014

#### Scenario: Sessão concluída
- **WHEN** o psicólogo pede a declaração de uma sessão `Completed`
- **THEN** o sistema devolve um PDF declarando o comparecimento do paciente na data e no horário da sessão

#### Scenario: Sessão não concluída
- **WHEN** alguém pede a declaração de uma sessão agendada
- **THEN** o sistema responde 409 com `code = document.attendance_requires_completed_session`

### Requirement: Relatório de sessões
O sistema SHALL gerar o relatório de sessões de um período (início obrigatório, fim opcional), com filtros por situação da
sessão, situação do pagamento e psicólogo, listando data, horário, paciente, situação, observações e pagamento (valor,
situação e método), com totais.

**Origem**: RF015 · UC15 · RN-63

#### Scenario: Relatório do mês
- **WHEN** o psicólogo gera o relatório de 01/10/2026 a 31/10/2026 com sessões concluídas e pagamento pendente
- **THEN** o PDF lista as sessões que atendem aos filtros e os totais de quantidade e valores

#### Scenario: Sem data inicial
- **WHEN** o relatório é pedido sem `from`
- **THEN** o sistema responde 422 com a mensagem "Informe a data inicial do período."

#### Scenario: Observações de outros psicólogos
- **WHEN** um Admin gera o relatório da clínica inteira
- **THEN** as observações clínicas só aparecem nas sessões do próprio usuário

#### Scenario: Período sem sessões
- **WHEN** nenhuma sessão atende aos filtros
- **THEN** o PDF informa "Nenhuma sessão encontrada com os filtros informados."

### Requirement: Relatório de feedback do paciente
O sistema SHALL gerar, só para psicólogos, o relatório das notas de feedback (0 a 10) de um paciente num período (paciente
e início obrigatórios, fim opcional), com os dados do paciente, as notas por data e horário da sessão e a média; só entram
sessões do psicólogo logado.

**Origem**: RF015-B · UC16 · RN-64

#### Scenario: Feedbacks do paciente
- **WHEN** o psicólogo gera o relatório de feedback de um paciente a partir de 01/08/2026
- **THEN** o PDF lista as notas das sessões concluídas dele com esse paciente e a média

#### Scenario: Sem feedbacks no período
- **WHEN** o paciente não tem sessões com feedback no período
- **THEN** o PDF informa "Nenhum feedback registrado no período."

#### Scenario: Gestão sem perfil de psicólogo
- **WHEN** um Manager sem perfil de psicólogo pede o relatório de feedback
- **THEN** o sistema responde 403 com `code = document.only_psychologists`

#### Scenario: Paciente não informado
- **WHEN** o relatório é pedido sem `patientId`
- **THEN** o sistema responde 422 com a mensagem "Informe o paciente."

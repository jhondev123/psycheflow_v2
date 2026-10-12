# Financeiro — Especificação

## Purpose

Controlar o pagamento de cada sessão: o pagamento nasce pendente com a sessão, é lançado (recebido) depois da conclusão,
pode ter o valor ajustado enquanto pendente, pode ser cancelado ou estornado e pode ser consultado com filtros.

## Requirements

### Requirement: Pagamento pendente criado com a sessão
Toda sessão, avulsa ou de recorrência, MUST nascer com um único pagamento Pendente cujo valor é o informado no agendamento,
senão o da recorrência, senão o valor padrão da clínica, senão zero.

**Origem**: UC04 · RN-50 · RN-51 · D-08

#### Scenario: Valor padrão da clínica
- **GIVEN** o valor padrão da clínica é 150,00
- **WHEN** o psicólogo agenda uma sessão avulsa sem informar valor
- **THEN** a sessão nasce com um pagamento `Pending` de 150,00

#### Scenario: Sem valor configurado
- **GIVEN** a clínica não tem valor padrão
- **WHEN** uma sessão é agendada sem valor
- **THEN** o pagamento pendente nasce com valor 0,00

### Requirement: Lançamento do pagamento
O sistema SHALL lançar o recebimento de um pagamento pendente informando o método (cartão de crédito, cartão de débito ou
Pix), a data, o valor e observações opcionais; o lançamento MUST exigir a sessão concluída e a data MUST NOT ser anterior a
hoje (padrão: hoje).

**Origem**: RF011 · UC11 · RN-52 · RN-53 · RN-55

#### Scenario: Pagamento recebido por Pix
- **GIVEN** a sessão está concluída e o pagamento pendente
- **WHEN** o psicólogo lança o pagamento com método `Pix` sem informar a data
- **THEN** o pagamento passa para `Paid` com `paidAt` igual a hoje

#### Scenario: Sessão ainda não concluída
- **WHEN** alguém lança o pagamento de uma sessão agendada
- **THEN** o sistema responde 409 com `code = payment.session_not_completed`

#### Scenario: Data anterior a hoje
- **WHEN** o lançamento informa `paidAt` de ontem
- **THEN** o sistema responde 422 com `code = payment.paid_at_in_past`

### Requirement: Edição do valor de pagamento pendente
O sistema SHALL permitir alterar o valor de um pagamento pendente; o valor MUST NOT ser negativo e um pagamento pago ou
cancelado MUST NOT ser editado.

**Origem**: RF013 · UC13 · RN-56

#### Scenario: Valor ajustado
- **WHEN** o psicólogo altera o valor de um pagamento pendente para 120,00
- **THEN** o pagamento passa a valer 120,00

#### Scenario: Pagamento já recebido
- **WHEN** alguém tenta alterar o valor de um pagamento `Paid`
- **THEN** o sistema responde 409 com `code = payment.not_pending`

#### Scenario: Valor negativo
- **WHEN** o valor enviado é -10,00
- **THEN** o sistema responde 422 com `code = payment.invalid_amount`

### Requirement: Cancelamento e estorno
O sistema SHALL cancelar um pagamento pendente ou estornar um pagamento recebido mediante motivo obrigatório; um pagamento
já cancelado MUST NOT ser cancelado de novo.

**Origem**: RF013 · RN-54 · RN-44

#### Scenario: Estorno de pagamento recebido
- **WHEN** o psicólogo estorna um pagamento `Paid` com o motivo "Cobrança em duplicidade"
- **THEN** o pagamento passa para `Cancelled` com o motivo registrado

#### Scenario: Motivo ausente
- **WHEN** o cancelamento é enviado sem motivo
- **THEN** o sistema responde 422 com `code = payment.reason_required`

#### Scenario: Já cancelado
- **WHEN** alguém cancela um pagamento `Cancelled`
- **THEN** o sistema responde 409 com `code = payment.already_cancelled`

### Requirement: Consulta de pagamentos
O sistema SHALL listar os pagamentos de forma paginada com filtros por paciente, psicólogo, período da data da sessão
(início e fim opcionais) e situação (Pendente, Pago, Cancelado); o psicólogo MUST ver apenas os pagamentos das próprias
sessões.

**Origem**: RF012 · UC12 · RN-54 · D-02

#### Scenario: Pendentes do mês
- **WHEN** o psicólogo filtra a situação `Pending` de 01/10/2026 a 31/10/2026
- **THEN** aparecem os pagamentos pendentes das próprias sessões nesse período

#### Scenario: Nenhum pagamento encontrado
- **WHEN** nenhum pagamento atende aos filtros
- **THEN** a tela Financeiro mostra uma mensagem informativa

#### Scenario: Gestão vê a clínica inteira
- **WHEN** um Admin lista os pagamentos sem filtro de psicólogo
- **THEN** aparecem os pagamentos de todas as sessões da empresa

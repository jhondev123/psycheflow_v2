# Recorrência de sessões — Especificação

## Purpose

Permitir que o psicólogo crie séries de sessões semanais ou mensais para um paciente, com valor próprio, gerando as sessões
em janelas de três meses que respeitam as regras de agenda, e que possa estender ou encerrar a série.

## Requirements

### Requirement: Criação de recorrência
O sistema SHALL criar uma recorrência semanal ou mensal para um paciente a partir de data inicial, horário, duração
opcional, valor opcional e data final opcional, gerando de uma vez as sessões dos próximos 3 meses; a data inicial MUST NOT
ser anterior a hoje.

**Origem**: RF010 · UC10 · RN-48 · D-05

#### Scenario: Recorrência semanal
- **WHEN** o psicólogo cria uma recorrência semanal a partir de terça, 13/10/2026, às 10:00, com valor 150,00
- **THEN** o sistema responde 201 com a recorrência e as datas agendadas de todas as terças até 3 meses à frente
- **AND** cada sessão gerada nasce com pagamento pendente de 150,00

#### Scenario: Data inicial no passado
- **WHEN** a recorrência é criada com data inicial de ontem
- **THEN** o sistema responde 422 com `code = recurrence.start_in_past`

#### Scenario: Criada pelo agendamento
- **WHEN** o psicólogo marca "Repetir" ao agendar uma sessão na agenda
- **THEN** o front cria a recorrência com os dados do agendamento

### Requirement: Datas da recorrência mensal
Na recorrência mensal, o sistema MUST repetir o dia do mês da data inicial e, nos meses em que esse dia não existe, MUST
usar o último dia do mês.

**Origem**: RN-48

#### Scenario: Dia 31 em mês de 30 dias
- **GIVEN** uma recorrência mensal que começa em 31/10/2026
- **WHEN** as sessões são geradas
- **THEN** a sessão de novembro cai em 30/11/2026

### Requirement: Datas indisponíveis são puladas
Cada sessão gerada MUST passar pelas regras de passado, expediente e conflito da agenda; uma data indisponível MUST ser
pulada, sem impedir as demais, e devolvida ao usuário com o motivo.

**Origem**: RN-48 · RN-33 · RN-34 · A-31

#### Scenario: Feriado bloqueado
- **GIVEN** o dia 20/10/2026 está bloqueado
- **WHEN** uma recorrência semanal de terças é criada a partir de 13/10/2026
- **THEN** 20/10/2026 aparece em `skipped` com `code = scheduling.conflict` e o motivo, e as outras terças são agendadas

### Requirement: Extensão da recorrência
O sistema SHALL gerar mais 3 meses de sessões de uma recorrência ativa a partir da última data gerada, respeitando a data
final quando houver.

**Origem**: RF010 · D-05

#### Scenario: Estender
- **WHEN** o psicólogo estende uma recorrência ativa
- **THEN** são geradas as sessões dos 3 meses seguintes a `generatedUntil`

#### Scenario: Nada mais a gerar
- **GIVEN** a recorrência já gerou sessões até a data final
- **WHEN** o psicólogo tenta estendê-la
- **THEN** o sistema responde 409 com `code = recurrence.fully_generated`

### Requirement: Encerramento da recorrência
O sistema SHALL encerrar uma recorrência com motivo opcional, cancelando as sessões futuras ainda agendadas; uma recorrência
encerrada MUST NOT ser estendida nem encerrada de novo.

**Origem**: RF010

#### Scenario: Recorrência encerrada
- **WHEN** o psicólogo encerra a recorrência com o motivo "Alta"
- **THEN** a recorrência fica inativa e as sessões futuras agendadas dela são canceladas com seus pagamentos pendentes

#### Scenario: Estender recorrência encerrada
- **WHEN** alguém tenta estender uma recorrência encerrada
- **THEN** o sistema responde 409 com `code = recurrence.not_active`

### Requirement: Consulta das recorrências do paciente
O sistema SHALL listar as recorrências por paciente, com a opção de mostrar só as ativas.

**Origem**: RF010

#### Scenario: Recorrências ativas na ficha do paciente
- **WHEN** o psicólogo abre a ficha do paciente
- **THEN** ele vê as recorrências ativas com tipo, horário, valor e até quando foram geradas, com as opções de estender e encerrar

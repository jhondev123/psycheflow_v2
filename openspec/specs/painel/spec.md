# Painel — Especificação

## Purpose

Dar ao usuário, na tela inicial, um resumo do dia e da semana: contadores de atendimentos, agenda de hoje, próximos
atendimentos e situação financeira, na visão do próprio psicólogo ou da clínica inteira para a gestão.

## Requirements

### Requirement: Resumo do dia e da semana
O sistema SHALL devolver, para a data de hoje no fuso da clínica, os pacientes ativos, as sessões de hoje e as confirmadas,
as sessões da semana (segunda a domingo), as confirmações pendentes, a agenda de hoje e até 6 próximos atendimentos a
partir de agora.

**Origem**: RC-06

#### Scenario: Painel do psicólogo
- **WHEN** o psicólogo abre o painel
- **THEN** ele vê os contadores, a agenda de hoje e os próximos atendimentos considerando só a própria agenda

#### Scenario: Semana de segunda a domingo
- **GIVEN** hoje é quarta-feira, 14/10/2026
- **WHEN** o painel é consultado
- **THEN** a semana vai de 12/10/2026 a 18/10/2026

### Requirement: Resumo financeiro
O painel SHALL mostrar a quantidade e o valor dos pagamentos pendentes de sessões já concluídas e o total recebido no mês.

**Origem**: RC-06 · RF012

#### Scenario: Valores a receber
- **GIVEN** há duas sessões concluídas com pagamento pendente de 150,00 cada
- **WHEN** o painel é consultado
- **THEN** o resumo financeiro mostra 2 pendentes somando 300,00

### Requirement: Escopo do painel por perfil
O psicólogo MUST ver o painel apenas da própria agenda; Admin e Manager SHALL ver o painel da clínica inteira ou de um
psicólogo escolhido.

**Origem**: RC-06 · D-02

#### Scenario: Gestão escolhe um psicólogo
- **WHEN** um Manager consulta o painel informando um psicólogo da empresa
- **THEN** os números consideram só esse psicólogo

#### Scenario: Psicólogo pede o painel de um colega
- **WHEN** um psicólogo consulta o painel informando outro psicólogo
- **THEN** o sistema responde 403

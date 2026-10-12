# Agenda — Especificação

## Purpose

Mostrar a agenda dos psicólogos (sessões, bloqueios e expediente) em visões de dia, semana e mês, permitir bloquear
horários ou dias e garantir as regras de disponibilidade que valem para todo agendamento: não marcar no passado, respeitar
o expediente e não sobrepor horários ocupados.

## Requirements

### Requirement: Visão da agenda
O sistema SHALL devolver, para um período de até 62 dias, as sessões, os bloqueios e o expediente dos psicólogos; o front
MUST exibir a agenda nas visões mensal, semanal e diária, com navegação entre datas e o expediente destacado.

**Origem**: RF017 · RF005 · UC18

#### Scenario: Agenda da semana
- **WHEN** o psicólogo abre a agenda na visão semanal
- **THEN** ele vê as próprias sessões, bloqueios e faixas de expediente da semana, com cores por situação

#### Scenario: Período longo demais
- **WHEN** a agenda é pedida de 01/09/2026 a 15/11/2026
- **THEN** o sistema responde 422 com a mensagem "Consulte no máximo 62 dias por vez."

#### Scenario: Agenda sem atividades
- **WHEN** não há sessões nem bloqueios no período
- **THEN** a agenda mostra os dias vazios com o expediente destacado

#### Scenario: Clique em horário livre
- **WHEN** o psicólogo clica num horário livre da agenda
- **THEN** o front oferece agendar uma sessão ou bloquear o horário

### Requirement: Visibilidade da agenda
O psicólogo MUST ver e alterar apenas a própria agenda; Admin e Manager SHALL ver a agenda de todos os psicólogos da
empresa ou de um psicólogo escolhido.

**Origem**: D-02 · A-15 · RD002

#### Scenario: Psicólogo pede a agenda de um colega
- **WHEN** um psicólogo consulta a agenda informando o id de outro psicólogo
- **THEN** o sistema responde 403 com `code = scheduling.cannot_manage_agenda`

#### Scenario: Gestão vê todos
- **WHEN** um Manager consulta a agenda sem informar psicólogo
- **THEN** o sistema devolve a agenda de todos os psicólogos da empresa

### Requirement: Agendamento não pode estar no passado
O sistema MUST recusar sessões e reagendamentos cuja data e horário de início sejam anteriores ao momento atual no fuso da
clínica.

**Origem**: RF004 · RN-30

#### Scenario: Horário que já passou hoje
- **GIVEN** são 14:00 na clínica
- **WHEN** o psicólogo agenda uma sessão para hoje às 10:00
- **THEN** o sistema responde 422 com `code = scheduling.in_the_past`

### Requirement: Intervalo de horário válido
O sistema MUST exigir que todo item de agenda termine depois de começar e no mesmo dia.

**Origem**: RN-31

#### Scenario: Atendimento que passa da meia-noite
- **WHEN** o psicólogo agenda uma sessão às 23:30 com 50 minutos
- **THEN** o sistema responde 422 com `code = scheduling.crosses_midnight`

### Requirement: Sessão dentro do expediente
O sistema MUST aceitar uma sessão apenas se todo o intervalo couber numa única faixa do expediente do psicólogo para aquele
dia da semana.

**Origem**: RN-33 · RF017

#### Scenario: Sessão que ultrapassa a faixa
- **GIVEN** o expediente de segunda é das 08:00 às 12:00
- **WHEN** o psicólogo agenda uma sessão de 50 minutos às 11:30 de uma segunda
- **THEN** o sistema responde 422 com `code = scheduling.outside_working_hours`

#### Scenario: Sessão dentro da faixa
- **WHEN** o psicólogo agenda uma sessão de 50 minutos às 09:00 de uma segunda
- **THEN** o agendamento é aceito

### Requirement: Sem sobreposição de horários
O sistema MUST impedir que um psicólogo tenha dois itens de agenda não cancelados e não excluídos que se sobreponham, mesmo
com pedidos simultâneos; itens cancelados ou excluídos MUST liberar o horário.

**Origem**: RN-34 · RN-38 · RF026

#### Scenario: Horário ocupado
- **GIVEN** há uma sessão das 09:00 às 09:50
- **WHEN** o psicólogo agenda outra sessão às 09:30 no mesmo dia
- **THEN** o sistema responde 409 com `code = scheduling.conflict` e a tela avisa o conflito

#### Scenario: Horário liberado por cancelamento
- **GIVEN** a sessão das 09:00 foi cancelada
- **WHEN** o psicólogo agenda outra sessão às 09:00
- **THEN** o agendamento é aceito

#### Scenario: Pedidos simultâneos
- **WHEN** dois pedidos para o mesmo horário do mesmo psicólogo chegam ao mesmo tempo
- **THEN** só um é gravado e o outro recebe 409

### Requirement: Bloqueio de agenda
O sistema SHALL permitir bloquear uma faixa de horário ou dias inteiros num intervalo de até 31 dias, com motivo opcional,
criando um bloqueio por dia; o bloqueio MAY ficar fora do expediente e MUST NOT virar sessão.

**Origem**: RF018 · UC19 · RN-37 · RN-39 · RN-40

#### Scenario: Bloqueio de horário
- **WHEN** o psicólogo bloqueia 12:00–13:00 de 13/10/2026 a 16/10/2026 com o motivo "Almoço"
- **THEN** o sistema responde 201 com quatro bloqueios, um por dia

#### Scenario: Bloqueio de dias inteiros
- **WHEN** o psicólogo bloqueia de 20/10/2026 a 22/10/2026 sem informar horários
- **THEN** cada dia fica bloqueado das 00:00 às 23:59

#### Scenario: Bloqueio fora do expediente
- **WHEN** o psicólogo bloqueia um sábado em que não atende
- **THEN** o bloqueio é aceito

#### Scenario: Intervalo maior que 31 dias
- **WHEN** o psicólogo tenta bloquear 40 dias
- **THEN** o sistema responde 422 com a mensagem "Bloqueie no máximo 31 dias por vez."

### Requirement: Bloqueio não conflita com sessões
O sistema MUST recusar um bloqueio que se sobreponha a uma sessão agendada e, nesse caso, MUST NOT criar nenhum dos
bloqueios do intervalo; um bloqueio existente MUST impedir novos agendamentos no período.

**Origem**: UC19 · RN-38 · RN-37

#### Scenario: Bloqueio sobre sessão marcada
- **GIVEN** há uma sessão em 14/10/2026 às 12:00
- **WHEN** o psicólogo bloqueia 12:00–13:00 de 13/10 a 16/10
- **THEN** o sistema responde 409 e nenhum bloqueio é criado

#### Scenario: Sessão em horário bloqueado
- **GIVEN** 20/10/2026 está bloqueado o dia inteiro
- **WHEN** o psicólogo agenda uma sessão nesse dia
- **THEN** o sistema responde 409 com `code = scheduling.conflict`

### Requirement: Remoção de bloqueio
O sistema SHALL permitir remover um bloqueio, liberando o horário para novos agendamentos.

**Origem**: RF018

#### Scenario: Bloqueio removido
- **WHEN** o psicólogo remove um bloqueio pelo detalhe na agenda
- **THEN** o sistema responde 204 e o horário fica livre

#### Scenario: Bloqueio inexistente
- **WHEN** o id do bloqueio não existe na empresa
- **THEN** o sistema responde 404 com `code = scheduling.block_not_found`

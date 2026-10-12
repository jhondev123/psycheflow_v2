# Psicólogos — Especificação

## Purpose

Manter o perfil profissional dos psicólogos da clínica (nome, CRP, abordagem e telefone) e o expediente semanal de cada um,
que serve de base para a agenda e para a validação dos agendamentos.

## Requirements

### Requirement: Consulta de psicólogos
O sistema SHALL listar os psicólogos da empresa com seus expedientes, detalhar um psicólogo e devolver o perfil do usuário
logado; usuário sem perfil de psicólogo MUST receber 404 ao pedir o próprio perfil.

**Origem**: RC-04 · RN-01

#### Scenario: Lista da empresa
- **WHEN** um usuário interno chama `GET /api/v1/psychologists`
- **THEN** o sistema devolve só os psicólogos da empresa dele, cada um com o expediente

#### Scenario: Psicólogo de outra empresa
- **WHEN** um usuário consulta o id de um psicólogo de outra empresa
- **THEN** o sistema responde 404

#### Scenario: Gerente sem perfil de psicólogo
- **WHEN** um usuário apenas Manager chama `GET /api/v1/psychologists/me`
- **THEN** o sistema responde 404 com `code = psychologist.profile_not_found`

### Requirement: Edição do perfil profissional
O sistema SHALL permitir editar nome, CRP, abordagem e telefone do psicólogo; só o próprio psicólogo, um Admin ou um
Manager MUST poder fazer a alteração.

**Origem**: RC-04 · UC28

#### Scenario: Psicólogo edita o próprio perfil
- **WHEN** o psicólogo altera a abordagem para `CognitiveBehavioral` e o telefone
- **THEN** o sistema grava os dados e devolve o perfil atualizado

#### Scenario: Psicólogo tenta editar outro psicólogo
- **WHEN** um psicólogo edita o perfil de um colega
- **THEN** o sistema responde 403 com `code = psychologist.cannot_manage`

### Requirement: CRP obrigatório e no formato do conselho
O sistema MUST exigir o número do CRP de todo psicólogo no formato `UU/NNNN` a `UU/NNNNNN` (dois dígitos da região, barra
e de 4 a 6 dígitos), por exemplo `06/12345`.

**Origem**: RN-14 · D-03

#### Scenario: CRP fora do formato
- **WHEN** o perfil é salvo com CRP `6/123`
- **THEN** o sistema responde 422 com erro no campo `licenseNumber`

### Requirement: Abordagem terapêutica
O sistema SHALL registrar a abordagem do psicólogo entre: não informada, cognitivo-comportamental, psicanálise,
comportamental, humanista, Gestalt, sistêmica ou outra.

**Origem**: RC-04

#### Scenario: Abordagem não informada
- **WHEN** a conta é criada sem abordagem
- **THEN** o perfil fica com a abordagem `NotInformed`

### Requirement: Expediente semanal
O sistema SHALL permitir definir várias faixas de horário por dia da semana; salvar MUST substituir todas as faixas do
psicólogo, cada faixa MUST terminar depois de começar e faixas do mesmo dia MUST NOT se sobrepor.

**Origem**: RC-03 · UC27 · RN-68

#### Scenario: Salvar substitui o expediente
- **GIVEN** o psicólogo atende segunda das 08:00 às 12:00
- **WHEN** ele salva apenas terça das 13:00 às 18:00
- **THEN** o expediente passa a ter só a faixa de terça

#### Scenario: Duas faixas no mesmo dia
- **WHEN** o psicólogo salva segunda 08:00–12:00 e segunda 13:00–18:00
- **THEN** o sistema aceita as duas faixas

#### Scenario: Faixas sobrepostas
- **WHEN** o psicólogo salva segunda 08:00–12:00 e segunda 11:00–14:00
- **THEN** o sistema responde 422 com `code = psychologist.overlapping_working_hours`

#### Scenario: Fim antes do início
- **WHEN** o psicólogo salva uma faixa das 14:00 às 10:00
- **THEN** o sistema responde 422 com `code = psychologist.invalid_working_hours`

#### Scenario: Gestão altera o expediente de um psicólogo
- **WHEN** um Manager altera o expediente de um psicólogo da empresa
- **THEN** o sistema grava o novo expediente

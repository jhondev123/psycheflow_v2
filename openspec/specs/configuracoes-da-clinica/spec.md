# Configurações da clínica — Especificação

## Purpose

Manter as configurações da clínica que alimentam outras regras: duração padrão da sessão, valor padrão da sessão e fuso
horário usado para interpretar a agenda e o "agora" da clínica.

## Requirements

### Requirement: Consulta das configurações
O sistema SHALL permitir que qualquer usuário interno consulte a duração padrão da sessão (minutos), o valor padrão da
sessão (opcional) e o fuso horário da clínica.

**Origem**: RF004 · RF011 · RN-32 · RN-51

#### Scenario: Valores padrão de uma clínica nova
- **GIVEN** uma clínica recém-criada
- **WHEN** um usuário consulta `GET /api/v1/settings`
- **THEN** o sistema devolve `sessionDurationMinutes = 50`, `timeZone = America/Sao_Paulo` e nenhum valor padrão

### Requirement: Alteração das configurações
O sistema SHALL permitir que apenas Admin e Manager alterem as configurações; a duração MUST ficar entre 15 e 240 minutos,
o valor padrão MUST NOT ser negativo e o fuso MUST ser um identificador IANA válido.

**Origem**: RN-32 · RN-51 · A-24

#### Scenario: Gerente altera as configurações
- **WHEN** um Manager envia duração 60, valor 150,00 e fuso `America/Sao_Paulo`
- **THEN** o sistema grava os valores e passa a usá-los nos novos agendamentos

#### Scenario: Psicólogo tenta alterar
- **WHEN** um usuário apenas Psychologist chama `PUT /api/v1/settings`
- **THEN** o sistema responde 403

#### Scenario: Duração menor que 15 minutos
- **WHEN** um Admin envia `sessionDurationMinutes = 10`
- **THEN** o sistema responde 422 com erro no campo `sessionDurationMinutes`

#### Scenario: Fuso inválido
- **WHEN** um Admin envia `timeZone = Brasil/Cascavel`
- **THEN** o sistema responde 422 com `code = settings.invalid_time_zone`

#### Scenario: Valor padrão negativo
- **WHEN** um Admin envia `sessionDefaultPrice = -10`
- **THEN** o sistema responde 422 com `code = settings.invalid_price`

### Requirement: Horário local da clínica
O sistema MUST interpretar as datas e os horários da agenda, e o momento atual usado nas regras de agenda e pagamento, no
fuso horário configurado da clínica, nunca no fuso do servidor.

**Origem**: RN-30 · RN-53 · A-18

#### Scenario: Data local diferente da data UTC
- **GIVEN** a clínica usa `America/Sao_Paulo` (UTC−3) e o instante atual é 13/10/2026 01:00 UTC
- **WHEN** o psicólogo agenda uma sessão para 12/10/2026 às 23:00
- **THEN** o agendamento é aceito, pois ainda são 22:00 de 12/10/2026 na clínica

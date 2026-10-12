# Assistente de IA — Especificação

## Purpose

Oferecer ao psicólogo sugestões geradas por IA (organização das anotações da sessão, análise do acompanhamento e próximos
passos) somente com o consentimento da clínica, sobre os tipos de dado liberados, com dados pseudonimizados e uso auditado.

## Requirements

### Requirement: Habilitação da IA pela clínica
O sistema SHALL permitir que Admin e Manager habilitem a IA escolhendo o provedor (Claude, OpenAI ou Gemini) e os dados
liberados (anotações de sessão, feedbacks, prontuários); habilitar MUST exigir aceite dos termos, ao menos um tipo de dado e
provedor com chave configurada no servidor.

**Origem**: RF021 · UC22 · RN-67 · D-07

#### Scenario: IA habilitada
- **WHEN** um Admin habilita a IA com o provedor Claude, libera anotações de sessão e aceita os termos
- **THEN** o sistema grava a configuração com a data do aceite e o usuário que aceitou

#### Scenario: Sem aceite dos termos
- **WHEN** a habilitação é enviada com `acceptTerms = false`
- **THEN** o sistema responde 422 no campo `acceptTerms`

#### Scenario: Nenhum dado liberado
- **WHEN** a habilitação é enviada sem nenhum tipo de dado liberado
- **THEN** o sistema responde 422 com `code = ai.no_data_shared`

#### Scenario: Provedor sem chave no servidor
- **WHEN** a habilitação escolhe um provedor sem chave configurada
- **THEN** o sistema responde 422 com `code = ai.provider_not_configured`

#### Scenario: Psicólogo tenta alterar a configuração
- **WHEN** um usuário apenas Psychologist chama `PUT /api/v1/ai/settings`
- **THEN** o sistema responde 403

### Requirement: Revogação do consentimento
Desabilitar a IA MUST apagar o aceite dos termos e bloquear todas as sugestões até nova habilitação.

**Origem**: RN-67 · UC22

#### Scenario: IA desabilitada
- **GIVEN** a clínica desabilitou a IA
- **WHEN** o psicólogo pede uma sugestão
- **THEN** o sistema responde 403 com `code = ai.disabled`

### Requirement: Sugestão de anotações da sessão
O sistema SHALL gerar, para o psicólogo da sessão, um registro de evolução organizado a partir do rascunho enviado ou das
anotações salvas, usando como contexto as três sessões anteriores; MUST exigir que as anotações de sessão estejam liberadas.

**Origem**: RF021 · UC22

#### Scenario: Organizar rascunho
- **WHEN** o psicólogo da sessão pede a sugestão enviando um rascunho
- **THEN** o sistema devolve a sugestão em Markdown com provedor, modelo, data e um aviso de revisão

#### Scenario: Rascunho vazio
- **WHEN** a sugestão é pedida sem rascunho e sem anotações salvas
- **THEN** o sistema responde 422 com `code = ai.empty_draft`

#### Scenario: Sessão de outro psicólogo
- **WHEN** um psicólogo pede sugestão para a sessão de um colega
- **THEN** o sistema responde 403 com `code = ai.not_your_session`

#### Scenario: Anotações não liberadas
- **GIVEN** a clínica não liberou as anotações de sessão
- **WHEN** o psicólogo pede a sugestão de anotações
- **THEN** o sistema responde 403 com `code = ai.data_not_shared`

### Requirement: Análise do paciente e próximos passos
O sistema SHALL gerar para o psicólogo uma análise do acompanhamento e sugestões de próximos passos de um paciente, usando só
as sessões concluídas e os prontuários do próprio psicólogo com o paciente e só os tipos de dado liberados.

**Origem**: RF021 · UC22

#### Scenario: Análise gerada
- **WHEN** o psicólogo pede a análise de um paciente com sessões concluídas
- **THEN** o sistema devolve a análise em Markdown com o aviso de revisão

#### Scenario: Sem dados clínicos autorizados
- **WHEN** o paciente não tem sessões nem prontuários do psicólogo nos tipos liberados
- **THEN** o sistema responde 422 com `code = ai.insufficient_data`

#### Scenario: Usuário sem perfil de psicólogo
- **WHEN** um Manager sem perfil de psicólogo pede uma análise
- **THEN** o sistema responde 403 com `code = ai.only_psychologists`

### Requirement: Contexto limitado e pseudonimizado
Antes de sair para o provedor, o texto MUST ser pseudonimizado — sem nome, CPF, e-mail, telefone ou endereço do paciente,
idade no lugar da data de nascimento e prenomes comuns de terceiros mascarados — e limitado às 10 últimas sessões e 10
registros, com cada texto cortado em 4.000 caracteres.

**Origem**: RN-67 · RNF001 · DT-26

#### Scenario: Nome do paciente não sai
- **GIVEN** as anotações citam o nome completo e o CPF do paciente
- **WHEN** o psicólogo pede uma sugestão
- **THEN** o texto enviado ao provedor não contém o nome nem o CPF

### Requirement: Auditoria e limites de uso
O sistema MUST registrar cada sugestão gerada (usuário, paciente, tipo, provedor, modelo e tamanho do prompt), sem guardar o
prompt nem a resposta, e MUST limitar as sugestões a 20 por minuto por usuário (valor configurável).

**Origem**: RN-67 · RNF001 · DT-23

#### Scenario: Uso auditado
- **WHEN** uma sugestão é gerada
- **THEN** existe um registro de uso com tipo, provedor e modelo, sem o texto enviado nem a resposta

#### Scenario: Limite por usuário
- **GIVEN** o psicólogo pediu 20 sugestões no último minuto
- **WHEN** ele pede a 21ª
- **THEN** o sistema responde 429 com `code = rate_limit.exceeded`

### Requirement: Falhas do provedor
O sistema SHALL responder 503 quando o provedor escolhido estiver indisponível ou não responder e 422 quando o provedor
recusar gerar a sugestão, com mensagem em pt-BR para o psicólogo.

**Origem**: RF021

#### Scenario: Provedor fora do ar
- **WHEN** o provedor de IA não responde a tempo
- **THEN** o sistema responde 503 com `code = ai.provider_failed`

#### Scenario: Recusa do provedor
- **WHEN** o provedor recusa gerar a sugestão para o conteúdo
- **THEN** o sistema responde 422 com `code = ai.refused`

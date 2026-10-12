# Laudos e relatórios psicológicos — Especificação

## Purpose

Permitir que o psicólogo elabore laudos e relatórios psicológicos de um paciente seguindo os modelos da Resolução CFP nº
06/2019, com rascunho editável, finalização com as seções obrigatórias e emissão em PDF, sob acesso exclusivo do autor.

## Requirements

### Requirement: Criação de laudo ou relatório psicológico
O sistema SHALL permitir que o psicólogo crie, como rascunho, um documento para um paciente escolhendo o modelo (laudo
psicológico ou relatório psicológico), informando a finalidade e, opcionalmente, demanda, procedimento, análise e conclusão.

**Origem**: RF016 · UC17 · RN-62

#### Scenario: Rascunho criado
- **WHEN** o psicólogo cria um laudo para um paciente com a finalidade "Avaliação para afastamento"
- **THEN** o sistema responde 201 com o documento em `Draft`

#### Scenario: Campo obrigatório ausente
- **WHEN** o documento é criado sem paciente ou sem finalidade
- **THEN** o sistema responde 422 e nada é criado

#### Scenario: Usuário sem perfil de psicólogo
- **WHEN** um Manager sem perfil de psicólogo tenta criar um laudo
- **THEN** o sistema responde 403 com `code = psychological_report.only_psychologists`

### Requirement: Resumo das sessões no documento
O sistema SHALL permitir incluir no documento um resumo das sessões concluídas do autor com o paciente.

**Origem**: RF016 · RN-62

#### Scenario: Incluir resumo das sessões
- **WHEN** o psicólogo marca `includeSessionSummary` e gera o PDF
- **THEN** o PDF traz o resumo das sessões concluídas dele com o paciente

### Requirement: Rascunho editável e finalização
O sistema SHALL permitir editar e excluir o documento enquanto for rascunho; a finalização MUST exigir todas as seções
preenchidas e, depois de finalizado, o documento MUST NOT ser editado nem excluído.

**Origem**: RF016 · UC17

#### Scenario: Finalização com seção vazia
- **WHEN** o psicólogo finaliza um laudo sem a conclusão
- **THEN** o sistema responde 422 com `code = psychological_report.section_required` no campo da seção

#### Scenario: Documento finalizado
- **WHEN** o psicólogo finaliza um documento com todas as seções preenchidas
- **THEN** o documento passa para `Finalized` com a data de finalização

#### Scenario: Editar documento finalizado
- **WHEN** alguém tenta editar ou excluir um documento finalizado
- **THEN** o sistema responde 409 com `code = psychological_report.already_finalized`

### Requirement: Emissão em PDF
O sistema SHALL emitir o documento em PDF com o cabeçalho da clínica, do psicólogo e do CRP; o PDF de um rascunho MUST
trazer a marca d'água "RASCUNHO".

**Origem**: RF016

#### Scenario: PDF do rascunho
- **WHEN** o psicólogo baixa o PDF de um rascunho
- **THEN** o PDF sai com a marca d'água "RASCUNHO"

#### Scenario: PDF do documento finalizado
- **WHEN** o psicólogo baixa o PDF de um documento finalizado
- **THEN** o PDF sai sem marca d'água

### Requirement: Acesso exclusivo do autor
Os laudos e relatórios MUST ser acessados somente pelo psicólogo que os criou; a listagem SHALL mostrar apenas os
documentos do psicólogo logado, com filtro opcional por paciente.

**Origem**: RD002 · D-02

#### Scenario: Colega tenta abrir o laudo
- **WHEN** outro psicólogo da clínica abre o documento
- **THEN** o sistema responde 403 com `code = psychological_report.only_author`

#### Scenario: Lista do psicólogo
- **WHEN** o psicólogo lista os documentos de um paciente
- **THEN** aparecem só os documentos que ele mesmo criou

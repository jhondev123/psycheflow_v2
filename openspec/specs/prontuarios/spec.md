# Prontuários — Especificação

## Purpose

Guardar com segurança os registros de prontuário de cada paciente, com texto e anexos, busca rápida, acesso exclusivo do
psicólogo autor e trilha de todos os acessos, atendendo à confidencialidade e às normas do CFP sobre prontuário eletrônico.

## Requirements

### Requirement: Criação de registro de prontuário
O sistema SHALL permitir que o psicólogo crie registros de prontuário para um paciente com título e conteúdo em Markdown;
paciente, título e conteúdo MUST ser obrigatórios.

**Origem**: RF019 · UC20 · RN-65

#### Scenario: Registro criado
- **WHEN** o psicólogo cria um registro com paciente, título "Anamnese" e conteúdo
- **THEN** o sistema responde 201 com o registro vinculado ao paciente

#### Scenario: Paciente não informado
- **WHEN** o registro é enviado sem paciente
- **THEN** o sistema responde 422 com erro no campo `patientId`

#### Scenario: Usuário sem perfil de psicólogo
- **WHEN** um Manager sem perfil de psicólogo tenta criar um registro
- **THEN** o sistema responde 403 com `code = medical_record.only_psychologists`

### Requirement: Anexos do prontuário
O sistema SHALL aceitar anexos PDF, JPG ou PNG de até 10 MB por arquivo, verificando o tipo pela assinatura do conteúdo e não
pela extensão; os anexos MUST poder ser baixados e removidos pelo autor.

**Origem**: RF019 · RF020 · RN-65 · D-06

#### Scenario: Anexo PDF aceito
- **WHEN** o psicólogo envia um PDF de 2 MB
- **THEN** o sistema responde 201 e o anexo aparece no registro

#### Scenario: Arquivo grande demais
- **WHEN** o psicólogo envia um arquivo de 12 MB
- **THEN** o sistema responde 422 com `code = attachment.too_large`

#### Scenario: Extensão falsa
- **WHEN** o psicólogo envia um executável renomeado para `.pdf`
- **THEN** o sistema responde 422 com `code = attachment.unsupported_type`

#### Scenario: Download do anexo
- **WHEN** o psicólogo baixa um anexo do próprio registro
- **THEN** o navegador recebe o arquivo com o nome original

### Requirement: Busca de prontuários
O sistema SHALL listar os registros do psicólogo de forma paginada, com filtros por paciente, período de criação e
palavra-chave no título ou no conteúdo.

**Origem**: RF020 · UC21 · RN-66

#### Scenario: Busca por palavra-chave
- **WHEN** o psicólogo busca "ansiedade"
- **THEN** aparecem os registros dele cujo título ou conteúdo contém a palavra

#### Scenario: Nada encontrado
- **WHEN** nenhum registro atende à busca
- **THEN** a tela mostra "Nenhum prontuário encontrado"

### Requirement: Edição e exclusão de registros
O sistema SHALL permitir ao autor editar título e conteúdo e excluir um registro; a exclusão MUST ser lógica, preservando a
guarda do prontuário.

**Origem**: RF020 · RD001 · RN-02

#### Scenario: Registro excluído
- **WHEN** o psicólogo exclui um registro
- **THEN** o sistema responde 204, o registro some da busca e continua guardado no banco

### Requirement: Acesso exclusivo do autor
Cada registro de prontuário MUST ser acessado somente pelo psicólogo que o criou, inclusive dentro da mesma clínica.

**Origem**: RD002 · RN-66 · D-02

#### Scenario: Colega tenta abrir o registro
- **WHEN** outro psicólogo da clínica abre o registro
- **THEN** o sistema responde 403 com `code = medical_record.only_author`

#### Scenario: Registro de outra empresa
- **WHEN** um usuário consulta um registro de outra empresa
- **THEN** o sistema responde 404

### Requirement: Trilha de acesso ao prontuário
O sistema MUST registrar cada leitura de registro, cada download de anexo e cada tentativa negada, com usuário e data, e SHALL
mostrar essa trilha ao autor, da mais recente para a mais antiga.

**Origem**: RD002 · RNF001 · DT-24

#### Scenario: Leitura registrada
- **WHEN** o autor abre um registro
- **THEN** a trilha ganha uma entrada `Viewed` com o usuário e a data

#### Scenario: Tentativa negada registrada
- **WHEN** um colega tenta abrir o registro e recebe 403
- **THEN** a trilha ganha uma entrada `Denied` com o usuário do colega

#### Scenario: Consulta da trilha
- **WHEN** o autor consulta a trilha de acessos do registro
- **THEN** o sistema devolve as entradas `Viewed`, `AttachmentDownloaded` e `Denied`, das mais recentes para as mais antigas

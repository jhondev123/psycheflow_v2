# Pacientes — Especificação

## Purpose

Cadastrar, consultar e manter os pacientes da clínica com dados de identificação, contato e endereço validados no padrão
brasileiro, garantindo um cadastro único por CPF em cada empresa.

## Requirements

### Requirement: Cadastro de paciente
O sistema SHALL permitir cadastrar pacientes com nome, CPF, e-mail e telefone obrigatórios e data de nascimento, endereço e
observações opcionais; todo paciente novo MUST nascer com a situação Ativo.

**Origem**: RF001 · UC01 · RN-20 · RN-25

#### Scenario: Paciente cadastrado
- **WHEN** o psicólogo envia nome, CPF válido, e-mail e telefone para `POST /api/v1/patients`
- **THEN** o sistema responde 201 com o paciente na situação `Active`

#### Scenario: Campo obrigatório ausente
- **WHEN** o cadastro é enviado sem telefone
- **THEN** o sistema responde 422 com erro no campo `phone` e nada é gravado
- **AND** o formulário mostra a mensagem junto do campo

### Requirement: CPF válido
O sistema MUST aceitar apenas CPF com 11 dígitos, dígitos verificadores corretos e dígitos não todos iguais, aceitando a
máscara na entrada e guardando somente os dígitos.

**Origem**: RF001 · RN-21

#### Scenario: Dígitos repetidos
- **WHEN** o cadastro é enviado com CPF `111.111.111-11`
- **THEN** o sistema responde 422 com `code = patient.invalid_cpf` no campo `cpf`

#### Scenario: Dígito verificador errado
- **WHEN** o cadastro é enviado com um CPF cujo último dígito está errado
- **THEN** o sistema responde 422 no campo `cpf`

#### Scenario: CPF com máscara
- **WHEN** o cadastro é enviado com um CPF válido formatado como `529.982.247-25`
- **THEN** o paciente é gravado com o CPF `52998224725`

### Requirement: CPF único por empresa
O sistema MUST impedir dois pacientes com o mesmo CPF na mesma empresa; empresas diferentes MAY cadastrar o mesmo CPF.

**Origem**: UC01 · RN-22

#### Scenario: CPF repetido na mesma empresa
- **GIVEN** a empresa já tem um paciente com o CPF `52998224725`
- **WHEN** alguém da mesma empresa cadastra outro paciente com esse CPF
- **THEN** o sistema responde 409 com `code = patient.cpf_already_registered`

#### Scenario: Mesmo CPF em outra empresa
- **WHEN** uma empresa diferente cadastra um paciente com o mesmo CPF
- **THEN** o sistema responde 201

### Requirement: Contato válido
O sistema MUST validar o e-mail do paciente e o telefone no padrão brasileiro, com DDD e 10 ou 11 dígitos.

**Origem**: RF001 · RF003 · RN-23 · RN-24

#### Scenario: E-mail inválido
- **WHEN** o cadastro é enviado com e-mail `ana@`
- **THEN** o sistema responde 422 no campo `email`

#### Scenario: Telefone sem DDD
- **WHEN** o cadastro é enviado com telefone `99999-1234`
- **THEN** o sistema responde 422 no campo `phone`

### Requirement: Endereço com preenchimento pelo CEP
O sistema SHALL aceitar um endereço opcional com CEP de 8 dígitos e UF válida; no formulário, o CEP MUST ser o primeiro
campo e, ao ser informado, MUST preencher rua, bairro, cidade e UF.

**Origem**: RF001 · RN-26

#### Scenario: CEP preenche o endereço
- **WHEN** o psicólogo digita um CEP existente no formulário de paciente
- **THEN** rua, bairro, cidade e UF são preenchidos automaticamente e ele completa número e complemento

#### Scenario: CEP incompleto
- **WHEN** o cadastro é enviado com CEP de 7 dígitos
- **THEN** o sistema responde 422 com a mensagem "CEP inválido. Informe os 8 dígitos." no campo `address.zipCode`

#### Scenario: UF inexistente
- **WHEN** o cadastro é enviado com a UF `XX`
- **THEN** o sistema responde 422 no campo `address.state`

### Requirement: Edição de paciente
O sistema SHALL permitir editar nome, e-mail, telefone, situação, data de nascimento, endereço e observações com as mesmas
validações do cadastro; o CPF MUST NOT ser editável e a data de atualização MUST ser registrada.

**Origem**: RF003 · UC02 · RN-27 · RN-03

#### Scenario: Paciente editado
- **WHEN** o psicólogo altera o telefone e salva
- **THEN** o sistema grava a alteração e atualiza `updatedAt`

#### Scenario: CPF não muda na edição
- **WHEN** o paciente é editado
- **THEN** o CPF permanece o mesmo e aparece somente leitura no formulário

#### Scenario: Erro ao salvar
- **WHEN** a edição tem um campo inválido
- **THEN** o sistema responde 422 e nenhum dado do paciente é alterado

### Requirement: Inativação em vez de exclusão
O sistema SHALL preservar o cadastro do paciente: em vez de excluí-lo, o paciente MUST ser marcado como Inativo, e um
paciente inativo MUST NOT receber novos agendamentos.

**Origem**: RF003 · RN-35

#### Scenario: Paciente inativado
- **WHEN** o psicólogo muda a situação do paciente para `Inactive`
- **THEN** o paciente continua consultável com o histórico

#### Scenario: Agendar para paciente inativo
- **GIVEN** o paciente está inativo
- **WHEN** o psicólogo tenta agendar uma sessão para ele
- **THEN** o sistema recusa com `code = session.patient_inactive`

### Requirement: Consulta e filtros de pacientes
O sistema SHALL listar os pacientes de forma paginada (20 por página, máximo 100), com busca por parte do nome, do e-mail ou
pelo CPF completo, filtro por situação e filtro pelo período da última sessão concluída.

**Origem**: RF002 · UC03

#### Scenario: Busca por nome
- **WHEN** o psicólogo busca `ana`
- **THEN** aparecem os pacientes cujo nome ou e-mail contém "ana", sem diferenciar maiúsculas

#### Scenario: Filtro pela última sessão
- **WHEN** o psicólogo filtra a última sessão entre 01/09/2026 e 30/09/2026
- **THEN** aparecem só os pacientes cuja última sessão concluída caiu nesse período

#### Scenario: Nenhum resultado
- **WHEN** nenhum paciente atende aos filtros
- **THEN** o sistema devolve uma página vazia e a tela mostra o estado vazio

### Requirement: Pacientes visíveis a toda a clínica
O sistema SHALL mostrar os pacientes a todos os usuários internos da mesma empresa e MUST esconder pacientes de outras
empresas.

**Origem**: D-02 · RN-01 · RD002

#### Scenario: Colega da mesma clínica
- **GIVEN** a psicóloga A cadastrou um paciente
- **WHEN** o psicólogo B, da mesma empresa, abre a lista
- **THEN** o paciente aparece para B

#### Scenario: Paciente de outra empresa
- **WHEN** um usuário consulta o id de um paciente de outra empresa
- **THEN** o sistema responde 404

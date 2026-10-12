# Usuários da clínica — Especificação

## Purpose

Organizar quem acessa a clínica: perfis de acesso (Admin, Gerente, Psicólogo), cadastro de membros da equipe por Admin e
Gerente com senha temporária, e consulta da equipe da própria empresa.

## Requirements

### Requirement: Perfis de acesso
O sistema SHALL trabalhar com os perfis internos Admin, Manager (Gerente) e Psychologist; o perfil Patient MUST existir
apenas como reserva para um futuro portal do paciente, sem acesso às rotas internas.

**Origem**: RC-02 · D-01 · A-14

#### Scenario: Rota interna exige perfil interno
- **GIVEN** um token sem nenhum dos perfis Admin, Manager ou Psychologist
- **WHEN** ele chama uma rota interna da API
- **THEN** o sistema responde 403

### Requirement: Cadastro de membro da equipe
O sistema SHALL permitir que Admin e Manager cadastrem usuários informando nome, e-mail, perfil (Admin, Manager ou
Psychologist) e, para psicólogos, CRP e abordagem. O usuário MUST ser criado na empresa de quem cadastra, com uma senha
temporária devolvida uma única vez e troca obrigatória no primeiro acesso.

**Origem**: RC-02 · UC26 · A-25

#### Scenario: Admin cadastra um psicólogo
- **WHEN** um Admin cadastra um usuário com perfil Psychologist e CRP `06/12345`
- **THEN** o sistema responde 201 com `temporaryPassword` e `psychologistId`
- **AND** o novo usuário pertence à empresa do Admin e precisa trocar a senha ao entrar

#### Scenario: Empresa informada pelo cliente é ignorada
- **WHEN** a requisição tenta indicar outra empresa para o novo usuário
- **THEN** o usuário é criado na empresa de quem cadastra

#### Scenario: Psicólogo sem CRP
- **WHEN** um Manager cadastra um usuário Psychologist sem CRP
- **THEN** o sistema responde 422 com erro no campo `licenseNumber`

#### Scenario: E-mail já usado
- **WHEN** um Admin cadastra um usuário com e-mail que já existe
- **THEN** o sistema responde 409 com `code = auth.email_in_use`

#### Scenario: Senha temporária mostrada uma vez
- **WHEN** o cadastro é concluído na tela de Usuários
- **THEN** o front mostra a senha temporária uma única vez para ser repassada ao novo usuário

### Requirement: Somente Admin cadastra Admin
O sistema MUST permitir que apenas usuários Admin cadastrem outro Admin.

**Origem**: RC-02

#### Scenario: Gerente tenta cadastrar Admin
- **WHEN** um Manager cadastra um usuário com perfil Admin
- **THEN** o sistema responde 403 com `code = user.only_admin_can_create_admin`

### Requirement: Psicólogo não gerencia a equipe
O sistema MUST restringir o cadastro e a listagem de usuários da equipe aos perfis Admin e Manager.

**Origem**: RC-02 · D-02

#### Scenario: Psicólogo tenta cadastrar usuário
- **WHEN** um usuário apenas Psychologist chama `POST /api/v1/users`
- **THEN** o sistema responde 403

### Requirement: Consulta da equipe
O sistema SHALL listar para Admin e Manager os usuários da própria empresa com nome, e-mail, perfis, psicólogo vinculado,
troca de senha pendente e bloqueio por tentativas.

**Origem**: RC-02 · RN-01

#### Scenario: Somente a própria empresa
- **GIVEN** as empresas A e B têm usuários
- **WHEN** um Admin da empresa A lista os usuários
- **THEN** só aparecem usuários da empresa A, com `mustChangePassword` e `isLockedOut`

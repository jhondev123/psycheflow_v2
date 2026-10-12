# Autenticação — Especificação

## Purpose

Garantir o acesso ao Psycheflow: criação da conta da clínica, login por e-mail e senha com token JWT, proteção contra
tentativas repetidas, troca obrigatória de senha temporária, consulta do usuário logado e logout.

## Requirements

### Requirement: Criação de conta da clínica
O sistema SHALL permitir que um visitante crie uma conta informando nome da clínica, nome completo, e-mail, senha, CRP,
abordagem e telefone opcional. Numa única transação, o sistema MUST criar a empresa com as configurações padrão, o usuário
com os perfis Admin e Psychologist e o perfil de psicólogo, e devolver um token de acesso.

**Origem**: RC-01 · UC25

#### Scenario: Conta criada com sucesso
- **WHEN** o visitante envia nome da clínica, nome, e-mail novo, senha forte e CRP `06/12345`
- **THEN** o sistema responde 201 com `accessToken`, `expiresAt` e `mustChangePassword = false`
- **AND** existem a empresa, o usuário Admin + Psychologist e o perfil de psicólogo, todos da nova empresa

#### Scenario: Falha em qualquer etapa não deixa dados parciais
- **WHEN** a criação de qualquer registro da conta falha
- **THEN** nada é gravado (nem empresa, nem usuário, nem perfil)

#### Scenario: Nome da clínica ausente
- **WHEN** o visitante envia o cadastro sem `companyName`
- **THEN** o sistema responde 422 com erro no campo `companyName`

### Requirement: E-mail de login único
O sistema MUST impedir que dois usuários usem o mesmo e-mail, comparando-o sem diferenciar maiúsculas e minúsculas.

**Origem**: RN-13

#### Scenario: E-mail já usado no cadastro de conta
- **GIVEN** já existe um usuário com o e-mail `ana@clinica.com`
- **WHEN** um visitante cria uma conta com `ANA@clinica.com`
- **THEN** o sistema responde 409 com `code = auth.email_in_use` e nada é criado

### Requirement: Política de senha
O sistema MUST aceitar apenas senhas com no mínimo 6 caracteres contendo letra maiúscula, letra minúscula, número e símbolo,
tanto na criação de conta quanto na troca de senha.

**Origem**: RN-11

#### Scenario: Senha fraca rejeitada
- **WHEN** o visitante cria uma conta com a senha `senha123`
- **THEN** o sistema responde 422 com `code = auth.weak_password` no campo da senha

#### Scenario: Front avisa antes de enviar
- **WHEN** o usuário digita uma senha sem símbolo na tela de cadastro
- **THEN** a tela lista o que falta na senha antes de chamar a API

### Requirement: Login por e-mail e senha
O sistema SHALL autenticar usuários por e-mail e senha e devolver um token JWT válido por 2 horas, contendo o usuário, os
perfis, a empresa e, quando houver, o psicólogo vinculado.

**Origem**: RF022 · UC23 · RN-10

#### Scenario: Credenciais válidas
- **WHEN** o usuário envia e-mail e senha corretos para `POST /api/v1/auth/login`
- **THEN** o sistema responde 200 com o token e a data de expiração (2 horas)
- **AND** o front guarda o token e abre o painel

#### Scenario: Credenciais inválidas
- **WHEN** o usuário envia uma senha errada ou um e-mail inexistente
- **THEN** o sistema responde 401 com a mensagem genérica "E-mail ou senha inválidos." (`auth.invalid_credentials`)

### Requirement: Bloqueio temporário por tentativas
O sistema MUST bloquear a conta por 15 minutos após 5 tentativas de login com senha errada.

**Origem**: RN-12 · UC23

#### Scenario: Quinta tentativa errada bloqueia
- **GIVEN** um usuário errou a senha 4 vezes seguidas
- **WHEN** ele erra a senha pela quinta vez
- **THEN** a conta fica bloqueada por 15 minutos

#### Scenario: Login durante o bloqueio
- **GIVEN** a conta está bloqueada
- **WHEN** o usuário envia a senha correta
- **THEN** o sistema responde 423 com `code = auth.locked_out`

### Requirement: Limite de requisições no acesso
O sistema MUST limitar as chamadas de login e de criação de conta a 10 por minuto por endereço IP (valor configurável) e
responder 429 com o cabeçalho `Retry-After` quando o limite for excedido.

**Origem**: RNF001 · DT-23

#### Scenario: Excesso de tentativas pelo mesmo IP
- **GIVEN** o mesmo IP fez 10 chamadas de login no último minuto
- **WHEN** ele faz a 11ª chamada
- **THEN** o sistema responde 429 com `code = rate_limit.exceeded` e `Retry-After` em segundos

### Requirement: Troca obrigatória de senha temporária
Enquanto o usuário tiver senha temporária, o sistema MUST permitir apenas a troca de senha e a consulta dos próprios
dados; a troca exige a senha atual e devolve um novo token sem a restrição.

**Origem**: RC-02 · UC26 · A-25

#### Scenario: Acesso bloqueado até trocar a senha
- **GIVEN** um usuário entrou com a senha temporária
- **WHEN** ele chama qualquer rota além de `POST /auth/change-password` e `GET /auth/me`
- **THEN** o sistema responde 403
- **AND** o front leva o usuário para a tela de troca de senha

#### Scenario: Senha atual incorreta
- **WHEN** o usuário pede a troca informando a senha atual errada
- **THEN** o sistema responde 422 com `code = auth.password_mismatch` no campo `currentPassword`

#### Scenario: Troca concluída
- **WHEN** o usuário informa a senha atual correta e uma nova senha forte
- **THEN** o sistema devolve um novo token com `mustChangePassword = false` e libera as demais rotas

### Requirement: Dados do usuário logado
O sistema SHALL informar ao usuário autenticado seu id, nome, e-mail, perfis, empresa, psicólogo vinculado e se precisa
trocar a senha.

**Origem**: RF022

#### Scenario: Consulta do próprio usuário
- **WHEN** um usuário autenticado chama `GET /api/v1/auth/me`
- **THEN** o sistema devolve `id`, `fullName`, `email`, `roles`, `companyId`, `companyName`, `psychologistId` e `mustChangePassword`

### Requirement: Logout
O sistema SHALL permitir que o usuário saia, descartando o token no front e voltando à tela de login; um token expirado ou
inválido MUST levar o usuário de volta ao login.

**Origem**: RF023 · UC24 · RN-15

#### Scenario: Usuário clica em Sair
- **WHEN** o usuário escolhe "Sair" no menu
- **THEN** o front descarta o token e mostra a tela de login

#### Scenario: Token expirado
- **GIVEN** o token do usuário expirou
- **WHEN** o front faz uma chamada e recebe 401
- **THEN** o front descarta a sessão e redireciona para o login

# Plataforma — Especificação

## Purpose

Reunir as regras transversais que valem para toda a API: isolamento dos dados por empresa, exclusão lógica, registro de
datas de criação e alteração, autenticação obrigatória por padrão, respostas de erro padronizadas, paginação e verificação
de saúde do serviço.

## Requirements

### Requirement: Isolamento por empresa
Todo dado de negócio MUST pertencer a uma empresa, e um usuário MUST ler e alterar apenas dados da própria empresa; dados de
outras empresas MUST ser tratados como inexistentes.

**Origem**: RN-01 · RD002 · RNF001

#### Scenario: Recurso de outra empresa
- **GIVEN** a empresa B tem uma sessão
- **WHEN** um usuário da empresa A consulta essa sessão pelo id
- **THEN** o sistema responde 404, como se ela não existisse

#### Scenario: Empresa preenchida pelo servidor
- **WHEN** um usuário cria qualquer registro
- **THEN** o registro recebe a empresa do usuário logado, independentemente do que o cliente enviar

### Requirement: Exclusão lógica
Excluir um registro de negócio MUST marcá-lo como excluído em vez de apagá-lo; registros excluídos MUST NOT aparecer em
listas nem contar em validações como unicidade e conflito de horário.

**Origem**: RN-02 · RD001

#### Scenario: Registro excluído some das listas
- **WHEN** um registro é excluído
- **THEN** ele continua no banco com a data de exclusão, mas não aparece mais nas consultas

### Requirement: Datas de criação e alteração
O sistema MUST registrar em UTC a data de criação e a data da última alteração de todo registro de negócio.

**Origem**: RN-03 · UC02

#### Scenario: Alteração registrada
- **WHEN** um paciente é editado
- **THEN** `updatedAt` passa a ter o instante da alteração em UTC

### Requirement: Autenticação obrigatória por padrão
Toda rota da API MUST exigir um usuário autenticado com perfil interno e senha já trocada, exceto login, criação de conta e
verificação de saúde; a documentação interativa da API SHALL existir só no ambiente de desenvolvimento.

**Origem**: RNF001 · DT-02

#### Scenario: Chamada sem token
- **WHEN** uma rota protegida é chamada sem token
- **THEN** o sistema responde 401 em ProblemDetails

#### Scenario: Documentação fora do desenvolvimento
- **WHEN** `/scalar` é acessado em produção
- **THEN** a documentação interativa não está disponível

### Requirement: Erros padronizados
Toda resposta de erro MUST seguir ProblemDetails (RFC 9457) com título e detalhe em pt-BR, `code` estável, erros por campo
quando houver e `traceId`; uma falha MUST NOT deixar gravações parciais e um erro inesperado MUST NOT expor detalhes
internos fora do desenvolvimento.

**Origem**: RN-04 · UC01–UC24 (fluxos alternativos)

#### Scenario: Erro de validação
- **WHEN** um cadastro é enviado com CPF e CEP inválidos
- **THEN** o sistema responde 422 com `code = validation_failed` e `errors` contendo `cpf` e `address.zipCode`

#### Scenario: Erro inesperado em produção
- **WHEN** ocorre uma exceção não tratada em produção
- **THEN** o sistema responde 500 em ProblemDetails sem stack trace

#### Scenario: Códigos de situação
- **WHEN** uma regra de negócio é violada
- **THEN** o sistema usa 403 para falta de permissão, 404 para inexistente na empresa, 409 para conflito de estado, 422 para dados inválidos e 423 para conta bloqueada

### Requirement: Listas paginadas
As listas que podem crescer SHALL ser paginadas com 20 itens por padrão e no máximo 100 por página, informando página,
tamanho, total de itens e total de páginas.

**Origem**: RNF002

#### Scenario: Página maior que o máximo
- **WHEN** uma lista é pedida com `pageSize = 500`
- **THEN** o sistema devolve no máximo 100 itens e informa `pageSize = 100`

### Requirement: Verificação de saúde
O sistema SHALL expor uma verificação de saúde pública que informa se a API e o banco de dados estão respondendo.

**Origem**: RC-08

#### Scenario: Serviço saudável
- **WHEN** `GET /health` é chamado com o banco no ar
- **THEN** o sistema responde 200 com `Healthy`

#### Scenario: Banco fora do ar
- **WHEN** `GET /health` é chamado sem acesso ao banco
- **THEN** o sistema responde 503 com `Unhealthy`

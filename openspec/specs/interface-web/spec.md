# Interface web — Especificação

## Purpose

Definir o comportamento comum das telas do front React: interface em português, responsiva, com tema claro e escuro,
rotas por perfil, carregamento e erros tratados de forma uniforme e validação imediata de formato antes de chamar a API.

## Requirements

### Requirement: Interface em português e responsiva
A interface SHALL estar inteira em português do Brasil, com textos curtos, e MUST funcionar em telas de computador e de
celular.

**Origem**: RNF003 · RNF-F01 · RNF-F04 · RNF-F12

#### Scenario: Uso no celular
- **WHEN** o psicólogo abre a agenda num celular
- **THEN** o layout se adapta à largura da tela sem rolagem horizontal

### Requirement: Tema claro e escuro
O front SHALL permitir alternar entre tema claro e escuro e MUST lembrar a escolha entre as visitas.

**Origem**: RC-07 · RNF-F05

#### Scenario: Escolha lembrada
- **GIVEN** o usuário escolheu o tema escuro
- **WHEN** ele volta ao sistema outro dia
- **THEN** a interface abre no tema escuro

### Requirement: Rotas por perfil
O front MUST exigir login para as telas internas, MUST mostrar a tela de usuários apenas para Admin e Manager, MUST mostrar
prontuários e perfil profissional apenas para quem tem perfil de psicólogo, e MUST levar à troca de senha quem estiver com
senha temporária.

**Origem**: RF022 · RC-02 · A-36

#### Scenario: Psicólogo não vê a tela de usuários
- **WHEN** um usuário apenas Psychologist tenta abrir a tela de usuários
- **THEN** o front não mostra a tela de gestão

#### Scenario: Gerente não vê prontuários
- **WHEN** um usuário apenas Manager tenta abrir a tela de prontuários
- **THEN** o front não mostra a tela

#### Scenario: Senha temporária
- **WHEN** um usuário com senha temporária entra no sistema
- **THEN** o front o leva para `/trocar-senha` antes de qualquer outra tela

### Requirement: Carregamento e erros uniformes
Toda tela SHALL indicar carregamento, mostrar a mensagem de erro em pt-BR devolvida pela API com a opção de tentar de novo e
MUST exibir os erros de validação junto dos campos correspondentes.

**Origem**: RN-04 · RNF003 · A-36

#### Scenario: Erro por campo
- **WHEN** a API responde 409 `patient.cpf_already_registered` ao salvar um paciente
- **THEN** o formulário mostra "Já existe um paciente com este CPF na empresa." junto do campo CPF

#### Scenario: Falha ao carregar
- **WHEN** a lista de pacientes não carrega
- **THEN** a tela mostra a mensagem de erro e o botão para tentar de novo

### Requirement: Validação imediata de formato
O front SHALL validar o formato de CPF, telefone, CRP e senha antes de enviar, para dar retorno imediato; as regras de
negócio MUST continuar sendo decididas pela API.

**Origem**: RNF-F11 · A-36

#### Scenario: CPF inválido no formulário
- **WHEN** o usuário digita um CPF com dígito verificador errado
- **THEN** o formulário avisa no campo sem chamar a API

### Requirement: Downloads de documentos e anexos
O front SHALL baixar PDFs e anexos com o nome de arquivo informado pela API.

**Origem**: RF014 · RF016 · RF020

#### Scenario: Baixar relatório
- **WHEN** o psicólogo gera o relatório de sessões
- **THEN** o navegador baixa o PDF com o nome definido pela API

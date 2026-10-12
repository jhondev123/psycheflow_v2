# ERP Psicologia — Documento de requisitos do sistema

> **Fonte histórica.** Conversão fiel de `docs/Documento requisitos Atualizado.docx` (versão 0.1), feita em 12/10/2026.
> O texto original foi mantido; só a formatação mudou. A especificação vigente está em [`openspec/specs/`](../../specs/)
> e a correspondência de IDs (RF015 duplicado, RF000 → RF024–RF026) em [`rastreabilidade.md`](../rastreabilidade.md).

**Centro Universitário Fundação Assis Gurgacz** — Cascavel, 2025
Autores: Jhonattan, Matheus Augusto e Matheus Mantovani.

## Histórico de revisão da documentação do projeto

| Data | Versão | Descrição | Autor |
|------|--------|-----------|-------|
| 15/10/2024 | 1.0 | Descrição dos requisitos funcionais e não funcionais. | |

## 1 Visão do documento

*(Seção vazia no original.)*

## 2 Tipos de requisitos

### 2.1 Requisitos funcionais

#### [RF001] Cadastro de Pacientes

**Descrição:** O sistema deve permitir o cadastro de pacientes, incluindo as seguintes informações:

- Nome\*
- Cpf\*
- Endereço
  - CEP como primeiro campo, o sistema deve consultar e trazer os outros dados
  - Rua
  - Bairro
  - Número
  - Cidade
  - Estado
  - Complemento
- Data de nascimento
- Telefone\*
- Email\*
- Status

As seguintes regras devem ser aplicadas ao cadastrar um paciente:

- Cpf precisa ser válido
- Email precisa ser válido
- Telefone precisa ser válido
- Status, só pode ser cadastrado como ativo

Prioridade: **Prioritário**

#### [RF002] Visualização de Pacientes

**Descrição:** O sistema deve permitir a visualização e filtragem de pacientes pelas seguintes informações:

- Status
- Data da última sessão (caso possua)
- Nome do paciente

Prioridade: **Prioritário**

#### [RF003] Edição Pacientes

**Descrição:** O sistema deve permitir a edição de pacientes, podendo ser alterado as seguintes informações:

- Nome
- Telefone
- Email
- Endereço
- Data de nascimento
- Status

As seguintes regras devem ser aplicadas ao editar um paciente:

- Nome precisa ser válido
- Telefone precisa ser válido
- Email precisa ser válido

Prioridade: **Prioritário**

#### [RF004] Cadastro de Sessões

**Descrição:** O sistema deve permitir o cadastro de sessões com as seguintes informações:

- Data\*
- Horário\*
- Duração (Essa informação deve ser puxada de uma configuração)\*
- Paciente\*
- Anotações (Precisa ser um campo com escrita em MARKDOWN)

As seguintes regras devem ser aplicadas ao cadastrar uma sessão:

- A data e horário não podem ser menores que a atual
- O paciente precisa já estar cadastrado
- A duração não pode ser inferior a 15 minutos

Prioridade: **Prioritário**

#### [RF005] Visualização de Sessões

**Descrição:** O sistema deve exibir as sessões em um calendário com os seguintes filtros:

- Por data, podendo sendo um intervalo entre elas (data de inicio e data fim), e pode pesquisar só pela data de inicio
- Por horário da sessão
- Por paciente

permitir reagendamento ou cancelamento, e enviar lembretes automáticos para os pacientes.

Prioridade: **Prioritário**

#### [RF006] Reagendamento de Sessões

**Descrição:** O sistema deve permitir o psicólogo realizar um reagendamento de uma sessão, solicitando as seguintes informações:

- Data (data da próxima sessão)\*
- Horário\*
- Motivo (esse é o motivo para o cancelamento)\*

Prioridade: **Prioritário**

#### [RF007] Cancelamento de Sessões

**Descrição:** O sistema deve permitir o psicólogo realizar um cancelamento de uma sessão, solicitando as seguintes informações:

- Motivo (esse é o motivo para o cancelamento)\*

Prioridade: **Prioritário**

#### [RF008] Edição de Sessões

**Descrição:** O sistema deve permitir que o psicólogo edite uma sessão. Podendo editar as seguintes informações:

- Anotações
- Cancelamento
- Exclusão
- Data
- Horário
- Duração
- Paciente

Prioridade: **Prioritário**

#### [RF009] Conclusão da sessão

**Descrição:** O sistema deve permitir ao psicólogo concluir uma sessão. Solicitando as seguintes informações:

- Anotações\*
- Feedback sobre a sessão (Uma nota de 0 a 10)\*

Prioridade: **Prioritário**

#### [RF010] Criação de recorrência da sessão

**Descrição:** O sistema deve permitir ao psicólogo criar uma recorrência de sessão por algum paciente. Solicitando as seguintes informações:

- Tipo de recorrência (Semanal, Mensal)\*
- valor das sessões

Prioridade: **Prioritário**

#### [RF011] Lançamento do pagamento da sessão

**Descrição:** O sistema deve permitir o psicólogo lançar o pagamento da sessão. Solicitando as seguintes informações:

- Valor da sessão (Puxar de uma configuração)\*
- Método de pagamento\*
  - Cartão de crédito
  - Débito
  - Pix
- Data do pagamento (não pode ser menor q a data de hoje)
- Informações extras (caso necessário)

Prioridade: **Prioritário**

#### [RF012] Visualização de Pagamentos

**Descrição:** O sistema deve permitir a visualização dos pagamentos, podendo ser filtrado pelas seguintes opções

- Paciente
- Intervalo de datas (Data de início e data final, podendo preencher só a data de início)
- Status do pagamento

Prioridade: **Prioritário**

#### [RF013] Edição de pagamentos

**Descrição:** O sistema deve permitir o psicólogo editar pagamentos, As seguintes informações podem ser editadas:

- Valor
- Status do pagamento

Regras para a edição de um pagamento:

- O pagamento não pode estar com o status pago

Prioridade: **Prioritário**

#### [RF014] Criação de Recibos de sessões

**Descrição:** O sistema deve permitir o psicólogo criar recibo de seus pagamentos, solicitando as seguintes informações:

- Sessão

Regras para a criação do recibo

- O pagamento da sessão deve estar com o status pago

Prioridade: **Prioritário**

#### [RF015] Geração de Relatórios de sessões

**Descrição:** O sistema deve gerar relatórios sobre sessões contendo as seguintes informações nele:

- Dados sobre a sessão
- Dados sobre o pagamento agrupado a sessão

O psicólogo poderia gerar o relatório com os seguintes filtros:

- Intervalo de datas (Data de início e uma data final)
- Status da sessão
- Status do pagamento

Prioridade: **Prioritário**

#### [RF015] Geração de Relatórios de feedback de pacientes

**Descrição:** O sistema deve gerar relatórios sobre informações de feedback dos pacientes o relatório deve conter as seguintes informações:

- Dados do paciente
- Feedback das sessões agrupado por sessão

O psicólogo poderia gerar o relatório com os seguintes filtros:

- Paciente\*
- Intervalo de datas (Data de início e uma data final, podendo ser só a data de início)

Prioridade: **Prioritário**

#### [RF016] Geração de Laudos

**Descrição:** O sistema deve permitir a geração de laudos com base nas anotações das sessões, com opção de personalização do formato. Para a geração do laudo as seguintes informações devem ser solicitadas

- Paciente\*
- Motivo\*
- Modelo do laudo (o sistema deve fornecer modelos de laudos)\*

Prioridade: **Prioritário**

#### [RF017] Gerenciamento da Agenda

**Descrição:** O sistema deve integrar uma agenda para visualização de todas as atividades, bloqueio de horários indisponíveis.

Prioridade: **Prioritário**

#### [RF018] Lançamento de bloqueio de agenda

**Descrição:** O sistema deve permitir o psicólogo bloquear agendamentos para um certo período de datas ou horários impedindo agendamentos nesses dias. As seguintes informações devem ser solicitadas:

- Tipo do intervalo (Dias ou horários)\*
- Valores do intervalo\*
- Motivo

Prioridade: **Prioritário**

#### [RF019] Criação de prontuários

**Descrição:** O sistema deve permitir que o psicólogo crie prontuários por paciente. As seguintes informações devem ser solicitadas:

- Paciente
- Informações do prontuário (anexos e textos)

Prioridade: **Prioritário**

#### [RF020] Gerenciamento de Prontuários

**Descrição:** O sistema deve permitir o armazenamento seguro de prontuários, com anexos, anotações detalhadas e busca rápida por informações específicas.

Prioridade: **Prioritário**

#### [RF021] Sugestões com IA

**Descrição:** O sistema deve permitir sugestões por Inteligência artificial caso o psicólogo aceita e defina quais dados podem ser usados para as respostas. Dados que podem ser utilizados:

- Histórico de feedbacks
- Histórico de sessões
- Prontuários
- Laudos

Prioridade: **Prioritário**

#### [RF022] Login do psicólogo

**Descrição:** O sistema deve permitir que o psicólogo faça login. Dados necessários:

- Usuário
- Senha

Prioridade: **Prioritário**

#### [RF023] Logout do psicólogo

**Descrição:** O sistema deve permitir que o psicólogo faça logout.

Prioridade: **Prioritário**

#### [RF000] Notificações das sessões para o paciente

**Descrição:** *(vazia no original)*

Prioridade: **Desejável**

#### [RF000] Notificações das sessões para o psicólogo

**Descrição:** *(vazia no original)*

Prioridade: **Desejável**

#### [RF000] Notificações de conflitos de agendamentos

**Descrição:** *(vazia no original)*

Prioridade: **Desejável**

### 2.2 Requisitos não funcionais

#### [RNF001] Segurança

**Descrição:** O sistema deve garantir a segurança dos dados com criptografia de ponta a ponta, autenticação de usuários e conformidade com a LGPD.

Prioridade: **Prioritário**

#### [RNF002] Desempenho

**Descrição:** O sistema deve ser capaz de processar operações rapidamente, mesmo com um grande volume de dados, garantindo tempo de resposta inferior a 2 segundos.

Prioridade: **Prioritário**

#### [RNF003] Usabilidade

**Descrição:** O sistema deve ter uma interface intuitiva e de fácil utilização, com design responsivo para acesso via desktop e dispositivos móveis.

Prioridade: **Prioritário**

#### [RNF004] Disponibilidade

**Descrição:** O sistema deve garantir uma disponibilidade mínima de 99,9%, com backups automáticos e recuperação de dados em caso de falhas.

Prioridade: **Prioritário**

### 2.3 Requisitos de domínio

#### [RD001] Aspectos Legais

**Descrição:** O sistema deve seguir as diretrizes da Resolução CFP nº 013/2015, que regulamenta a atuação do psicólogo e o uso de prontuários eletrônicos.

Prioridade: **Prioritário**

#### [RD002] Confidencialidade

**Descrição:** O sistema deve garantir a confidencialidade dos dados dos pacientes, permitindo acesso apenas a profissionais autorizados.

Prioridade: **Prioritário**

# ERP Psicologia — Documento de casos de uso

> **Fonte histórica.** Conversão fiel de `docs/Documento casos de uso Atualizado.docx`, feita em 12/10/2026.
> O texto original foi mantido (inclusive os trechos copiados de outro caso de uso, como "reagendamento" nos UC 7 e 8);
> só a formatação mudou. A versão corrigida e vigente está nos cenários de [`openspec/specs/`](../../specs/) e o índice
> UC → capacidade em [`rastreabilidade.md`](../rastreabilidade.md).

Ator principal em todos os casos: **psicólogo**.

## 1. Cadastro Paciente

**Pré-condições:**

- O psicólogo precisa estar logado
- paciente não possui uma outra conta, pois o CPF já vai estar cadastrado

**Fluxo Principal:**

- Psicólogo vai no menu e acessa a aba pacientes
- Na aba paciente vai ter um botão de cadastro
- na tela de cadastro vão ser necessários preencher os campos obrigatórios
- Depois dos campos preenchidos pode salvar o paciente

**Fluxo Alternativo:**

- Se algum campo obrigatório não for preenchido, o sistema exibe um alerta.

**Pós-condições:**

- Um novo paciente foi criado

## 2. Editar Paciente

**Pré-condições:**

- O psicólogo precisa estar logado
- O paciente precisa estar cadastrado

**Fluxo Principal:**

- O psicólogo vai no menu e acessa a aba de pacientes
- na aba de pacientes ele encontra o paciente que quer alterar, e no lado dele vai possuir o botão de editar
- Na tela de edição vai possuir os mesmos campos que na tela de cadastro, e as mesmas validações de campos
- O usuário preenche os campos alterados que deseja alterar e clica em salvar

**Fluxo Alternativo:**

- Se algum campo obrigatório não for preenchido, o sistema exibe um alerta.
- Caso haja algum erro ao editar, as alterações vão ser canceladas

**Pós-condições:**

- Dados atualizados no sistema.
- Data de atualização gravado no registro

## 3. Visualizar pacientes

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo vai no menu e acessa a aba de pacientes
- Nessa aba é possível visualizar os pacientes já cadastrados e filtrar de acordo com os filtros de pesquisa

**Fluxo Alternativo:**

- Caso o sistema não encontre pacientes de acordo com os filtros ele irá trazer os registros vazios

## 4. Cadastro de sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- O horário precisa estar vago

**Fluxo Principal:**

- O psicólogo vai no menu e acessa a aba de sessões
- Nessa tela é possível visualizar as sessões, vai ter um botão de cadastro
- Na tela de cadastro você pode preencher as informações solicitadas para cadastrar um agendamento e finalizar

**Fluxo Alternativo:**

- Caso haja algum erro na hora de criar a sessão retornará uma mensagem de erro para o usuário e cancela o processo

**Pós-condições:**

- Sessão cadastrada com sucesso
- O sistema irá criar o lançamento da conta a ser paga referente a sessão agendada com o status pendente

## 5. Visualizar sessões

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo vai no menu e acessa a aba de sessões
- Nessa aba é possível visualizar as sessões já cadastrados e filtrar de acordo com os filtros de pesquisa

**Fluxo Alternativo:**

- Caso o sistema não encontre sessões de acordo com os filtros ele irá trazer os registros vazios

## 6. Reagendamento de sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- Precisava haver uma sessão agendada
- A sessão não pode estar concluída ou cancelada

**Fluxo Principal:**

- O psicólogo vai no menu e acessa a aba de sessões
- Nessa tela é possível visualizar as sessões
- Dentro de cada registro de sessões pode ter um botão de reagendamento
- Ele irá abrir um popup pedindo a data, horário e o motivo
- se os campos forem preenchidos corretamente o reagendamento foi concluído com sucesso

**Fluxo Alternativo:**

- Caso haja algum erro na hora de reagendar a sessão retornará uma mensagem de erro para o usuário e cancela o processo

**Pós-condições:**

- Sessão reagendada com sucesso

## 7. Cancelamento da sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- Precisava haver uma sessão agendada
- A sessão não pode estar concluída ou cancelada
- Caso o pagamento da sessão esteja marcado como concluído vai ser necessário cancelar para continuar

**Fluxo Principal:**

- O psicólogo vai no menu e acessar a aba de sessões
- Nessa tela é possível visualizar as sessões
- Dentro de cada registro de sessões pode ter um botão de reagendamento
- Ele irá abrir um popup pedindo o motivo
- se os campos forem preenchidos corretamente o reagendamento foi concluído com sucesso

**Fluxo Alternativo:**

- Caso haja algum erro na hora de cancelar a sessão retornará uma mensagem de erro para o usuário e cancela o processo

**Pós-condições:**

- Sessão cancelado com sucesso
- Pagamento da sessão cancelado

## 8. Conclusão da sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- Precisava haver uma sessão agendada
- A sessão não pode estar concluída ou cancelada

**Fluxo Principal:**

- O psicólogo vai no menu e acessar a aba de sessões
- Nessa tela é possível visualizar as sessões
- Dentro de cada registro de sessões pode ter um botão de edição que ira abrir a edição da sessão, logo abaixo irá ter um botão para concluir a sessão
- Ele irá pedir o feedback da sessão
- se os campos forem preenchidos corretamente o reagendamento foi concluído com sucesso

**Fluxo Alternativo:**

- Caso haja algum erro na hora de concluir a sessão retornará uma mensagem de erro para o usuário e cancela o processo

**Pós-condições:**

- Sessão concluída com sucesso, seu status foi alterado

## 9. Edição de sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- Precisava haver uma sessão agendada
- A sessão não pode estar concluída ou cancelada

**Fluxo Principal:**

- O psicólogo vai no menu e acessar a aba de sessões
- Nessa tela é possível visualizar as sessões
- Nessa tela é possível visualizar o botão de edição
- Acessando a tela vai ter alguns campos que podem ser alterados
- Ao terminar a atualização nessas informações pode salvar e está editado

**Fluxo Alternativo:**

- Caso haja algum erro na hora de editar a sessão retornará uma mensagem de erro para o usuário e cancela o processo

**Pós-condições:**

- Sessão editada com sucesso

## 10. Recorrência da sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- Precisa que o paciente esteja cadastrado

**Fluxo Principal:**

- O psicólogo vai no menu e acessar a aba de pacientes
- Nessa tela é possível visualizar as pacientes
- Nessa tela é possível visualizar o botão criar uma recorrência
- ele irá pedir o tipo da recorrência(semanal,mensal)
- o valor da sessão

**Fluxo Alternativo:**

- Caso haja algum erro na hora de gravar a recorrência retornará uma mensagem de erro para o usuário e cancela o processo

**Pós-condições:**

- Sessão editada com sucesso

## 11. Lançamento do pagamento

**Pré-condições:**

- O psicólogo precisa estar logado
- Deve existir uma sessão agendada e concluída

**Fluxo Principal:**

- O psicólogo acessa a aba de sessões
- Seleciona a sessão desejada e clica em "Lançar pagamento"
- Um formulário será exibido com os seguintes campos obrigatórios:
- Valor da sessão (puxado automaticamente de uma configuração ou recorrência)
- Método de pagamento (Cartão de crédito, Débito, Pix)
- Data do pagamento (não pode ser menor que a data atual)
- Informações extras (campo opcional)
- Após o preenchimento correto, o pagamento será salvo com status "Pago"

**Fluxo Alternativo:**

- Se a data do pagamento for inválida, o sistema exibirá uma mensagem de erro
- Se houver falha no lançamento, o processo será cancelado e o usuário será notificado

**Pós-condições:**

- Pagamento registrado com sucesso e vinculado à sessão

## 12. Visualização de pagamentos

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo acessa a aba de pagamentos
- É possível visualizar uma lista de pagamentos e aplicar filtros:
- Nome do paciente
- Intervalo de datas (data de início e final – um dos dois pode ser opcional)
- Status do pagamento (Pendente, Pago, Cancelado)

**Fluxo Alternativo:**

- Caso nenhum pagamento seja encontrado, a tela exibirá uma mensagem informativa

**Pós-condições:**

- Exibição dos registros conforme os filtros aplicados

## 13. Edição de pagamentos

**Pré-condições:**

- O psicólogo precisa estar logado
- O pagamento não pode ter status "Pago"

**Fluxo Principal:**

- O psicólogo acessa a aba de pagamentos
- Seleciona um pagamento com status diferente de "Pago"
- Pode editar os seguintes campos:
  - Valor
  - Status do pagamento
- Após as alterações, clica em "Salvar"

**Fluxo Alternativo:**

- Caso o pagamento esteja com status "Pago", o sistema impede a edição
- Se houver erro na edição, o sistema cancela a operação e exibe mensagem de erro

**Pós-condições:**

- Pagamento atualizado com sucesso

## 14. Criação de recibos de sessões

**Pré-condições:**

- O psicólogo precisa estar logado
- O pagamento da sessão deve estar com o status "Pago"

**Fluxo Principal:**

- O psicólogo acessa a aba de sessões ou pagamentos
- Seleciona a sessão desejada com pagamento efetuado
- Clica em "Gerar recibo"
- O sistema solicita a confirmação e gera o recibo vinculado à sessão

**Fluxo Alternativo:**

- Se o pagamento da sessão não estiver "Pago", o sistema impede a geração do recibo

**Pós-condições:**

- Recibo gerado com sucesso e disponível para download ou impressão

## 15. Geração de Relatórios de Sessões

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo acessa a aba de relatórios
- Seleciona o tipo de relatório "Relatório de Sessões"
- Aplica os filtros desejados:
  - Intervalo de datas (data de início e uma data final)
  - Status da sessão (Agendada, Concluída, Cancelada)
  - Status do pagamento (Pendente, Pago, Cancelado)
- O sistema gera o relatório com as seguintes informações:
  - Dados da sessão (data, horário, paciente, status, observações)
  - Dados do pagamento (valor, status, método)

**Fluxo Alternativo:**

- Caso nenhum dado seja encontrado, o sistema exibe uma mensagem informativa

**Pós-condições:**

- Relatório gerado e disponível para visualização, impressão ou exportação

## 16. Geração de Relatórios de Feedback de Pacientes

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo acessa a aba de relatórios
- Seleciona o tipo de relatório "Relatório de Feedbacks"
- Preenche os seguintes filtros:
  - Paciente (obrigatório)
  - Intervalo de datas (data de início e/ou data final)
- O sistema gera um relatório com:
  - Dados do paciente
  - Feedbacks das sessões agrupados por data e horário da sessão

**Fluxo Alternativo:**

- Caso não haja feedbacks no período, o sistema exibe uma mensagem informativa

**Pós-condições:**

- Relatório gerado e disponível para visualização, impressão ou exportação

## 17. Geração de laudos

**Pré-condições:**

- O psicólogo precisa estar logado
- O paciente deve estar cadastrado

**Fluxo Principal:**

- O psicólogo acessa a aba de laudos
- Clica em "Criar novo laudo"
- Preenche os seguintes campos obrigatórios:
  - Paciente
  - Motivo do laudo
  - Modelo de laudo (selecionável entre opções do sistema)
- O sistema permite incluir dados das sessões e anotações
- Ao finalizar, o laudo é salvo e pode ser exportado

**Fluxo Alternativo:**

- Caso algum campo obrigatório não seja preenchido, o sistema emite alerta e impede a criação

**Pós-condições:**

- Laudo gerado e vinculado ao paciente

## 18. Gerenciamento da agenda

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo acessa a aba de agenda
- Visualiza sessões, bloqueios e horários disponíveis
- Pode alternar entre modos de visualização (diária, semanal, mensal)
- Pode clicar em um horário para agendar sessão ou criar bloqueio

**Fluxo Alternativo:**

- Caso não haja atividades na agenda, exibe mensagem de "sem registros"

**Pós-condições:**

- Agenda atualizada com dados dinâmicos das sessões e bloqueios

## 19. Lançamento de bloqueio da agenda

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo acessa a aba de agenda
- Clica em "Novo bloqueio"
- Preenche os seguintes campos obrigatórios:
  - Tipo do intervalo (dias inteiros ou horários específicos)
  - Valores do intervalo (data e/ou hora)
  - Motivo do bloqueio (opcional)
- Clica em "Confirmar bloqueio"

**Fluxo Alternativo:**

- Caso os dados estejam inválidos ou o intervalo conflite com sessões já agendadas, o sistema impedirá o bloqueio

**Pós-condições:**

- Horário/dia bloqueado com sucesso, impedindo agendamentos nesse período

## 20. Criação de prontuários

**Pré-condições:**

- O psicólogo precisa estar logado
- O paciente deve estar cadastrado

**Fluxo Principal:**

- O psicólogo acessa a aba de prontuários
- Clica em "Criar novo prontuário"
- Seleciona o paciente
- Insere as informações desejadas no prontuário (campo de texto e anexos, como PDFs ou imagens)
- Salva o prontuário

**Fluxo Alternativo:**

- Caso algum campo obrigatório não seja preenchido (como o paciente), o sistema exibe uma mensagem de erro

**Pós-condições:**

- Prontuário salvo e vinculado ao paciente

## 21. Gerenciamento de prontuários

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo acessa a aba de prontuários
- Pode buscar prontuários por nome do paciente, data de criação ou palavras-chave
- Pode visualizar, editar ou excluir prontuários, se necessário

**Fluxo Alternativo:**

- Caso nenhum prontuário seja encontrado, exibe mensagem "Nenhum prontuário encontrado"

**Pós-condições:**

- Prontuários acessados de forma segura e organizada

## 22. Sugestões com inteligência artificial

**Pré-condições:**

- O psicólogo precisa estar logado
- O psicólogo precisa aceitar o uso da funcionalidade e escolher os dados a serem utilizados

**Fluxo Principal:**

- O psicólogo ativa a funcionalidade de sugestões com IA
- Seleciona os dados permitidos para uso da IA (ex.: feedbacks, sessões, laudos, prontuários)
- O sistema passa a exibir sugestões contextuais nas áreas de:
  - Registro de sessões
  - Geração de laudos
  - Criação de relatórios
  - Análises de padrões de comportamento

**Fluxo Alternativo:**

- Caso o psicólogo não selecione nenhum dado ou revogue o uso, a IA é desativada para ele

**Pós-condições:**

- Sugestões exibidas com base nos dados permitidos

## 23. Login psicólogo

**Pré-condições:**

- O psicólogo deve ter um cadastro ativo no sistema

**Fluxo Principal:**

- O psicólogo acessa a tela de login
- Informar usuário e senha
- O sistema valida as credenciais e permite o acesso

**Fluxo Alternativo:**

- Caso os dados estejam incorretos, o sistema exibe mensagem de erro
- Após várias tentativas incorretas, pode aplicar bloqueio temporário

**Pós-condições:**

- Psicólogo autenticado no sistema

## 24. Logout do usuário

**Pré-condições:**

- O psicólogo precisa estar logado

**Fluxo Principal:**

- O psicólogo clica na opção "Sair" no menu
- O sistema encerra a sessão e redireciona para a tela de login

**Pós-condições:**

- Sessão encerrada com segurança
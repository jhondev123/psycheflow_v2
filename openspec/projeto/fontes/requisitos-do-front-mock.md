# Psycheflow — Requisitos do Sistema (front com dados simulados)

> **Fonte histórica.** Conversão fiel de `docs/Requisitos-Psycheflow.docx` (trabalho de Projeto Web), feita em 12/10/2026.
> Descreve o front **quando ainda usava dados simulados no `localStorage`**. Desde 28/09/2026 o front consome a API e o mock
> foi removido (decisão D-09). Os IDs deste documento (RF01–RF29, RNF01–RNF12) não são os oficiais: a correspondência
> está em [`rastreabilidade.md`](../rastreabilidade.md) e a especificação vigente em [`openspec/specs/`](../../specs/).

Sistema de gestão para psicólogos · Trabalho de Projeto Web

## Sobre o sistema

O Psycheflow ajuda o psicólogo a organizar o consultório em um lugar só: pacientes, agenda e atendimentos. A ideia é que o dia de trabalho caiba em poucas telas, sem complicação.

Este documento lista o que já está pronto. O foco é o frontend (a parte que o usuário vê e usa), que foi onde o trabalho foi feito.

## Como os dados funcionam

Os dados não vêm de um servidor. Tudo fica guardado no próprio navegador, no localStorage (chave `psycheflow.db.v1`), já com uma base de exemplo carregada. Isso foi de propósito: o trabalho pedia para simular os dados no front, sem conectar na API. Na prática, o sistema abre e funciona mesmo sem internet.

## Quem usa

O sistema entende quatro perfis. O cadastro inicial cria a empresa e o usuário responsável.

- Admin — dono da conta, enxerga tudo da empresa.
- Gerente — cuida da rotina e dos cadastros.
- Psicólogo — atende, usa a agenda e registra as sessões.
- Paciente — pessoa atendida pela clínica.

## Requisitos funcionais

*São as coisas que o sistema faz. Todos os itens abaixo já estão implementados.*

### Conta e acesso

| ID | Requisito | Situação |
|----|-----------|----------|
| RF01 | Criar uma conta (empresa mais o usuário responsável) | Pronto |
| RF02 | Entrar com e-mail e senha | Pronto |
| RF03 | Sair da conta | Pronto |
| RF04 | Continuar logado entre as visitas | Pronto |
| RF05 | Conferir a força da senha no cadastro | Pronto |

### Pacientes

| ID | Requisito | Situação |
|----|-----------|----------|
| RF06 | Cadastrar um paciente | Pronto |
| RF07 | Listar e buscar pacientes | Pronto |
| RF08 | Editar os dados de um paciente | Pronto |
| RF09 | Excluir um paciente | Pronto |
| RF10 | Validar CPF e telefone no cadastro | Pronto |

### Agenda

É o coração do sistema. A agenda tem três formas de ver o tempo e um fluxo completo de marcação.

| ID | Requisito | Situação |
|----|-----------|----------|
| RF11 | Ver a agenda por mês | Pronto |
| RF12 | Ver a agenda por semana | Pronto |
| RF13 | Ver a agenda por dia | Pronto |
| RF14 | Navegar entre datas (hoje, anterior, próximo) | Pronto |
| RF15 | Marcar um atendimento na agenda | Pronto |
| RF16 | Bloquear um horário (almoço, indisponível) | Pronto |
| RF17 | Confirmar, cancelar e reativar um agendamento | Pronto |
| RF18 | Abrir os detalhes e excluir um agendamento | Pronto |

### Horários de trabalho

| ID | Requisito | Situação |
|----|-----------|----------|
| RF19 | Definir as faixas de horário por dia da semana | Pronto |
| RF20 | Usar esses horários como base da agenda | Pronto |

### Atendimentos

| ID | Requisito | Situação |
|----|-----------|----------|
| RF21 | Registrar um atendimento a partir de um horário | Pronto |
| RF22 | Anotar a descrição e o feedback do atendimento | Pronto |
| RF23 | Acompanhar o status (agendada, em andamento, concluída, falta) | Pronto |
| RF24 | Marcar a falta do paciente | Pronto |

### Documentos e perfil

| ID | Requisito | Situação |
|----|-----------|----------|
| RF25 | Criar modelos de documento com campos | Pronto |
| RF26 | Marcar quais campos são obrigatórios e a ordem | Pronto |
| RF27 | Editar o perfil do profissional (abordagem, CRP, contato) | Pronto |
| RF28 | Trocar entre tema claro e escuro | Pronto |
| RF29 | Ver um resumo geral na tela inicial | Pronto |

## Requisitos não funcionais

*São as características de como o sistema se comporta, não uma tela específica.*

| ID | Requisito |
|----|-----------|
| RNF01 | Interface toda em português (pt-BR). |
| RNF02 | Dados simulados no próprio front, sem chamar a API (pedido do trabalho). |
| RNF03 | Persistência no localStorage; funciona offline e mantém os dados ao recarregar. |
| RNF04 | Layout responsivo, do desktop a telas menores. |
| RNF05 | Tema claro e escuro, com a escolha guardada. |
| RNF06 | Calendário próprio, feito à mão, sem biblioteca de calendário. |
| RNF07 | React com TypeScript em modo strict. |
| RNF08 | Build com Vite. |
| RNF09 | Visual com CSS próprio, sem framework de UI. |
| RNF10 | Separação por empresa: cada conta vê só os seus dados. |
| RNF11 | Validações no padrão brasileiro (CPF, telefone, CRP). |
| RNF12 | Textos curtos e diretos para o usuário. |

## Regras e validações

- Senha: pelo menos 6 caracteres, com maiúscula, minúscula, número e símbolo.
- CPF: 11 dígitos com os dígitos verificadores certos.
- Telefone: formato brasileiro.
- CRP: registro do psicólogo, usado no perfil.
- Bloqueio na agenda é só reserva de horário: não vira atendimento.
- Um atendimento sempre nasce de um horário marcado na agenda.

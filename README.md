# Psycheflow

ERP para clínicas de psicologia e psicólogos autônomos: pacientes, agenda, sessões, financeiro, documentos (recibos,
relatórios, laudos), prontuários e sugestões com IA.

Trabalho de Conclusão de Curso — **Centro Universitário FAG** (Cascavel/PR).
Autores: Jhonattan Curtarelli, Matheus Augusto e Matheus Mantovani.

## Repositório

| Pasta | Conteúdo |
|-------|----------|
| [`Psycheflow.Api/`](Psycheflow.Api/) | API em **.NET 10** (Minimal APIs, Vertical Slice, EF Core 10 + PostgreSQL 17), com testes unitários e de integração. Detalhes no [README da API](Psycheflow.Api/README.md). |
| [`Psycheflow.Front/`](Psycheflow.Front/) | Front-end em **React 18 + TypeScript + Vite**, integrado à API (também sobe pelo Docker Compose da raiz). |
| [`docker-compose.yml`](docker-compose.yml) · [`dev.sh`](dev.sh) | Ambiente de desenvolvimento: banco + API + front no Docker com um comando (ver [Como rodar](#como-rodar)). |
| [`openspec/`](openspec/) | Especificação do sistema no padrão **SDD (Spec-Driven Development)** com [OpenSpec](https://github.com/Fission-AI/OpenSpec): specs por capacidade com requisitos e cenários ([`openspec/specs/`](openspec/specs/)), mudanças propostas ([`openspec/changes/`](openspec/changes/)) e documentação de apoio — arquitetura, modelo de dados, API, decisões, rastreabilidade, backlog e os documentos originais do TCC convertidos para Markdown ([`openspec/projeto/`](openspec/projeto/)). **Comece por [`openspec/README.md`](openspec/README.md).** |

> `psycheflow_v2` reúne API, front e documentação num único repositório. O histórico de commits da API foi preservado
> (vindo de [`jhondev123/Psycheflow.Api`](https://github.com/jhondev123/Psycheflow.Api)).

## Arquitetura da API

Organizada em **fatias verticais** (*Vertical Slice Architecture*): cada caso de uso tem sua própria pasta com request,
validação, regra e endpoint (`Features/<Módulo>/<CasoDeUso>/`). Dentro de cada fatia, **comandos** (que alteram dados)
e **consultas** (que só leem) são separados, seguindo o princípio CQS. As regras de negócio ficam em entidades ricas,
inspiradas na Clean Architecture, e os endpoints usam **Minimal APIs**.

Não é CQRS completo: leitura e escrita usam o mesmo banco e o mesmo modelo, o que basta para o porte do sistema.
As decisões e seus motivos estão em [`openspec/projeto/decisoes-de-arquitetura.md`](openspec/projeto/decisoes-de-arquitetura.md).

## Como evoluímos o sistema (SDD)

Toda mudança de comportamento começa pela especificação: `/opsx:propose` cria a proposta com o delta das specs, design e
tarefas; `/opsx:apply` implementa com TDD; `/opsx:archive` incorpora o delta às specs vigentes. Detalhes em
[`openspec/README.md`](openspec/README.md).

## Situação atual

- **API:** conta e login (JWT), usuários e perfis, configurações da clínica, psicólogos e expediente, pacientes, agenda,
  bloqueios e ciclo de vida das sessões, pagamentos e recorrência, documentos em PDF (QuestPDF), laudos, prontuários com
  anexos e assistente de IA (Claude, OpenAI ou Gemini, com dados pseudonimizados) — com isolamento por empresa e sigilo
  das anotações clínicas.
- **Front:** integrado à API — painel, agenda, pacientes (com ficha), sessões, financeiro, prontuários, documentos,
  configurações (perfil, horários, clínica, usuários) e assistente de IA.
- **Próximos passos:** notificações (adiadas) e deploy.
  Veja o backlog em [`openspec/projeto/status-e-backlog.md`](openspec/projeto/status-e-backlog.md).

## Requisitos

Requisitos e cenários de cada área em [`openspec/specs/`](openspec/specs/); status completo, com casos de uso e regras,
em [`openspec/projeto/rastreabilidade.md`](openspec/projeto/rastreabilidade.md).

### Feitos (API + front)

| Área | Requisitos |
|------|-----------|
| Pacientes | RF001 Cadastro · RF002 Visualização e filtros · RF003 Edição |
| Sessões e agenda | RF004 Cadastro · RF005 Visualização · RF006 Reagendamento · RF007 Cancelamento · RF008 Edição · RF009 Conclusão · RF010 Recorrência · RF017 Gerenciamento da agenda · RF018 Bloqueio de agenda |
| Financeiro | RF011 Lançamento · RF012 Visualização · RF013 Edição de pagamentos |
| Documentos | RF014 Recibos · RF015 Relatório de sessões · RF015-B Relatório de feedback · RF016 Laudos e relatórios psicológicos |
| Prontuários | RF019 Criação · RF020 Gerenciamento (anexos, busca e trilha de acesso) |
| Inteligência artificial | RF021 Sugestões com IA (Claude, OpenAI ou Gemini, com dados pseudonimizados) |
| Acesso | RF022 Login · RF023 Logout |
| Complementares | RC-01 Conta da clínica · RC-02 Usuários da equipe · RC-03 Horários de atendimento · RC-04 Perfil profissional · RC-06 Painel · RC-07 Tema claro/escuro · RC-08 Health check |
| Não funcionais e domínio | RNF003 Usabilidade e responsividade · RD002 Confidencialidade dos dados do paciente |

### Faltando ou parciais

| ID | Requisito | Situação |
|----|-----------|----------|
| RF024 · RF025 · RF026 | Notificações de sessão (paciente e psicólogo) e de conflito de agenda | **Não feito** — desejável, adiado. O conflito de horário já é avisado na tela ao agendar. |
| RNF004 | Disponibilidade de 99,9% e backups automáticos | **Não feito** — depende do deploy, que ainda não existe. |
| RNF001 | Segurança, criptografia e LGPD | **Parcial** — autenticação, isolamento por clínica, sigilo, auditoria e limite de requisições feitos; falta criptografia dos dados em repouso (disco/banco), que é do deploy. |
| RNF002 | Tempo de resposta abaixo de 2 s | **Parcial** — listas paginadas e índices no banco, mas sem medição formal. |
| RD001 | Resolução CFP 013/2015 (prontuário eletrônico) | **Parcial** — prontuário sigiloso com guarda (exclusão lógica); falta conferir o texto das normas do CFP. |
| RC-05 | Modelos de documento editáveis | **Removido** — substituído por documentos fixos em PDF (recibo, declaração, relatórios e laudos). |
| — | Testes automatizados do front | **Não feito** (opcional) — o front foi validado manualmente no navegador; a API tem testes unitários e de integração. |

## Como rodar

Com o Docker Desktop aberto, na raiz do repositório (no Windows, pelo Git Bash):

```bash
./dev.sh          # sobe banco + API + front e espera ficar pronto
```

- Front: http://localhost:5173 · API: http://localhost:8080/scalar · login demo `ana@psycheflow.dev` / `Psycheflow@123`
- Na primeira vez o script cria o `.env` a partir do `.env.example` (portas, chave JWT de dev e chaves de IA opcionais).

| Comando | O que faz |
|---------|-----------|
| `./dev.sh` | Sobe tudo, reconstruindo as imagens (equivale a `docker compose up -d --build --wait`) |
| `./dev.sh watch` | Sobe tudo e reconstrói a API/o front sozinho ao salvar o código |
| `./dev.sh db` | Sobe só o banco, para rodar a API com `dotnet run` e o front com `npm run dev` |
| `./dev.sh logs [db\|api\|web]` | Acompanha os logs |
| `./dev.sh down` | Para os containers (os dados ficam) |
| `./dev.sh reset` | Apaga o banco e os anexos e sobe de novo com os dados de demonstração |

Para desenvolver fora dos containers (recarga instantânea), use `./dev.sh db` e rode a API
(`dotnet run --project src/Psycheflow.Api`, em `Psycheflow.Api/`) e o front (`npm install && npm run dev`, em
`Psycheflow.Front/`, com `VITE_API_URL=http://localhost:5240` no `.env.local`). Pré-requisitos: Docker (e .NET 10 SDK /
Node.js para desenvolver sem containers). Logins de demonstração e demais detalhes no [README da API](Psycheflow.Api/README.md).

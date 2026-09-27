# Psycheflow

ERP para clínicas de psicologia e psicólogos autônomos: pacientes, agenda, sessões e, nas próximas etapas, financeiro,
documentos (recibos, relatórios, laudos), prontuários e sugestões com IA.

Trabalho de Conclusão de Curso — **Centro Universitário FAG** (Cascavel/PR).
Autores: Jhonattan Curtarelli, Matheus Augusto e Matheus Mantovani.

## Repositório

| Pasta | Conteúdo |
|-------|----------|
| [`Psycheflow.Api/`](Psycheflow.Api/) | API em **.NET 10** (Minimal APIs, Vertical Slice, EF Core 10 + PostgreSQL 17), com testes unitários e de integração. Detalhes no [README da API](Psycheflow.Api/README.md). |
| [`Psycheflow.Front/`](Psycheflow.Front/) | Front-end em **React 18 + TypeScript + Vite**. Hoje funciona em modo demonstração (dados no `localStorage`); a integração com a API é a próxima etapa. |
| [`docs/`](docs/) | Documentos originais do TCC (requisitos, casos de uso, modelo de dados, DER). |
| [`docs/ai/`](docs/ai/) | Documentação viva do projeto: requisitos com status, casos de uso, regras de negócio, modelo de dados, contratos da API, backlog e decisões de arquitetura. **Comece por [`docs/ai/README.md`](docs/ai/README.md).** |

> `psycheflow_v2` reúne API, front e documentação num único repositório. O histórico de commits da API foi preservado
> (vindo de [`jhondev123/Psycheflow.Api`](https://github.com/jhondev123/Psycheflow.Api)).

## Situação atual

- **API:** conta e login (JWT), usuários e perfis, configurações da clínica, psicólogos e expediente, pacientes, agenda,
  bloqueios e ciclo de vida completo das sessões — com isolamento por empresa e sigilo das anotações clínicas.
- **Front:** telas de agenda, pacientes, sessões, horários, perfil e painel (mock).
- **Próximos passos:** integrar o front com a API → financeiro e recorrência → documentos (QuestPDF) → prontuários → IA.
  Veja o backlog em [`docs/ai/07-status-e-backlog.md`](docs/ai/07-status-e-backlog.md).

## Como rodar

```bash
# API + banco (Docker)
cd Psycheflow.Api
cp .env.example .env
docker compose up -d --build        # http://localhost:8080/scalar

# Front
cd Psycheflow.Front
npm install
npm run dev                          # http://localhost:5173
```

Pré-requisitos: .NET 10 SDK, Docker e Node.js. Logins de demonstração e demais detalhes no [README da API](Psycheflow.Api/README.md).

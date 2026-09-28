---
name: psycheflow-frontend
description: Cria ou altera telas do front Psycheflow (React 18 + TS + Vite) integradas à API, seguindo o cliente HTTP, os componentes e o visual existentes. Use para qualquer mudança em Psycheflow.Front.
---

# Frontend Psycheflow

Stack: React 18 + TypeScript strict + Vite + react-router-dom 6 + date-fns (pt-BR) + lucide-react. CSS próprio em `src/styles/`
(sem framework de UI). Tema claro/escuro via `data-theme` no `<html>`.

## Antes de mexer
1. Leia a página mais parecida em `src/pages/` e copie a estrutura (PageHeader → filtros → card/lista → modal → toasts).
2. Confira em `docs/ai/02-requisitos.md` os campos/filtros do RF e em `docs/ai/06-api.md` o contrato do endpoint.

## Dados e API
- **Toda chamada passa por `src/lib/api.ts`**: `api.get/post/put/del`, `api.upload` (multipart) e `api.download` (PDF/anexo com o
  nome enviado pela API). Erros viram `ApiError` (`message` em pt-BR, `code`, `fields` por campo em camelCase, ex. `address.zipCode`).
- Carregamento: `useAsync(() => api.get<T>(...), [deps])` → `{ data, loading, error, reload, setData }`. Mostre `<Loading />`,
  `<ErrorState onRetry={reload} />` e `<EmptyState />`.
- Tipos do contrato em `src/types/index.ts` (enums como texto). Horas chegam `HH:mm:ss` → exiba com `hm()`; datas `yyyy-MM-dd` →
  `fmtDateShort`/`fmtRelativeDay`; dinheiro com `fmtMoney`/`parseMoney` (`lib/format.ts`).
- Sessão/perfil/configurações/tema/avisos vêm de `useStore()` (`me`, `psychologist`, `settings`, `isManagement`, `isPsychologist`,
  `notify`). Não guarde dados de negócio em estado global nem no `localStorage`.
- Regras de negócio ficam na API; no front valide só formato (`lib/validation.ts`: CPF, telefone, CRP, CEP, senha) e mostre o erro
  que a API devolver (`errorMessage(e)` e `e.fields`).

## Reaproveite
- `components/SessionModal` (ciclo de vida da sessão), `PatientForm`, `PatientSelect`, `PayDialog`, `AiSuggestionModal`,
  `ui/ReasonDialog` (ações com motivo), `ui/Modal`/`ConfirmDialog`, `ui/Feedback` (`Loading`, `ErrorState`, `Pager`), `ui/Markdown`.
- Status sempre pelos metadados de `lib/domain.ts` (label + badge), nunca strings soltas.
- Rotas novas em `App.tsx` (use `RequireRole` para gestão/psicólogo) e item no `Sidebar.tsx`.

## Visual e acessibilidade
- Só variáveis CSS existentes (`--brand-*`, `--success`, `--warning`, `--danger`, `--info` e `-bg`) e classes existentes
  (`btn`, `card`, `field`, `label`, `input`, `select`, `badge`, `filters`, `notice`, `cal-views`…). Cor nova vira variável nos dois temas.
- `label` ligado ao input, `aria-label` em botão só com ícone, textos curtos em pt-BR, toda ação com `notify`.

## Fechar
- `npm run typecheck` e `npm run build` (sem Node na máquina: `docker run --rm -v "$PWD:/src:ro" node:22-alpine ...` copiando o código
  para dentro do container, ou `docker compose up -d --build web` na pasta da API).
- Teste o fluxo principal no navegador contra a API do compose (login demo `ana@psycheflow.dev` / `Psycheflow@123`).
- Atualize os status em `docs/ai/`.

# Psycheflow — Modelo de Dados (DER)

> **Fonte histórica.** Conversão fiel de `docs/Modelo-de-Dados-Psycheflow.docx` (trabalho de Projeto Web), feita em 12/10/2026.
> Descreve o banco **anterior à reestruturação da API** (tabelas `Documents`, `Config`, `ConfigAi` etc. foram removidas).
> O modelo vigente está em [`modelo-de-dados.md`](../modelo-de-dados.md); o diagrama original é [`der-psycheflow.png`](../der-psycheflow.png).

Sistema de gestão para psicólogos · Trabalho de Projeto Web

## Sobre este documento

Aqui está a estrutura do banco de dados do Psycheflow e como as tabelas se ligam. O modelo segue o banco real da API (PostgreSQL, mapeado pelo Entity Framework Core). O frontend usa uma cópia simplificada desses mesmos dados em memória.

Junto deste documento vem o arquivo `psycheflow-der.dbml`. Ele é a fonte para desenhar o diagrama em uma ferramenta, sem precisar montar tabela por tabela na mão. *(O `.dbml` não foi versionado no repositório.)*

## Como gerar o diagrama

Caminho mais simples, com o dbdiagram.io:

1. Abra o site dbdiagram.io e clique em Create new diagram.
2. Apague o exemplo que aparece no editor da esquerda.
3. Abra o arquivo `psycheflow-der.dbml`, copie tudo e cole no editor.
4. O diagrama aparece pronto, com as tabelas e as linhas de relacionamento.
5. Use o menu Export para salvar em PNG, PDF ou gerar o SQL.

Se preferir, o código Mermaid no fim deste documento desenha uma versão resumida em qualquer editor que aceite Mermaid (Mermaid Live, VS Code, GitHub). *(O código Mermaid não estava no arquivo `.docx`.)*

## Entidades

As tabelas principais do domínio. As que começam com AspNet vêm do ASP.NET Identity, responsável pelo login e pelos perfis.

| Tabela | O que guarda |
|--------|--------------|
| Companies | A clínica/empresa. Tudo no sistema pertence a uma empresa. |
| AspNetUsers | Quem acessa o sistema: nome, e-mail, telefone e senha. |
| AspNetRoles | Os perfis: Admin, Gerente, Psicólogo e Paciente. |
| Psychologists | O psicólogo: abordagem e CRP. Os dados pessoais vêm do usuário ligado. |
| Patients | O paciente. Os dados pessoais vêm do usuário ligado, quando existe. |
| PsychologistsHours | As faixas de horário de trabalho do psicólogo por dia da semana. |
| Schedules | Os itens da agenda: um atendimento marcado ou um bloqueio. |
| Sessions | O atendimento em si: liga o horário da agenda ao paciente. |
| Payments | O pagamento de um atendimento. |
| Documents | Modelos de documento (contrato, anamnese, etc.). |
| DocumentFields | Os campos de cada modelo de documento. |
| Config | Configurações por empresa ou por usuário. |
| ConfigAi | Ajustes de IA ligados a uma configuração. |

## Relacionamentos

Como as tabelas se ligam. A coluna "Ao excluir" mostra o que o banco faz com os filhos quando o pai sai.

| Ligação | Tipo | Ao excluir |
|---------|------|------------|
| Companies → AspNetUsers | 1 para muitos | Cascade |
| Companies → Psychologists | 1 para muitos | Cascade |
| Companies → Patients | 1 para muitos | Cascade |
| Companies → Schedules | 1 para muitos | Restrict |
| Companies → Sessions | 1 para muitos | No action |
| AspNetUsers → Psychologists | 1 para 1 (perfil) | Restrict |
| AspNetUsers → Patients | 1 para 1 (perfil) | Restrict |
| Psychologists → PsychologistsHours | 1 para muitos | Restrict |
| Psychologists → Schedules | 1 para muitos | Restrict |
| Schedules → Sessions | 1 para 1 | No action |
| Psychologists → Sessions | 1 para muitos | No action |
| Patients → Sessions | 1 para muitos | No action |
| Sessions → Payments | 1 para muitos | Cascade |
| Documents → DocumentFields | 1 para muitos | Cascade |
| Config → ConfigAi | 1 para 1 | Cascade |
| AspNetUsers ↔ AspNetRoles | muitos para muitos | Cascade |

## Enums

Alguns campos guardam um número que representa um estado. A tabela mostra o que cada valor significa.

| Enum | Valores |
|------|---------|
| Abordagem | None, Psicanálise, Comportamental |
| Tipo de agenda | Atendimento (0), Bloqueio (1) |
| Status da agenda | Aguardando (0), Confirmado (1), Cancelado (2) |
| Status do atendimento | Agendada (0), Em andamento (1), Concluída (2), Falta (3) |

## Convenções do modelo

- Toda tabela de domínio guarda CreatedAt, UpdatedAt e DeletedAt.
- DeletedAt preenchido significa registro excluído (soft delete): some das listas, mas o histórico fica.
- CompanyId aparece em quase tudo: é o que separa os dados de cada empresa.
- Os enums são guardados como número inteiro no banco.
- Nome, e-mail e telefone do psicólogo e do paciente ficam no usuário (AspNetUsers), não repetidos nas tabelas.
- O CPF é validado na tela, mas não tem coluna própria no banco da API.

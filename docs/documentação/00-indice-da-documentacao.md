# Documentação técnica — TCC Assiduidade

Este conjunto de documentos explica, de forma completa e didática, como o sistema
**TCC Assiduidade** é construído por dentro: a arquitetura das camadas, como a
interface é preenchida por *data binding*, como o banco de dados é modelado e
relacionado, e como a lógica de negócio (regras de cadastro, importação de CSV e
geração de relatórios) funciona do clique do usuário até a gravação no banco.

O objetivo é servir tanto como material de apoio para a monografia (capítulo de
metodologia/desenvolvimento) quanto como referência para qualquer pessoa que
precise dar manutenção no sistema no futuro.

## Como navegar

| Arquivo | Conteúdo |
|---|---|
| [01-arquitetura-do-sistema.md](01-arquitetura-do-sistema.md) | Visão geral das camadas (MVVM), pastas do projeto, como as peças se encaixam e por quê. |
| [02-data-binding-e-mvvm.md](02-data-binding-e-mvvm.md) | O que é *data binding*, como o WPF preenche a tela sozinho e todos os tipos de binding usados no sistema, com trechos reais do código. |
| [03-banco-de-dados-e-relacionamentos.md](03-banco-de-dados-e-relacionamentos.md) | As cinco tabelas do banco MySQL, o diagrama entidade-relacionamento e a lógica por trás de cada relação (inclusive as decisões não óbvias). |
| [04-fluxo-de-dados-e-regras-de-negocio.md](04-fluxo-de-dados-e-regras-de-negocio.md) | O caminho completo de cada operação (cadastrar, editar, excluir, importar CSV, gerar relatório) desde o clique na tela até o banco de dados e de volta. |
| [05-tratamento-de-erros-e-testes.md](05-tratamento-de-erros-e-testes.md) | Como o sistema identifica e comunica cada tipo de erro ao usuário, e os testes automatizados que garantem a integridade e a consistência dos dados. |

## Como ler este material

Se você nunca teve contato com o projeto, a ordem sugerida é a da tabela acima:
primeiro entenda a arquitetura (as "gavetas" onde cada coisa mora), depois como a
tela conversa com o código (*data binding*), depois o banco de dados, e só então
os fluxos completos — que amarram tudo o que veio antes.

Todos os exemplos de código citados nestes documentos são trechos reais do
projeto (não pseudocódigo), com o caminho do arquivo indicado, para que possam
ser conferidos diretamente no Visual Studio.

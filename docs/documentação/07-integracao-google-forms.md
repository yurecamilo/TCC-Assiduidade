# 07 — Integração com Google Forms (presença automática)

Este documento explica, de ponta a ponta, como o sistema **TCC Assiduidade**
recebe a lista de presença preenchida pelos alunos **no Google Forms** e
transforma isso em registro de frequência no banco — sem precisar baixar
arquivo à mão. Cobrimos a decisão de arquitetura, a configuração do lado do
Google, o funcionamento interno do código, as regras de negócio que continuam
valendo, o tratamento de erros e as limitações conhecidas.

## Sumário

1. [O problema e a escolha da arquitetura](#1-o-problema-e-a-escolha-da-arquitetura)
2. [Visão geral do fluxo](#2-visão-geral-do-fluxo)
3. [Configuração no Google (uma vez só)](#3-configuração-no-google-uma-vez-só)
4. [Configuração no app](#4-configuração-no-app)
5. [Uso no dia a dia](#5-uso-no-dia-a-dia)
6. [O que acontece por dentro (classe por classe)](#6-o-que-acontece-por-dentro-classe-por-classe)
7. [Regras de negócio e idempotência](#7-regras-de-negócio-e-idempotência)
8. [Tratamento de erros](#8-tratamento-de-erros)
9. [Segurança e rede](#9-segurança-e-rede)
10. [Testes e validação](#10-testes-e-validação)
11. [Limitações e evoluções futuras](#11-limitações-e-evoluções-futuras)
12. [Referência rápida de código](#12-referência-rápida-de-código)

---

## 1. O problema e a escolha da arquitetura

Até aqui, a chamada de uma aula era sempre *offline-first*: alguém marcava os
presentes em um arquivo CSV no padrão

```
matricula_aluno,email_aluno,nome_aluno,data_presenca
```

e importava esse arquivo pelo popup **Importar presença**. Isso funciona, mas
exige uma etapa manual (gerar o CSV) que na sala de aula acaba sendo a pessoa
digitando ou exportando de algum lugar.

A integração com o **Google Forms** elimina essa etapa: os próprios alunos
respondem um formulário no celular e o app vai **buscar** as respostas já
prontas, no momento que o professor decidir (fim da aula), usando o mesmo
caminho de gravação que já existia.

**Decisão de arquitetura — por que "pull" de CSV publicado e não webhook:**

| Opção avaliada | Por que foi descartada/adotada |
|---|---|
| **Pull do CSV publicado (ADOTADA)** | O app faz um HTTPS de **saída** para a Google e recebe CSV pronto. Não exige credenciais, não exige nada escutando na rede da escola, e a duração da implementação é mínima: um download + o parser que já existia. |
| Webhook recebendo a resposta (in-bound) | Exigiria a rede da instituição aceitar conexões **de fora para dentro** (túnel/ngrok ou IP fixo), o que normalmente é bloqueado e adiciona infraestrutura nova ao projeto. |
| App intermediário "de staging" | Cria gotejamento de presenças durante a aula e confunde o dashboard (o sistema assume que uma aula importada é um **fechamento**, não um fluxo vivo). |

A semântica escolhida é de **fechamento de aula**: o professor sincroniza quando
a chamada terminou, e a importação grava a aula completa de uma vez.

## 2. Visão geral do fluxo

```
ALUNO                              GOOGLE                       APP TCC ASSIDUIDADE
─────                              ──────                       ───────────────────
responde o Forms                           │
                  planilha de respostas    │
                  ("Publicar na web")       │
                                           │  1. clique "Sincronizar Forms"
                                           ├─ GET https://.../pub?output=csv
                                           │  2. download do CSV (sem login)
                                           │  3. parse (mesmo parser dos CSVs locais)
                                           │  4. validações de negócio (turma, e-mail,
                                           │     matrícula, data única)
                                           │  5. cria/atualiza Aula + grava Ausências
                                           │  6. atualiza o cache e o dashboard
```

Pontos-chave:

- **No banco não existe "presença"**: o sistema grava **ausências** por aula. Quem
  não apareceu no CSV do forms é tombado como ausente
  (presença = vinculados − presentes).
- A integração **reutiliza 100%** o caminho de gravação já validado da importação
  de CSV manual (`ImportacaoService.ImportarPresenca`). Nada de escrita nova.
- A URL pública é **anônima**: a Google só exige que a planilha esteja publicada.

## 3. Configuração no Google (uma vez só)

Esta configuração é feita no navegador, uma vez por semestre/turma, e pode ser
refeita se a planilha mudar.

### 3.1 O formulário

O formulário tem **exatamente** as quatro perguntas, com títulos idênticos às
colunas do CSV — essa coincidência de nomes é o que permite importar sem
mapeamento de colunas:

| Pergunta (título no form) | Tipo no Forms | Coluna resultante |
|---|---|---|
| `matricula_aluno` | Resposta curta | `matricula_aluno` |
| `email_aluno` | Resposta curta | `email_aluno` |
| `nome_aluno` | Resposta curta | `nome_aluno` |
| `data_presenca` | Data | `data_presenca` (formato dd/MM/yyyy) |

Além dessas quatro, o forms mantém a coluna automática **`Carimbo de
data/hora`** na primeira posição. O app **ignora** essa coluna (ver seção 6.3).

Configurações recomendadas:

- **Publicar** o formulário (comportamento novo do Google Forms: o link de
  participante só funciona depois de "Publicar", com "Qualquer pessoa com o link").
- **Não** habilitar "Coletar endereços de e-mail automaticamente" — evita uma
  coluna extra de e-mail duplicada na planilha.
- Uma aula = um envio por aluno. Evitamos "limitar a 1 resposta" para não
  exigir login do aluno (a conta institucional nem sempre está logada no celular).

### 3.2 A planilha vinculada

No editor do formulário: aba **Respostas → "Link para as Planilhas" → Criar uma
nova planilha**. Passa a existir um documento do Sheets (ex.:
*Lista de Presença (respostas)*) com todos os envios, um por linha, com o
`Carimbo de data/hora` na primeira posição. Grava automaticamente.

### 3.3 Publicação do CSV (a peça central)

Na planilha: **Arquivo → Compartilhar → Publicar na Web → Formato: Valores
separados por vírgulas (.csv) → Publicar**. A Google entrega uma URL no formato

```
https://docs.google.com/spreadsheets/d/e/<ID-DE-PUBLICACAO>/pub?output=csv
```

- Essa URL devolve o CSV **sem login**, a qualquer momento.
- Ela sobrevive a renomear o arquivo/planilha (o identificador de publicação não muda).
- **Cuidado:** qualquer reproposição do conteúdo da planilha reflui por essa
  URL em cerca de **1 minuto** (lag normal do cache de publicação da Google —
  foi medido durante a montagem desta integração). Isso só afeta a fase
  "resposta do aluno → CSV"; a sincronização do app pode acontecer a qualquer
  momento depois disso.
- Se a publicação for **cancelada/despublicada**, a URL volta a devolver uma
  página HTML — o app detecta isso e avisa o usuário (seção 8).

### 3.4 Artefatos atuais (referência da turma piloto)

| Item | Valor |
|---|---|
| Formulário | "Lista de Presença" |
| Link do formulário (alunos) | `https://docs.google.com/forms/d/e/1FAIpQLSfgFi2_T3D4PL5zqJUZ2BdVTqke36BBFJB5jB6CxeGt8zHUbw/viewform` |
| Planilha de respostas | "Lista de Presença (respostas)" |
| CSV publicado (usado pelo app) | `https://docs.google.com/spreadsheets/d/e/2PACX-1vSrLILJKnvI0T71KoOATZ-oLwVpjfAaPjiXDJQnDxJtnb5FNTnEykUwayZ95S411JECZF4GlXetf5S1/pub?output=csv` |

## 4. Configuração no app

A URL do CSV vive no `App.config` do projeto (e vira `TCC Assiduidade.exe.config`
no binário publicado):

```xml
<appSettings>
    <!-- CSV público da planilha de respostas do formulário "Lista de Presença"
         (Google Sheets > Arquivo > Publicar na web > CSV). Atualize este valor
         se a planilha for republicada ou trocada. -->
    <add key="FormsCsvUrl" value="https://docs.google.com/spreadsheets/d/e/2PACX-.../pub?output=csv" />
</appSettings>
```

Se a planilha for despublicada e republicada (muda o ID de publicação), basta
atualizar esse valor e recompilar/reinstalar. Nenhum outro ponto do código
precisa conhecer a URL: tudo passa por `GoogleFormsService.ObterUrlPadrao()`.

## 5. Uso no dia a dia

No final da aula, com o formulário já fechado aos envios novos:

1. Abrir **Turmas → bem "Importar presença"**.
2. **Passo 1** — selecionar a turma da chamada.
3. **Passo 2** — clique em **"Sincronizar Forms"** (ao lado de "Escolher CSV").
   - Aparece o overlay *"Baixando respostas da planilha (Google Forms)..."*.
   - O campo de origem mostra `Planilha do Google Forms — N resposta(s) baixada(s)`.
4. **Iniciar Importação**.
   - O app aplica todas as regras de negócio (seção 7) e grava a aula + ausências.
   - Ao final, o popup mostra o resumo: número da aula, data, víndicados, presentes, ausentes — e os ausentes ficam listados para conferência.

Observações de operação:

- O aluno que respondeu **mais de uma vez** no forms quebra a contagem
  (matrícula repetida) e a importação é cancelada com aviso — o certo é limpar a
  duplicidade na planilha antes (ou aceitar que o envio duplicado zere, removando a
  linha extra lá no Sheets).
- **Registrar manual segue funcionando**: "Escolher CSV" e "Sincronizar Forms"
  são caminhos equivalentes para o mesmo destino; a origem definida por último
  é a que vale
  (selecionar um CSV anula as respostas baixadas e vice-versa).
- Se a rede cair no meio do download, o app avisa e você pode sincronizar de novo
  sem efeito residual (seção 7).

## 6. O que acontece por dentro (classe por classe)

### 6.1 `Servicos/GoogleFormsService.cs` — download e travas de segurança

Classe estática com duas responsabilidades:

**`ObterUrlPadrao()`** — lê `FormsCsvUrl` do `AppSettings` via
`ConfigurationManager`. Retorna `null` quando não configurada (o chamador
converte em mensagem amigável).

**`BaixarRespostas(string url = null)`** — faz o ciclo completo:

1. Resolve a URL (parâmetro > config). Nenhuma → `InvalidOperationException`
   com instrução clara de configurar o `App.config`.
2. Valida a URL: precisa ser `https` absoluto, conter `/spreadsheets/d/e/` **e**
   `output=csv` — é uma trava de sanidade contra alguém colar no config um link
   de edição do Sheets (que não serve para download anônimo).
3. `HttpWebRequest` com `TLS 1.2` forçado no `ServicePointManager` (necessário
   no .NET Framework 4.7.2 — conexões modernas de TLS com a Google),
   timeout de 30 s e `UserAgent` próprio.
4. Baixa tudo para memória e decodifica usando **a mesma regra de encoding do
   leitor de arquivos** (BOM → UTF-8/UTF-16; sem BOM → UTF-8 estrito e, em
   último caso, ANSI Windows-1252). Isso é o que evita que "José" chegue como
   "José" corrompido do CSV público.
5. **Trava de conteúdo**: se a resposta começa com `<` (Google devolver página
   HTML), levanta erro explicando que a planilha saiu publicada — em vez de o
   parser cuspir um registro sem sentido.
6. Entrega o texto ao parser (6.2).

Erros de rede (`WebException`) são re-empacotados em
`InvalidOperationException` com mensagem orientada ao usuário.

### 6.2 `Servicos/ArquivoService.cs` — parser em comum

A lógica que já lia CSVs locais foi extraída para
**`AnalisarConteudoCsv(string conteudo, char separador = ',')`** e é usada pelos
dois caminhos (`LerCsv` de arquivo e o download do forms). Ganhamos de brinde:

- um só lugar para o parser (aspas, vírgula/ponto-e-vírgula, linhas vazias);
- cabeçalhos normalizados para minúsculas — o CSV do Google (casada com o
  "Carimbo de data/hora" capitalizado) entra no mesmo padrão dos CSVs locais;
- colunas resultantes: dicionários por linha, exatamente o formato que o
  `ImportacaoService` já consome.

### 6.3 `ViewModel/Popups/ImportarPresencaViewModel.cs` — a ponte

- Comando **`SincronizarFormsCommand`**: valida turma selecionada →
  `Task.Run(GoogleFormsService.BaixarRespostas)` (overlay de progresso da
  tela, mesma UX da importação por arquivo) → se a planilha está vazia, avisa
  e não começa importação → caso haja respostas, guarda em
  `_dadosFormsPendentes` e escreve "Planilha do Google Forms — N resposta(s)"
  no campo de origem.
- **`ExecutarImportacao`** passa a consumir, nesta ordem:
  `_dadosFormsPendentes` (forms) → `ArquivoService.LerCsv(...)` (arquivo).
  Zera `_dadosFormsPendentes` quando o usuário seleciona um CSV à mão.
- A coluna `Carimbo de data/hora` entra no dicionário e é **ignorada
  naturalmente**: `ImportarPresenca` busca as chaves por nome
  (`Keys.FirstOrDefault(OrdinalIgnoreCase)`) e nenhum campo de gravação usa esse
  carimbo (o campo operativo é `data_presenca`, preenchido no forms).
- Toda gravação continua centralizada em `ImportacaoService.ImportarPresenca`:
  mesmas validações, mesmo `_alunoService.ImportarAulaEAusenciasAtomico`.

### 6.4 `Servicos/DataCacheService.cs` — cache à prova de blip

A integração não muda o cache, mas ele é parte do fluxo: depois de importar,
`ForçarAtualizacaoAsync()` atualiza alunos/turmas/aulas que alimentam telas e
dashboards. Constatou-se em operação que **uma falha única na carga inicial**
(blip de rede) ficava travada para sempre (`ErroCarregamento` persistente),
fazendo todas as telas seguintes falharem com o banco já saudável. O
`AguardarCarregamentoAsync()` agora **tenta recarregar uma vez** ao detectar
erro pendente e só repassa a falha se essa nova tentativa também falhar.

## 7. Regras de negócio e idempotência

Nada disso é novo, mas vale explicitar como se comporta **no caminho do Forms**:

- **Uma importação = uma aula (uma data).** Todas as linhas precisam ter a mesma
  `data_presenca` (`ObterDataPresenca` cancela se houver mistura). No forms, a
  data vem do campo de resposta (não do carimbo) — o professor fecha o forms no
  dia da aula.
- **Deduplicação de aula por dia**: `ImportarAulaEAusenciasAtomico` localiza a
  aula existente para a mesma `TurmaId + Data` dentro da transação, em vez de
  criar uma segunda. Consequência operacional: **sincronizar a mesma turma
  duas vezes no mesmo dia não duplica nada** — pode fazer isso à vontade
  (inclusive para "atualizar" a chamada depois que mais alunos responderam).
- **Matrícula repetida no CSV cancela tudo** (nenhuma linha gravada parcialmente).
- **Aluno de outra turma cancela tudo** — a checagem
  (`ContarAlunosDeOutraTurma`) continua valendo; se um professor compartilhar o
  link do forms da turma errada, a importação recusa e explica.
- **Aluno não vinculado / sem data de entrada na turma** naquela data também
  cancela, com mensagem específica (a data de entrada é o que delimita o
  "quando cada aluno começou a contar frequência").
- **Ausência é derivada**: ausentes = alunos vinculados à turma com
  `DataEntrada <= data_presenca` − presentes. O forms lista quem **estava**; o
  tombamento é de quem **não estava**.

## 8. Tratamento de erros

| Situação | Como se manifesta no app |
|---|---|
| `FormsCsvUrl` não configurado | Popup de erro no ato do sync: "URL da planilha de respostas não configurada..." |
| URL colada de outro tipo (sem `/spreadsheets/d/e/` ou `output=csv`) | Rejeitada antes do download, com explicação do formato esperado |
| Rede fora / DNS / timeout 30 s | "Não foi possível baixar a planilha de respostas (falha de rede ou link inválido)..." |
| Planilha despublicada (Google devolve HTML) | "O endereço devolveu uma página web em vez do CSV. Confirme que a planilha continua publicada..." |
| Planilha publicada e ainda sem respostas | Popup informativo "A planilha publicada ainda não tem respostas para importar." — não abre a importação |
| CSV malformado (coluna duplicada, linha quebrada etc.) | Mensagem do parser (`InvalidDataException`), igual aos CSVs manuais |
| Turma sem alunos, e-mail inválido, matrícula repetida, aluno de outra turma, aluno não vinculado | Mensagens de negócio do `ImportacaoService` (as mesmas da importação por arquivo) |
| Falha de gravação no banco | Transação faz rollback, tradutor de erro MySQL traduz para português, nada parcial é salvo |
| Cache falho de forma crônica | Com o auto-retry do `DataCacheService`, blips se resolvem sozinhos; falha real sobe com o erro real |

Princípio preservado: **nenhum path de erro avança parcialmente** — ou a aula
inteira cai no banco (transação), ou o popup explica o motivo e nada muda.

## 9. Segurança e rede

- **Somente HTTPS de saída.** Não há nada escutando na máquina; a nenhum momento
  a rede da escola precisa aceitar conexão externa.
- **Sem credenciais do Google no app.** A URL de publicação é anônima (foi
  projetada assim pela Google para sites públicos). O app nunca pede login nem
  guarda token OAuth.
- **O que a URL pública revela**: quem tem o link do CSV vê os dados das
  respostas (incluindo matrículas/e-mails/nomes dos que responderam). Recomendações:
  - publicar apenas a **aba de respostas**; não compartilhar essa URL em carta
    pública para os alunos;
  - rotacionar a URL (despublicar → publicar de novo → trocar no `App.config`)
    semestralmente ou quando suspeitar de vazamento.
- **Dados no dispositivo**: o app processa o CSV em memória e grava no banco
  MySQL da instituição (Aiven, TLS obrigatório). Nada fica em disco.

## 10. Testes e validação

Validação feita durante a construção da integração (branch `teste/sandbox`):

1. **Ponta a ponta com formulário real**: 3 respostas de alunos de teste
   submetidas via viewform público → planilha atualizada → CSV público servindo
   as 3 linhas (com lag ~1 min) ✓
2. **Harness isolado do serviço** (mesmo driver e config do app, executado fora
   da UI): download + parse + extração das 4 colunas + parse de data
   `dd/MM/yyyy` para as 3 linhas ✓
3. **Build**: solução compila com o novo serviço no `.csproj` ✓
4. **Resistência a falha**: cache inicial que falha uma vez se recupera sem
   repetir erro para o usuário (auto-retry do `DataCacheService`) ✓

Sugestão de roteiro para demonstração (apresentação):

- responder o forms com 1 aluno novo na hora, esperar ~1 min, sincronizar;
- sincronizar **duas vezes seguidas** o mesmo dia para mostrar que não duplica aula;
- tentar sincronizar com o forms aberto a uma turma errada para ver a checagem de matrícula recusar.

## 11. Limitações e evoluções futuras

| Limitação atual | Impacto | Possível evolução |
|---|---|---|
| Um CSV/forms por vez cobre **uma turma e uma data** | Se cada turma tiver o próprio forms, o professor repete o passo por turma (trocar URL) | Form por turma com várias URLs no config + seletor de origem no popup |
| Mapeamento de colunas depende de os títulos do forms serem iguais aos do CSV | Adicionar/renomear pergunta no forms quebra a validação (erro claro no import) | Camada de mapeamento "título do forms → coluna" persistida no `App.config` |
| Envios duplicados do aluno precisam ser limpos na planilha | Bloqueia a importação até ajuste manual na Google | Deduplicação por `matricula_aluno + data` com política "primeiro envio vence" |
| Fechamento do forms é manual | Professor precisa desativar o forms no fim da aula | Apps Script (`form.isAcceptingResponses()` na hora marcada) ou "deadline" no próprio app |
| URL única no `App.config` troca exigiria recompilar/reinstalar para desplegar em máquina de laboratório | Baixo para o TCC (máquina do professor) | Leitura da URL de arquivo externo/parametrizada por instalação |
| DU solicitar publicação de dados de resposta publicamente | Privacidade: qualquer um com a URL lê respostas | Rotacionar URL por semestre; futuramente, leitura autenticada via API do Google Sheets |

## 12. Referência rápida de código

| Arquivo | Papel na integração |
|---|---|
| `Servicos/GoogleFormsService.cs` | Download anônimo do CSV publicado, travas de URL/conteúdo, encoding, expõe `ObterUrlPadrao()`/`BaixarRespostas()` |
| `Servicos/ArquivoService.cs` | `AnalisarConteudoCsv` — parser compartilhado entre arquivos locais e o download |
| `ViewModel/Popups/ImportarPresencaViewModel.cs` | `SincronizarFormsCommand`, guarda `_dadosFormsPendentes`, alimenta `ExecutarImportacao` |
| `View/Popups/ImportarPresenca.xaml` | Botão "Sincronizar Forms" no Passo 2 ("Origem dos dados de presença") |
| `App.config` | `appSettings/FormsCsvUrl` — única configuração da integração |
| `Servicos/ImportacaoService.cs` | `ImportarPresenca` — processa e grava a aula (não mudou) |
| `Repositories/AlunoRepository.cs` | `ImportarAulaEAusenciasAtomico` — transação com deduplicação de aula por dia (não mudou) |
| `Servicos/DataCacheService.cs` | Atualização de telas/dashboards pós-importação + auto-retry da carga inicial |

Histórico relevante na branch `teste/sandbox`:

- `b041911` — feat: sincronizar presenças da planilha pública do Google Forms
- `c3c12a2` — fix: falha pontual do cache inicial deixa de envenenar a sessão

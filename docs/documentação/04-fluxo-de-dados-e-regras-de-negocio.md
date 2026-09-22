# Fluxo de dados e regras de negócio

Este documento acompanha, passo a passo, o caminho completo de cada operação
importante do sistema — do clique do usuário até o banco de dados, e de volta.
Pressupõe a leitura prévia de
[01-arquitetura-do-sistema.md](01-arquitetura-do-sistema.md) (as camadas) e
[03-banco-de-dados-e-relacionamentos.md](03-banco-de-dados-e-relacionamentos.md)
(as tabelas envolvidas).

## 1. Inicialização do sistema

```mermaid
sequenceDiagram
    participant App as App.xaml.cs
    participant Cache as DataCacheService
    participant BD as MySQL

    App->>Cache: InicializarCargaBackground()
    Note over Cache: Task.Run — não bloqueia a abertura da janela
    par 4 buscas em paralelo
        Cache->>BD: ObterPerfilAluno()
        Cache->>BD: ObterTurmasComContagem()
        Cache->>BD: ObterResumoAulas()
        Cache->>BD: ObterTodasTurmas()
    end
    BD-->>Cache: resultados
    Cache->>Cache: IsCarregado = true
    Cache-->>Cache: dispara evento CacheAtualizado
```

A janela principal (`BaseWindow`) já abre e mostra a tela de Início
imediatamente; as telas individuais aguardam o cache ficar pronto
(`while (!DataCacheService.IsCarregado) await Task.Delay(100);`) antes de
exibir dados, evitando que o aplicativo pareça "travado" enquanto busca no
banco.

## 2. Cadastrar um registro simples (exemplo: novo aluno)

```mermaid
sequenceDiagram
    participant Usuario as Usuário
    participant View as NovoAluno.xaml
    participant VM as NovoAlunoViewModel
    participant Srv as AlunoService
    participant Repo as AlunoRepository
    participant BD as MySQL
    participant Cache as DataCacheService

    Usuario->>View: preenche campos, clica "Salvar"
    View->>VM: SalvarAlunoCommand.Execute()
    VM->>VM: monta objeto Aluno com os dados do binding
    VM->>Srv: Adicionar(aluno)
    Srv->>Srv: ValidarAluno(aluno) — matrícula, nome, e-mail, turma
    alt dados inválidos
        Srv-->>VM: lança ArgumentException
        VM-->>Usuario: MessageBox "Dados inválidos" + motivo
    else dados válidos
        Srv->>Repo: Adicionar(aluno)
        Repo->>BD: BEGIN TRANSACTION
        Repo->>BD: INSERT INTO Aluno (...)
        Repo->>BD: INSERT INTO VinculoTurmaAluno (...)
        Repo->>BD: COMMIT
        BD-->>Repo: ok
        Repo-->>Srv: ok
        Srv-->>VM: ok
        VM->>Cache: ForçarAtualizacaoAsync()
        Cache->>BD: recarrega tudo
        Cache-->>VM: evento CacheAtualizado
        VM-->>Usuario: MessageBox "Aluno cadastrado com sucesso!"
        VM->>View: fecha a janela (FecharJanelaCommand)
    end
```

**Onde cada regra mora:**

- **Validação de formato/obrigatoriedade** → `AlunoService.ValidarAluno`
  (matrícula, nome, e-mail em formato válido, turma selecionada).
- **Persistência + consistência entre duas tabelas** (`Aluno` e
  `VinculoTurmaAluno`) → `AlunoRepository.Adicionar`, dentro de uma
  transação.
- **Atualização da tela e de todas as outras telas abertas** → o par
  `ForçarAtualizacaoAsync()` / evento `CacheAtualizado`.

O mesmo padrão (validar no Serviço → persistir no Repositório → atualizar o
cache → fechar o popup) se repete em **todos** os cadastros e edições do
sistema: `NovaTurmaViewModel`, `EditarAlunoViewModel`, `EditarTurmaViewModel`.

## 3. Excluir um registro — e por que precisa de mais cuidado

Excluir não é só "apagar uma linha": é preciso decidir o que fazer com tudo o
que depende daquele registro. O fluxo de exclusão de turma ilustra bem:

```mermaid
flowchart TD
    A["Usuário clica no ícone de lixeira<br/>na linha da turma"] --> B{"Confirma no MessageBox<br/>'Tem certeza?'"}
    B -- Não --> Z[Nada acontece]
    B -- Sim --> C["TurmasViewModel.ExecutarExcluir<br/>dentro de try/catch"]
    C --> D["TurmaService.Excluir(id)"]
    D --> E["TurmaRepository.Excluir(id)<br/>— uma única transação"]
    E --> F["DELETE Ausencia das aulas da turma"]
    F --> G["DELETE Aula da turma"]
    G --> H["DELETE VinculoTurmaAluno da turma"]
    H --> I["DELETE Turma"]
    I --> J{"tudo certo?"}
    J -- Sim --> K[COMMIT] --> L["Cache é atualizado<br/>grade não mostra mais a turma"]
    J -- Não, ex.: erro de conexão --> M[ROLLBACK] --> N["MostrarErro classifica o tipo<br/>de exceção e avisa o usuário"]
```

A confirmação antes de excluir (`MessageBox.Show(..., MessageBoxButton.YesNo,
...)`) é a primeira barreira; a transação no banco é a segunda — mesmo que o
usuário confirme, a operação só é considerada concluída se **todas** as
exclusões dependentes funcionarem.

## 4. Importação de cadastro de alunos via CSV

Esta é a rotina de negócio mais elaborada do sistema, porque precisa lidar
com um arquivo externo, decidir entre criar uma turma nova ou usar uma
existente, e validar **cada linha** do arquivo antes de gravar qualquer
coisa.

```mermaid
flowchart TD
    A["Usuário escolhe/cria arquivo CSV<br/>e clica 'Iniciar Importação'"] --> B["ArquivoService.LerCsv"]
    B --> C{"cabeçalho válido?<br/>(sem colunas vazias/duplicadas)"}
    C -- Não --> C1["InvalidDataException"]
    C -- Sim --> D["ImportacaoService.Importacao(...)"]
    D --> E{"tem as colunas<br/>matricula, nome, email?"}
    E -- Não --> E1["Resultado.Sucesso = false"]
    E -- Sim --> F["percorre linhas:<br/>nenhuma matrícula pode estar vazia"]
    F --> G["consulta em lote:<br/>quantas matrículas já existem no banco?"]
    G --> H{"alguma já existe?"}
    H -- Sim --> H1["Resultado.Sucesso = false<br/>'X alunos já cadastrados'"]
    H -- Não --> I{"turma já existe pelo nome?"}
    I -- Não --> I1["cria a turma primeiro"]
    I -- Sim --> J["usa a turma existente"]
    I1 --> J
    J --> K["monta a lista de objetos Aluno<br/>com a TurmaId resolvida"]
    K --> L["AlunoService.Adicionar(lista) —<br/>1 INSERT em lote por tabela"]
    L --> M["Resultado.Sucesso = true"]
```

O ponto mais importante deste fluxo — e que foi corrigido na revisão de
consistência de dados — é que **`ImportarCadastroViewModel` só fecha a janela
e atualiza o cache se `resultado.Sucesso` for `true`**:

```csharp
// ViewModel/Popups/ImportarCadastroViewModel.cs
ResultadoImportacaoCadastro resultado = _importacaoService.Importacao(nomeDaTurma, dadosCsv, DataEntrada);

if (!resultado.Sucesso)
{
    MessageBox.Show(resultado.Mensagem, "Não foi possível importar", MessageBoxButton.OK, MessageBoxImage.Warning);
    return;   // mantém o popup aberto, não mexe no cache
}

await DataCacheService.ForçarAtualizacaoAsync();
MessageBox.Show(resultado.Mensagem, "Importação concluída", MessageBoxButton.OK, MessageBoxImage.Information);
_fecharJanela?.Invoke();
```

## 5. Importação de presença (lista de chamada) via CSV

```mermaid
flowchart TD
    A["Usuário seleciona a turma<br/>e o CSV de presença"] --> B["ArquivoService.LerCsv"]
    B --> C["ImportacaoService.ImportarPresenca(turmaId, dados)"]
    C --> D{"tem uma data válida<br/>em 'data_presenca'?"}
    D -- Não --> D1["Resultado.Sucesso = false"]
    D -- Sim --> E{"turma existe?"}
    E -- Não --> E1["Resultado.Sucesso = false"]
    E -- Sim --> F{"tem as 4 colunas obrigatórias?"}
    F -- Não --> F1["Resultado.Sucesso = false"]
    F -- Sim --> G["monta o conjunto de matrículas<br/>presentes no CSV"]
    G --> H{"alguma matrícula do CSV<br/>é de OUTRA turma?"}
    H -- Sim --> H1["Resultado.Sucesso = false<br/>('não pertencem a esta turma')"]
    H -- Não --> I["busca todos os alunos<br/>DA TURMA (vínculo atual)"]
    I --> J["ausentes = alunos da turma<br/>MENOS presentes no CSV"]
    J --> K["AulaService.Adicionar(data, turmaId)<br/>cria a linha em Aula"]
    K --> L["AusenciaService.Adicionar(aulaId, ausentes)<br/>1 INSERT em lote em Ausencia"]
    L --> M["RelatorioService monta a lista<br/>de ausentes para exibir na grade"]
    M --> N["Resultado.Sucesso = true"]
```

Note como a lógica de "quem faltou" (passo `J`) é pura comparação de
conjuntos em memória (`HashSet<string>` de presentes vs. lista de matrículas
da turma) — nenhuma consulta SQL calcula ausência diretamente; é a regra de
negócio da camada de Serviço que faz essa subtração, exatamente pelo motivo
explicado em
[03-banco-de-dados-e-relacionamentos.md](03-banco-de-dados-e-relacionamentos.md)
(o banco só guarda faltas, nunca presenças).

## 6. Geração de relatórios em HTML

Os três relatórios (`RelatorioPorAluno`, `RelatorioPorAula`,
`RelatorioPorTurma` em `Servicos/RelatorioService.cs`) seguem o mesmo
molde:

```mermaid
flowchart LR
    A["Usuário marca as caixas de seleção<br/>na grade (IsSelected)"] --> B["Clica 'Gerar HTML'"]
    B --> C["ViewModel filtra os itens<br/>com IsSelected == true"]
    C --> D["SaveFileDialog — usuário escolhe<br/>onde salvar o .html"]
    D --> E["RelatorioService monta uma<br/>string HTML (StringBuilder)"]
    E --> F["File.WriteAllText grava o arquivo"]
    F --> G["Process.Start abre o navegador padrão<br/>com o arquivo gerado"]
```

O HTML é montado inteiramente em C# com `StringBuilder` (sem nenhum motor de
*template*) e cada valor inserido no meio do HTML passa por
`WebUtility.HtmlEncode(...)` — isso evita que um nome de aluno com caracteres
especiais (`<`, `&`, aspas) quebre a estrutura da página gerada.

## 7. Dois estilos de sinalização de erro convivendo no sistema

Vale explicar essa diferença porque ela aparece em vários lugares do código:

| Estilo | Onde é usado | Como o ViewModel reage |
|---|---|---|
| **Exceção** (`throw`) | Cadastro/edição/exclusão simples (`AlunoService`, `TurmaService`, `AulaService`) | `try { ... } catch (Exception ex) { MostrarErro(...) }` |
| **Objeto de resultado** (`Sucesso` + `Mensagem`) | Importações de CSV (`ImportacaoService`) | `if (!resultado.Sucesso) { mostra a mensagem; não continua }` |

A importação usa o padrão de resultado porque uma falha ali (ex.: "3
matrículas já existem") **não é excepcional** — é um resultado esperado e
comum do dia a dia de quem importa planilhas, então o código trata isso como
um fluxo normal, não como uma exceção. Já uma falha ao gravar um único
cadastro (ex.: o banco caiu no meio da operação) é, de fato, uma situação
excepcional — daí o uso de exceções nesse caso. Essa distinção (erro esperado
vs. erro excepcional) é uma prática comum em engenharia de software e está
detalhada, junto com a classificação de cada tipo de exceção, em
[05-tratamento-de-erros-e-testes.md](05-tratamento-de-erros-e-testes.md).

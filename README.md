# TCC Assiduidade

Sistema desktop para gestão de assiduidade acadêmica, desenvolvido em WPF. A aplicação centraliza cadastros de turmas e alunos, importação de chamadas por CSV, cálculo de frequência, relatórios em HTML e dashboards analíticos.

![Tela inicial do TCC Assiduidade](docs/demonstracao/capa-demonstracao.png)

## Visão geral

O TCC Assiduidade transforma listas de presença em informações úteis para acompanhamento acadêmico. A aplicação importa os alunos presentes em cada aula, identifica automaticamente os ausentes e atualiza indicadores de frequência por aluno, aula e turma.

### Principais recursos

- Cadastro, consulta, edição e exclusão de alunos e turmas.
- Importação de cadastros de alunos por arquivo CSV.
- Importação de presença e cálculo automático de ausências.
- Pesquisa e filtragem por nome, matrícula, turma e período.
- Perfil individual com total de aulas, faltas e percentual de assiduidade.
- Resumos de aulas e turmas.
- Relatórios de alunos, aulas e turmas com exportação para HTML.
- Dashboards de alunos, aulas e turmas com indicadores e gráficos.
- Cache em memória para atualização coordenada das telas.
- Modo automatizado de demonstração e gravação da interface.

## Demonstração

| Conteúdo | Arquivo |
| --- | --- |
| Gravação com massa fictícia | [gravacao-demonstracao-completa.avi](docs/demonstracao/gravacao-demonstracao-completa.avi) |
| Gravação com dados atuais do banco | [gravacao-demonstracao-dados-reais.avi](docs/demonstracao/gravacao-demonstracao-dados-reais.avi) |
| Cadastro de alunos para teste | [cadastro_alunos_demo.csv](docs/demonstracao/cadastro_alunos_demo.csv) |
| Lista de presença para teste | [presenca_demo.csv](docs/demonstracao/presenca_demo.csv) |
| Relatório HTML de exemplo | [relatorio-aula-demo.html](docs/demonstracao/relatorio-aula-demo.html) |
| Instruções da demonstração | [LEIA-ME.md](docs/demonstracao/LEIA-ME.md) |

Os vídeos são silenciosos e possuem legendas incorporadas. O modo com dados reais deve ser compartilhado somente com pessoas autorizadas, pois pode exibir informações acadêmicas existentes no banco.

## Tecnologias

- C# e .NET Framework 4.7.2
- WPF e XAML
- Padrão MVVM
- MySQL
- Material Design in XAML
- ADO.NET com `MySql.Data` e `MySqlConnector`
- Relatórios HTML e gráficos vetoriais gerados pela própria aplicação

## Arquitetura

O projeto separa apresentação, estado de tela, regras de negócio e acesso a dados:

```text
View (XAML)
    ↓ binding e comandos
ViewModel
    ↓ regras de aplicação
Servicos
    ↓ persistência
Repositories
    ↓
MySQL
```

Principais diretórios:

```text
TCC-Assiduidade/
├── Modelos/          # Entidades, DTOs, resultados e modelos de dashboard
├── Repositories/     # Consultas e comandos SQL
├── Servicos/         # Regras de negócio, cache, importação e relatórios
├── View/             # Janelas, telas, pop-ups e controles WPF
├── ViewModel/        # Estado, comandos e comportamento das telas
├── docs/             # Massas de teste, demonstrações e documentação
├── App.xaml          # Inicialização e recursos da aplicação
└── App.config        # Configuração do .NET e conexão com o banco
```

## Pré-requisitos

- Windows 10 ou Windows 11.
- Visual Studio com a carga de trabalho **Desenvolvimento para desktop com .NET**.
- .NET Framework 4.7.2 Developer Pack.
- Instância MySQL acessível pela máquina.

As dependências NuGet estão declaradas em [`packages.config`](packages.config).

## Configuração do banco de dados

Defina a conexão `MyDbConn` em `App.config`:

```xml
<connectionStrings>
  <add name="MyDbConn"
       connectionString="server=SEU_HOST;port=3306;database=SEU_BANCO;user=SEU_USUARIO;password=SUA_SENHA;SslMode=Required;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

> **Segurança:** não publique senhas reais no repositório. Para distribuição, mantenha segredos fora do controle de versão e injete a configuração durante a implantação.

O banco utilizado pela aplicação contém as entidades conceituais `Aluno`, `Turma`, `VinculoTurmaAluno`, `Aula` e `Ausencia`. A estrutura deve existir antes da primeira execução.

Turmas usam os estados Ativa, Arquivada e Excluida, com tabela de referência e
data da última transição. Os vínculos, aulas e ausências permanecem no banco.
A migração [003_status_turmas.sql](docs/banco/003_status_turmas.sql) deve ser
validada com backup antes de ser aplicada a bancos que ainda usam o esquema
legado. Ela foi aplicada ao banco configurado em `App.config` em 11/10/2026;
consulte o [guia da migração](docs/banco/README.md) para o registro e a validação.

## Compilação e execução

### Visual Studio

1. Abra `TCC Assiduidade.slnx`.
2. Restaure os pacotes NuGet.
3. Selecione a configuração `Debug` ou `Release`.
4. Compile a solução e execute o projeto `TCC Assiduidade`.

### Linha de comando

Em um Developer PowerShell do Visual Studio:

```powershell
msbuild ".\TCC Assiduidade.csproj" /t:Build /p:Configuration=Debug
& ".\bin\Debug\TCC Assiduidade.exe"
```

Se a assinatura do manifesto não estiver disponível na máquina de desenvolvimento, a compilação local pode ser feita com:

```powershell
msbuild ".\TCC Assiduidade.csproj" /t:Build /p:Configuration=Debug /p:SignManifests=false /p:GenerateManifests=false
```

## Importação de arquivos CSV

Os arquivos podem utilizar vírgula ou ponto e vírgula como separador. Os nomes das colunas não diferenciam maiúsculas de minúsculas.

### Cadastro de alunos

```csv
matricula,nome,email
2026001,Ana Silva,ana.silva@exemplo.com
2026002,Bruno Souza,bruno.souza@exemplo.com
```

Colunas obrigatórias: `matricula`, `nome` e `email`.

### Lista de presença

```csv
matricula_aluno,email_aluno,nome_aluno,data_presenca
2026001,ana.silva@exemplo.com,Ana Silva,17/09/2026
```

Colunas obrigatórias: `matricula_aluno`, `email_aluno`, `nome_aluno` e `data_presenca`.

Na importação de presença, os alunos da turma que não aparecem no CSV são registrados como ausentes na aula criada.

## Relatórios e dashboards

Os relatórios podem ser filtrados e exportados para HTML. Os dashboards disponibilizam:

- assiduidade e evolução individual do aluno;
- presença média e ausências por aula;
- comparação e ranking de turmas;
- identificação de alunos abaixo do limite de 75%;
- filtros por turma e intervalo de datas.

## Modos de demonstração

Após compilar o projeto, execute um dos comandos:

```powershell
# Demonstração com a massa fictícia filtrada
& ".\bin\Debug\TCC Assiduidade.exe" --demonstracao

# Demonstração exibindo os dados atuais do banco
& ".\bin\Debug\TCC Assiduidade.exe" --demonstracao-dados-reais
```

Os modos percorrem automaticamente as telas, geram um vídeo AVI em `docs/demonstracao` e reutilizam a massa fictícia quando ela já existe. Exclusões não são executadas.

## Fluxo de presença

1. O usuário cadastra ou importa uma turma e seus alunos.
2. Para cada aula, importa um CSV com os estudantes presentes.
3. O sistema valida a turma, as matrículas e a data.
4. Uma nova aula é registrada.
5. Os estudantes que não constam na presença são marcados como ausentes.
6. Cache, perfis, relatórios e dashboards são atualizados.

## Estado do projeto

O projeto possui os fluxos principais de cadastro, importação, consulta, relatórios e dashboards implementados. Antes de publicar uma versão de produção, recomenda-se adicionar testes automatizados, migrações de banco, gerenciamento externo de segredos e empacotamento de instalação sem credenciais incorporadas.

## Licença

Este repositório não possui uma licença de código aberto definida. Consulte os responsáveis pelo projeto antes de reutilizar ou redistribuir o código.

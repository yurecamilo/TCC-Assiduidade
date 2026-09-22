using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Resultados;
using TCC_Assiduidade.View.Dashboards;
using TCC_Assiduidade.View.Popups;
using TCC_Assiduidade.View.Relatórios;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Dashboards;
using TCC_Assiduidade.ViewModel.Popups;
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.Servicos.Demonstracao
{
    public sealed class DemonstracaoService
    {
        private const string NomeTurma = "Turma Demonstração 17-09-2026";
        private static readonly DateTime DataAula = new DateTime(2026, 9, 17);
        private static readonly List<string> Matriculas = new List<string>
        {
            "DEMO26091701", "DEMO26091702", "DEMO26091703", "DEMO26091704", "DEMO26091705"
        };

        private readonly StringBuilder _registro = new StringBuilder();

        public async Task ExecutarAsync(Window janelaPrincipal, bool mostrarDadosReais = false)
        {
            string raiz = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."));
            string pasta = Path.Combine(raiz, "docs", "demonstracao");
            string sufixo = mostrarDadosReais ? "-dados-reais" : string.Empty;
            string video = Path.Combine(pasta, "gravacao-demonstracao" +
                (mostrarDadosReais ? "-dados-reais" : "-completa") + ".avi");
            string capa = Path.Combine(pasta, "capa-demonstracao" + sufixo + ".png");
            string status = Path.Combine(pasta, "resultado-demonstracao" + sufixo + ".txt");

            Directory.CreateDirectory(pasta);
            try
            {
                await AguardarCacheAsync();
                await PrepararDadosAsync(pasta);

                Turma turmaModelo = DataCacheService.TurmaModeloCache
                    .FirstOrDefault(t => string.Equals(t.Nome, NomeTurma, StringComparison.OrdinalIgnoreCase));
                TurmaExibicaoDTO turmaDemonstracao = DataCacheService.TurmasCache
                    .FirstOrDefault(t => string.Equals(t.Nome, NomeTurma, StringComparison.OrdinalIgnoreCase));
                AlunoExibicaoDTO alunoDemonstracao = DataCacheService.AlunosCache
                    .FirstOrDefault(a => a.Matricula == Matriculas[0]);
                AulaExibicaoDTO aulaDemonstracao = DataCacheService.AulasCache
                    .FirstOrDefault(a => a.Data.Date == DataAula.Date &&
                                         string.Equals(a.Turma, NomeTurma, StringComparison.OrdinalIgnoreCase));

                TurmaExibicaoDTO turma = mostrarDadosReais
                    ? DataCacheService.TurmasCache.FirstOrDefault(t =>
                        !string.Equals(t.Nome, NomeTurma, StringComparison.OrdinalIgnoreCase)) ?? turmaDemonstracao
                    : turmaDemonstracao;
                AlunoExibicaoDTO aluno = mostrarDadosReais
                    ? DataCacheService.AlunosCache.FirstOrDefault(a => !Matriculas.Contains(a.Matricula)) ?? alunoDemonstracao
                    : alunoDemonstracao;
                AulaExibicaoDTO aula = mostrarDadosReais
                    ? DataCacheService.AulasCache.FirstOrDefault(a =>
                        !string.Equals(a.Turma, NomeTurma, StringComparison.OrdinalIgnoreCase)) ?? aulaDemonstracao
                    : aulaDemonstracao;

                GerarRelatorioDeAula(pasta, aula, mostrarDadosReais);

                using (var gravador = new GravadorJanelaService(janelaPrincipal, video, capa))
                {
                    await gravador.MostrarPorAsync(mostrarDadosReais
                        ? "Início — aplicação conectada aos dados atuais do banco"
                        : "Início — atalhos para importações e cadastros", 3);

                    await MostrarImportacaoCadastroAsync(janelaPrincipal, gravador);
                    await NavegarAsync(janelaPrincipal, "BtnTurmas", gravador,
                        "Turmas — busca, quantidade de alunos, resumo, edição e exclusão", 3);

                    UserControl telaAtual = ObterTelaAtual(janelaPrincipal);
                    var turmasVm = telaAtual == null ? null : telaAtual.DataContext as TurmasViewModel;
                    if (!mostrarDadosReais && turmasVm != null)
                        turmasVm.TextoBusca = "Demonstração";
                    await gravador.MostrarPorAsync(mostrarDadosReais
                        ? "Turmas reais carregadas do banco, com totais e ações disponíveis"
                        : "Busca aplicada à turma criada pela importação", 3);

                    if (turma != null)
                    {
                        await MostrarPopupAsync(janelaPrincipal, new ResumoTurma(turma), gravador,
                            "Resumo da turma — alunos e indicadores consolidados", 3);
                        await MostrarPopupAsync(janelaPrincipal, new EditarTurma(turma), gravador,
                            "Edição de turma — formulário real preenchido", 3);
                    }

                    var novaTurma = new NovaTurma();
                    ((NovaTurmaViewModel)novaTurma.DataContext).NomeNovaTurma = "Nova Turma — exemplo de criação";
                    await MostrarPopupAsync(janelaPrincipal, novaTurma, gravador,
                        "Criação manual de turma — validação e salvamento disponíveis", 3);

                    ClicarMenu(janelaPrincipal, "BtnAlunos");
                    await Task.Delay(350);
                    telaAtual = ObterTelaAtual(janelaPrincipal);
                    var alunosVm = telaAtual == null ? null : telaAtual.DataContext as AlunosViewModel;
                    if (!mostrarDadosReais && alunosVm != null)
                        alunosVm.TextoBusca = "Demonstração";
                    await gravador.MostrarPorAsync(
                        "Alunos — pesquisa, filtro por turma, perfil, edição e exclusão", 3);
                    if (alunosVm != null && aluno != null)
                        alunosVm.VisualizarDadosCommand.Execute(aluno);
                    await gravador.MostrarPorAsync("Perfil de aluno e frequência calculada após a chamada", 3);

                    if (aluno != null)
                    {
                        await MostrarPopupAsync(janelaPrincipal, new EditarAluno(aluno), gravador,
                            "Edição de aluno — matrícula, nome, e-mail, turma e data de entrada", 3);
                    }

                    var novoAluno = new NovoAluno();
                    var novoAlunoVm = (NovoAlunoViewModel)novoAluno.DataContext;
                    novoAlunoVm.Matricula = "DEMO26091706";
                    novoAlunoVm.Nome = "Aluno Novo — demonstração";
                    novoAlunoVm.Email = "aluno.novo.demo@exemplo.com";
                    novoAlunoVm.TurmaSelecionada = turmaModelo;
                    novoAlunoVm.DataEntrada = DataAula;
                    await MostrarPopupAsync(janelaPrincipal, novoAluno, gravador,
                        "Criação manual de aluno — formulário real preenchido", 3);

                    await MostrarImportacaoPresencaAsync(janelaPrincipal, gravador, turmaModelo);

                    if (aula != null)
                    {
                        await MostrarPopupAsync(janelaPrincipal, new ResumoAula(aula), gravador,
                            "Resumo da aula — três presentes e duas ausências registradas", 3);
                    }

                    await DemonstrarRelatoriosAsync(janelaPrincipal, gravador, mostrarDadosReais);
                    await DemonstrarDashboardsAsync(janelaPrincipal, gravador, mostrarDadosReais);

                    await NavegarAsync(janelaPrincipal, "BtnInicio", gravador,
                        "Demonstração concluída — arquivos CSV, relatório HTML e gravação foram gerados", 4);
                }

                _registro.AppendLine("Gravação concluída: " + video);
                _registro.AppendLine("Capa gerada: " + capa);
                File.WriteAllText(status, _registro.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _registro.AppendLine("Falha na demonstração: " + ex);
                File.WriteAllText(status, _registro.ToString(), Encoding.UTF8);
            }
            finally
            {
                await Task.Delay(700);
                Application.Current.Shutdown();
            }
        }

        private async Task PrepararDadosAsync(string pasta)
        {
            var alunoService = new AlunoService();
            int existentes = alunoService.ObterQuantidadeMatriculasExistentes(Matriculas);
            ValidarQuantidadeMassa(existentes, Matriculas.Count);

            if (existentes == 0)
            {
                var importacao = new ImportacaoService();
                var dados = ArquivoService.LerCsv(Path.Combine(pasta, "cadastro_alunos_demo.csv"));
                ResultadoImportacaoCadastro resultado = importacao.Importacao(
                    NomeTurma, dados, new DateTime(2026, 9, 1));
                _registro.AppendLine("Cadastro: " + resultado.Mensagem);
                if (!resultado.Sucesso)
                    throw new InvalidOperationException(resultado.Mensagem);
                await DataCacheService.ForçarAtualizacaoAsync();
            }
            else
            {
                _registro.AppendLine("Cadastro: massa de demonstração já existente; reutilizada.");
            }

            Turma turma = DataCacheService.TurmaModeloCache
                .FirstOrDefault(t => string.Equals(t.Nome, NomeTurma, StringComparison.OrdinalIgnoreCase));
            if (turma == null)
            {
                await DataCacheService.ForçarAtualizacaoAsync();
                turma = DataCacheService.TurmaModeloCache
                    .FirstOrDefault(t => string.Equals(t.Nome, NomeTurma, StringComparison.OrdinalIgnoreCase));
            }
            if (turma == null)
                throw new InvalidOperationException("A turma da demonstração não foi localizada após a importação.");

            bool aulaExiste = DataCacheService.AulasCache.Any(a =>
                a.Data.Date == DataAula.Date && string.Equals(a.Turma, NomeTurma, StringComparison.OrdinalIgnoreCase));
            if (!aulaExiste)
            {
                var importacao = new ImportacaoService();
                var dados = ArquivoService.LerCsv(Path.Combine(pasta, "presenca_demo.csv"));
                ResultadoImportacaoPresenca resultado = importacao.ImportarPresenca(turma.Id, dados);
                _registro.AppendLine("Presença: " + resultado.Mensagem);
                if (!resultado.Sucesso)
                    throw new InvalidOperationException(resultado.Mensagem);
                await DataCacheService.ForçarAtualizacaoAsync();
            }
            else
            {
                _registro.AppendLine("Presença: aula de demonstração já existente; reutilizada.");
            }
        }

        internal static void ValidarQuantidadeMassa(int existentes, int totalEsperado)
        {
            if (totalEsperado <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalEsperado));
            if (existentes < 0 || existentes > totalEsperado)
                throw new ArgumentOutOfRangeException(nameof(existentes));
            if (existentes > 0 && existentes < totalEsperado)
            {
                throw new InvalidOperationException(
                    "A massa de demonstração está incompleta: foram encontrados " + existentes +
                    " de " + totalEsperado + " alunos. Remova a massa parcial ou complete-a antes de gerar a demonstração.");
            }
        }

        private void GerarRelatorioDeAula(string pasta, AulaExibicaoDTO aula, bool mostrarDadosReais)
        {
            if (aula == null)
                return;
            var servico = new RelatorioService();
            string html = servico.RelatorioPorAula(aula, servico.ObterDadosRelatorio(aula.AulaId));
            string caminho = Path.Combine(pasta, mostrarDadosReais
                ? "relatorio-aula-dados-reais.html"
                : "relatorio-aula-demo.html");
            File.WriteAllText(caminho, html, Encoding.UTF8);
            _registro.AppendLine("Relatório HTML gerado: " + caminho);
        }

        private async Task MostrarImportacaoCadastroAsync(Window principal, GravadorJanelaService gravador)
        {
            var popup = new ImportarCadastro();
            var vm = (ImportarCadastroViewModel)popup.DataContext;
            vm.IsNovaTurma = true;
            vm.NovaTurmaNome = NomeTurma;
            vm.DataEntrada = new DateTime(2026, 9, 1);
            vm.CaminhoArquivo = "cadastro_alunos_demo.csv";
            await MostrarPopupAsync(principal, popup, gravador,
                "Importação de cadastro — CSV com cinco alunos fictícios", 4);
        }

        private async Task MostrarImportacaoPresencaAsync(Window principal, GravadorJanelaService gravador, Turma turma)
        {
            var popup = new ImportarPresenca();
            var vm = (ImportarPresencaViewModel)popup.DataContext;
            vm.TurmaSelecionada = turma;
            vm.CaminhoArquivo = "presenca_demo.csv";
            await MostrarPopupAsync(principal, popup, gravador,
                "Importação de presença — três presentes; Carla e Elisa ficam ausentes", 4);
        }

        private static async Task DemonstrarRelatoriosAsync(Window principal, GravadorJanelaService gravador,
            bool mostrarDadosReais)
        {
            await NavegarAsync(principal, "BtnRelatorios", gravador,
                "Relatórios — escolha entre aulas, alunos e turmas", 3);
            UserControl tela = ObterTelaAtual(principal);
            var vm = tela == null ? null : tela.DataContext as RelatoriosViewModel;
            if (vm == null)
                return;

            vm.AbrirRelatorioAulasCommand.Execute(null);
            await gravador.MostrarPorAsync("Relatório de aulas — filtros por turma e período e exportação HTML", 3);
            vm.ConteudoAtual = null;
            vm.AbrirRelatorioAlunosCommand.Execute(null);
            var relatorioAlunosView = vm.ConteudoAtual as RelatorioAlunosView;
            var relatorioAlunosVm = relatorioAlunosView == null
                ? null
                : relatorioAlunosView.DataContext as RelatorioAlunosViewModel;
            if (!mostrarDadosReais && relatorioAlunosVm != null)
                relatorioAlunosVm.TextoBusca = "Demonstração";
            await gravador.MostrarPorAsync("Relatório de alunos — seleção em lote, busca e exportação HTML", 3);
            vm.ConteudoAtual = null;
            vm.AbrirRelatorioTurmasCommand.Execute(null);
            await gravador.MostrarPorAsync("Relatório de turmas — seleção, busca e geração em lote", 3);
        }

        private static async Task DemonstrarDashboardsAsync(Window principal, GravadorJanelaService gravador,
            bool mostrarDadosReais)
        {
            await NavegarAsync(principal, "BtnDashboards", gravador,
                "Dashboards — análises visuais de alunos, aulas e turmas", 3);
            UserControl tela = ObterTelaAtual(principal);
            var vm = tela == null ? null : tela.DataContext as DashboardsViewModel;
            if (vm == null)
                return;

            vm.AbrirDashboardAlunosCommand.Execute(null);
            await Task.Delay(900);
            var dashboardAlunosView = vm.ConteudoAtual as DashboardAlunosView;
            var dashboardAlunosVm = dashboardAlunosView == null
                ? null
                : dashboardAlunosView.DataContext as DashboardAlunosViewModel;
            if (!mostrarDadosReais && dashboardAlunosVm != null)
            {
                dashboardAlunosVm.TurmaSelecionada = dashboardAlunosVm.Turmas
                    .FirstOrDefault(t => string.Equals(t.Nome, NomeTurma, StringComparison.OrdinalIgnoreCase));
                dashboardAlunosVm.AlunoSelecionado = dashboardAlunosVm.Alunos
                    .FirstOrDefault(a => a.Matricula == Matriculas[0]);
            }
            await gravador.MostrarPorAsync("Dashboard de alunos — frequência, evolução e comparação com a turma", 4);
            vm.ConteudoAtual = null;
            vm.AbrirDashboardAulasCommand.Execute(null);
            await Task.Delay(900);
            await gravador.MostrarPorAsync("Dashboard de aulas — presença média, ausências e evolução", 4);
            vm.ConteudoAtual = null;
            vm.AbrirDashboardTurmasCommand.Execute(null);
            await Task.Delay(900);
            await gravador.MostrarPorAsync("Dashboard de turmas — ranking e assiduidade consolidada", 4);
        }

        private static async Task NavegarAsync(Window principal, string nomeBotao,
            GravadorJanelaService gravador, string legenda, double segundos)
        {
            ClicarMenu(principal, nomeBotao);
            await Task.Delay(350);
            await gravador.MostrarPorAsync(legenda, segundos);
        }

        private static void ClicarMenu(Window principal, string nomeBotao)
        {
            var botao = principal.FindName(nomeBotao) as Button;
            if (botao == null)
                throw new InvalidOperationException("Botão de navegação não encontrado: " + nomeBotao);
            botao.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            principal.UpdateLayout();
        }

        private static async Task MostrarPopupAsync(Window principal, Window popup,
            GravadorJanelaService gravador, string legenda, double segundos)
        {
            popup.Owner = principal;
            popup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            popup.Show();
            popup.UpdateLayout();
            await Task.Delay(350);
            await gravador.MostrarPorAsync(legenda, segundos);
            popup.Close();
            await Task.Delay(250);
        }

        private static UserControl ObterTelaAtual(Window principal)
        {
            var host = principal.FindName("ConteudoPrincipal") as ContentControl;
            return host == null ? null : host.Content as UserControl;
        }

        private static async Task AguardarCacheAsync()
        {
            await DataCacheService.AguardarCarregamentoAsync();
        }
    }
}

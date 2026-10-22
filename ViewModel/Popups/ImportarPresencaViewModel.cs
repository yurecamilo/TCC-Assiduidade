using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.Resultados;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class ImportarPresencaViewModel :BaseViewModel
    {
        private List<Turma> _listaOriginalDoBanco = new List<Turma>();

        private readonly ImportacaoService _importacaoService;
        private readonly TurmaService _turmaService;

        private List<Turma> _turmas;
        private Turma _turmaSelecionada;
        private string _caminhoArquivo = "Nenhum arquivo selecionado";
        private string _caminhoArquivoCompleto;

        public string CaminhoArquivo
        {
            get => _caminhoArquivo;
            set { _caminhoArquivo = value; OnPropertyChanged(); }
        }
        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }
        public Turma TurmaSelecionada
        {
            get => _turmaSelecionada;
            set
            {
                _turmaSelecionada = value;
                OnPropertyChanged();
            }
        }

        public ICommand IniciarImportacaoCommand { get; private set; }
        public ICommand SelecionarArquivoCommand { get; private set; }
        public ICommand SincronizarFormsCommand { get; private set; }

        // Respostas baixadas da planilha do Google Forms. Quando existem,
        // substituem o arquivo CSV escolhido no recurso "Iniciar Importação".
        private List<Dictionary<string, string>> _dadosFormsPendentes;

        private readonly Action _fecharJanela;
        public ImportarPresencaViewModel(Action fecharJanela)
        {
            _fecharJanela = fecharJanela;
            _turmaService = new TurmaService();
            _importacaoService = new ImportacaoService();
            // Bloqueia o botão enquanto a importação estiver em execução (overlay ativo),
            // evitando que um segundo clique inicie outra importação concorrente.
            IniciarImportacaoCommand = new RelayCommand(async () => await ExecutarImportacao(), () => !EstaOcupado);
            SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);
            SincronizarFormsCommand = new RelayCommand(async () => await SincronizarForms(), () => !EstaOcupado);
            _ = CarregarTurmasDoBanco();
        }

        private void SelecionarArquivo()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Selecionar arquivo CSV da turma"
            };
            if (dialog.ShowDialog() == true)
            {
                // Um CSV manual substitui qualquer resposta baixada do Forms.
                _dadosFormsPendentes = null;
                CaminhoArquivo = dialog.FileName.Substring(dialog.FileName.LastIndexOf('\\') + 1);
                _caminhoArquivoCompleto = dialog.FileName;
            }
        }

        /// <summary>
        /// Baixa o CSV publicado da planilha de respostas do Google Forms e
        /// deixa os dados prontos para o "Iniciar Importação" da mesma forma
        /// que um arquivo escolhido à mão — toda a validação e gravação
        /// continuam no ImportacaoService, que é o único caminho de escrita.
        /// </summary>
        private async Task SincronizarForms()
        {
            if (TurmaSelecionada == null)
            {
                PopupMensagem.Show("Selecione uma turma.");
                return;
            }

            try
            {
                IniciarProgresso("Baixando respostas da planilha (Google Forms)...");

                List<Dictionary<string, string>> respostas = await Task.Run(() =>
                    GoogleFormsService.BaixarRespostas());

                if (respostas.Count == 0)
                {
                    PopupMensagem.Show("A planilha publicada ainda não tem respostas para importar.", "Sem respostas", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _dadosFormsPendentes = respostas;
                CaminhoArquivo = $"Planilha do Google Forms — {respostas.Count} resposta(s) baixada(s)";
            }
            catch (Exception ex)
            {
                MostrarErro("Não foi possível baixar as respostas do Google Forms. Verifique a internet e tente novamente.", ex);
            }
            finally
            {
                FinalizarProgresso();
            }
        }

        private async Task ExecutarImportacao()
        {

            if (TurmaSelecionada == null)
            {
                PopupMensagem.Show("Selecione uma turma.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_caminhoArquivoCompleto) && _dadosFormsPendentes == null)
            {
                PopupMensagem.Show("Escolha um arquivo CSV, ou sincronize as respostas do Google Forms.");
                return;
            }

            {
                try
                {
                    IniciarProgresso("Carregando os dados de presença...");
                    List<Dictionary<string, string>> dadosCsv = _dadosFormsPendentes
                        ?? await Task.Run(() => ArquivoService.LerCsv(_caminhoArquivoCompleto));

                    AtualizarProgresso(35, "Validando os dados da lista de presença...");

                    ResultadoImportacaoPresenca resultado = await Task.Run(() =>
                        _importacaoService.ImportarPresenca(TurmaSelecionada.Id, dadosCsv));

                    // Se a importação falhou (turma sem alunos, coluna faltando, etc.),
                    // avisa o motivo sem atualizar o cache como se tivesse dado certo.
                    if (!resultado.Sucesso)
                    {
                        PopupMensagem.Show(resultado.Mensagem, "Não foi possível importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    AtualizarProgresso(90, "Atualizando os dados da aplicação...");

                    // A transação da importação já foi confirmada. Uma falha ao
                    // atualizar o cache não significa que a importação foi rejeitada.
                    try
                    {
                        await DataCacheService.ForçarAtualizacaoAsync();
                    }
                    catch (Exception exAtualizacao)
                    {
                        MostrarErro("A importação foi concluída e os dados foram gravados, mas a tela não conseguiu atualizar. Reabra ou atualize a tela para consultar os dados.", exAtualizacao);
                    }

                    ConcluirProgresso("Importação concluída!");
                    await Task.Delay(600);

                    PopupMensagem.Show(resultado.Mensagem, "Importação concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                    _fecharJanela?.Invoke();
                }
                catch (Exception ex)
                {
                    MostrarErro("Nao foi possivel importar a presenca. Confira o arquivo CSV e tente novamente.", ex);
                }
                finally
                {
                    FinalizarProgresso();
                }
            }
        }

        async Task CarregarTurmasDoBanco()
        {
            try
            {
                await DataCacheService.AguardarCarregamentoAsync();

                _listaOriginalDoBanco = DataCacheService.TurmaModeloCache ?? new List<Turma>();
                Turmas = _listaOriginalDoBanco;
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel carregar as turmas. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }
    }
}

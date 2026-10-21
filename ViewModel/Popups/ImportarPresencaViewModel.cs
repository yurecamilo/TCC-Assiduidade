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

        private readonly Action _fecharJanela;
        public ImportarPresencaViewModel(Action fecharJanela)
        {
            _fecharJanela = fecharJanela;
            _turmaService = new TurmaService();
            _importacaoService = new ImportacaoService();
            IniciarImportacaoCommand = new RelayCommand(async () => await ExecutarImportacao());
            SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);
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
                CaminhoArquivo = dialog.FileName.Substring(dialog.FileName.LastIndexOf('\\') + 1);
                _caminhoArquivoCompleto = dialog.FileName;
            }
        }

        private async Task ExecutarImportacao()
        {

            if (TurmaSelecionada == null)
            {
                PopupMensagem.Show("Selecione uma turma.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_caminhoArquivoCompleto))
            {
                PopupMensagem.Show("Escolha um arquivo CSV.");
                return;
            }

            {
                try
                {
                    IniciarProgresso("Lendo o arquivo CSV...");
                    List<Dictionary<string, string>> dadosCsv = await Task.Run(() =>
                        ArquivoService.LerCsv(_caminhoArquivoCompleto));

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

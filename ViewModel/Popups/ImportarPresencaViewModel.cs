using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.Relatorios;
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
        private List<RelatorioAusente> _ausentes;
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
                MessageBox.Show("Selecione uma turma.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_caminhoArquivoCompleto))
            {
                MessageBox.Show("Escolha um arquivo CSV.");
                return;
            }

            {
                try
                {
                    List<Dictionary<string, string>> dadosCsv = ArquivoService.LerCsv(_caminhoArquivoCompleto);
                    ResultadoImportacaoPresenca resultado = _importacaoService.ImportarPresenca(TurmaSelecionada.Id, dadosCsv);

                    await DataCacheService.ForçarAtualizacaoAsync();

                    MessageBox.Show(resultado.Mensagem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro critico: " + ex.Message);
                }
            }
        }

        async Task CarregarTurmasDoBanco()
        {
            try
            {
                while (!DataCacheService.IsCarregado)
                {
                    await Task.Delay(100);
                }

                _listaOriginalDoBanco = DataCacheService.TurmaModeloCache ?? new List<Turma>();
                Turmas = _listaOriginalDoBanco;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar turmas do cache: " + ex.Message);
            }
        }
    }
}

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
    public class ImportarCadastroViewModel : BaseViewModel
    {
        private List<Turma> _listaOriginalDoBanco = new List<Turma>();
        private readonly ImportacaoService _importacaoService;

        private string _novaTurmaNome;
        private string _caminhoArquivo = "Nenhum arquivo selecionado";
        private string _caminhoArquivoCompleto;
        private bool _isNovaTurma = true;
        private bool _isTurmaExistente = false;
        private List<Turma> _turmas;
        private Turma _turmaSelecionada;
        private DateTime? _dataEntrada; // Campo privado para a data externa

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

        // Propriedades da UI
        public string NovaTurmaNome
        {
            get => _novaTurmaNome;
            set { _novaTurmaNome = value; OnPropertyChanged(); }
        }

        public string CaminhoArquivo
        {
            get => _caminhoArquivo;
            set { _caminhoArquivo = value; OnPropertyChanged(); }
        }

        public bool IsNovaTurma
        {
            get => _isNovaTurma;
            set
            {
                _isNovaTurma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTurmaExistente)); // Avisa que o outro mudou
            }
        }

        public bool IsTurmaExistente
        {
            get => _isTurmaExistente;
            set
            {
                _isTurmaExistente = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNovaTurma)); // Avisa que o outro mudou
            }
        }

        // NOVA PROPRIEDADE PÚBLICA: Vinculada com o SelectedDate do DatePicker do Passo 2
        public DateTime? DataEntrada
        {
            get => _dataEntrada;
            set { _dataEntrada = value; OnPropertyChanged(); }
        }

        // Comandos
        public ICommand IniciarImportacaoCommand { get; private set; }
        public ICommand SelecionarArquivoCommand { get; private set; }

        private readonly Action _fecharJanela;
        public ImportarCadastroViewModel(Action fecharJanela)
        {
            _fecharJanela = fecharJanela;
            _importacaoService = new ImportacaoService();
            IniciarImportacaoCommand = new RelayCommand(async () => await ExecutarImportacao());
            SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);

            // Define a data padrão exibida na tela como o dia de hoje ao abrir o popup
            DataEntrada = DateTime.Now.Date;

            // Carrega as turmas existentes do seu cache
            _ = CarregarTurmasDoBanco();
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
                MostrarErro("Nao foi possivel carregar as turmas. Verifique a conexao com o banco e tente novamente.", ex);
            }
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
                CaminhoArquivo = System.IO.Path.GetFileName(dialog.FileName);
                _caminhoArquivoCompleto = dialog.FileName;
            }
        }

        private async Task ExecutarImportacao()
        {
            // 1. Validação de destino
            string nomeDaTurma = IsNovaTurma ? NovaTurmaNome : TurmaSelecionada?.Nome;

            if (string.IsNullOrWhiteSpace(nomeDaTurma))
            {
                MessageBox.Show(IsNovaTurma ? "Informe o nome da nova turma." : "Selecione uma turma existente.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_caminhoArquivoCompleto))
            {
                MessageBox.Show("Escolha um arquivo CSV.");
                return;
            }

            try
            {
                List<Dictionary<string, string>> dadosCsv = ArquivoService.LerCsv(_caminhoArquivoCompleto);

                // ENVIANDO O DADO EXTERNO: Passamos a DataEntrada capturada na tela para a regra de negócio do Service
                ResultadoImportacaoCadastro resultado = _importacaoService.Importacao(nomeDaTurma, dadosCsv, DataEntrada);

                // Atualiza o cache global
                await DataCacheService.ForçarAtualizacaoAsync();

                MessageBox.Show(resultado.Mensagem);
                _fecharJanela?.Invoke();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel importar o cadastro. Confira o arquivo CSV e tente novamente.", ex);
            }
        }
    }
}

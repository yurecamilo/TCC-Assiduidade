using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Modelos.Resultados;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class ImportarCadastroViewModel : BaseViewModel
    {
        private readonly ImportacaoService _importacaoService;

        private string _novaTurmaNome;
        private string _caminhoArquivo = "Nenhum arquivo selecionado";
        private bool _isTurmaExistente = true;
        private string _caminhoArquivoCompleto;

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

        public bool IsTurmaExistente
        {
            get => _isTurmaExistente;
            set { _isTurmaExistente = value; OnPropertyChanged(); }
        }

        public ICommand IniciarImportacaoCommand { get; private set; }
        public ICommand SelecionarArquivoCommand { get; private set; }

        public ImportarCadastroViewModel()
        {
            _importacaoService = new ImportacaoService();
            IniciarImportacaoCommand = new RelayCommand(ExecutarImportacao);
            SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);
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

        private void ExecutarImportacao()
        {
            if (string.IsNullOrWhiteSpace(NovaTurmaNome))
            {
                MessageBox.Show("Informe o nome da turma.");
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
                ResultadoImportacaoCadastro resultado = _importacaoService.Importacao(NovaTurmaNome, dadosCsv);

                MessageBox.Show(resultado.Mensagem);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro critico: " + ex.Message);
            }
        }
    }
}
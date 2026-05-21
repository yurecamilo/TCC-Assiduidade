using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Servicos;     
using TCC_Assiduidade.ViewModel.Base;
using TCC_Assiduidade.ViewModels.Base;

namespace TCC_Assiduidade
{
    public class PresencaViewModel : BaseViewModel
    {
        private readonly ImportacaoService _importacaoService;
        private readonly TurmaService _turmaService;

        private List<Turma> _turmas;
        private Turma _turmaSelecionada;
        private string _tbDados;

        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public string TbDados
        {
            get => _tbDados;
            set { _tbDados = value; OnPropertyChanged(); }
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

        public ICommand ImportarPresencaCommand { get; private set; }
        public PresencaViewModel()
        {
            _turmaService = new TurmaService();
            _importacaoService = new ImportacaoService();
            ImportarPresencaCommand = new RelayCommand(ExecutarImportacao);
            CarregarTurmasDoBanco();
        }

        private void ExecutarImportacao()
        {
            TbDados = string.Empty;

            if (TurmaSelecionada == null)
            {
                MessageBox.Show("Selecione uma turma.");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Selecionar arquivo CSV da turma"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    List<Dictionary<string, string>> dadosCsv = ArquivoService.LerCsv(dialog.FileName);
                    TbDados = _importacaoService.ImportarPresenca(TurmaSelecionada.Id, dadosCsv);
                }
                catch (Exception ex)
                {
                    TbDados = $"Erro crítico: {ex.Message}\n";
                }
            }
        }

        private void CarregarTurmasDoBanco()
        {
            Turmas = _turmaService.ObterTodasTurmas();
        }
    }
}
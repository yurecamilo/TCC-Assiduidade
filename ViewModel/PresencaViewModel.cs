using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Modelos.Resultados;
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
        private List<RelatorioAusente> _ausentes;

        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public List<RelatorioAusente> Ausentes
        {
            get => _ausentes;
            set { _ausentes = value; OnPropertyChanged(); }
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
            Ausentes = new List<RelatorioAusente>();
            ImportarPresencaCommand = new RelayCommand(ExecutarImportacao);
            CarregarTurmasDoBanco();
        }

        private void ExecutarImportacao()
        {
            Ausentes = new List<RelatorioAusente>();

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
                    ResultadoImportacaoPresenca resultado = _importacaoService.ImportarPresenca(TurmaSelecionada.Id, dadosCsv);

                    Ausentes = resultado.Ausentes;
                    MessageBox.Show(resultado.Mensagem);
                }
                catch (Exception ex)
                {
                    Ausentes = new List<RelatorioAusente>();
                    MessageBox.Show("Erro critico: " + ex.Message);
                }
            }
        }

        private void CarregarTurmasDoBanco()
        {
            Turmas = _turmaService.ObterTodasTurmas();
        }
    }
}

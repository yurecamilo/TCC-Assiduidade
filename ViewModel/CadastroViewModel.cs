using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;
using TCC_Assiduidade.ViewModels.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class CadastroViewModel : BaseViewModel
    {
        private readonly ImportacaoService _importacaoService;
        private string _turmaNome;
        private string _tbDados;

        public string TurmaNome
        {
            get => _turmaNome;
            set { _turmaNome = value; OnPropertyChanged(); }
        }

        public string TbDados
        {
            get => _tbDados;
            set { _tbDados = value; OnPropertyChanged(); }
        }

        public ICommand ImportarCadastroCommand { get; private set; }
        public ICommand AbrirTelaPresencaCommand { get; private set; }

        public CadastroViewModel()
        {
            // Criamos a instância do serviço de importação que criamos no passo anterior
            _importacaoService = new ImportacaoService();
            ImportarCadastroCommand = new RelayCommand(ExecutarImportacao);
            AbrirTelaPresencaCommand = new RelayCommand(ExecutarAbrirTelaPresenca);
        }

        private void ExecutarImportacao()
        {
            TbDados = string.Empty;

            if (string.IsNullOrWhiteSpace(TurmaNome))
            {
                MessageBox.Show("Informe o nome da turma.");
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
                    TbDados = _importacaoService.Importacao(TurmaNome, dadosCsv);
                }
                catch (Exception ex)
                {
                    TbDados = $"Erro crítico: {ex.Message}\n";
                }
            }
        }

        private void ExecutarAbrirTelaPresenca()
        {
            try
            {
                // Instancia a sua janela de presença
                TelaPresenca telaPresenca = new TelaPresenca();

                // Exibe a janela na tela
                telaPresenca.Show();

                // (Opcional): Se você quiser fechar a tela de cadastro atual ao abrir a outra,
                // precisaremos passar a referência da janela atual, mas usar apenas o .Show()
                // já é o suficiente para abrir a nova por cima mantendo o padrão MVVM puro.
            }
            catch (Exception ex)
            {
                TbDados = $"Erro ao abrir a tela de presença: {ex.Message}\n";
            }
        }
    }
}
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.Modelos.Resultados;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.View.ANTIGO;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.ANTIGO
{
    public class CadastroViewModel : BaseViewModel
    {
        private readonly ImportacaoService _importacaoService;
        private string _turmaNome;
        private List<Aluno> _alunosImportados;

        public string TurmaNome
        {
            get => _turmaNome;
            set { _turmaNome = value; OnPropertyChanged(); }
        }

        public List<Aluno> AlunosImportados
        {
            get => _alunosImportados;
            set { _alunosImportados = value; OnPropertyChanged(); }
        }

        public ICommand ImportarCadastroCommand { get; private set; }
        public ICommand AbrirTelaPresencaCommand { get; private set; }

        public CadastroViewModel()
        {
            _importacaoService = new ImportacaoService();
            AlunosImportados = new List<Aluno>();
            ImportarCadastroCommand = new RelayCommand(ExecutarImportacao);
            AbrirTelaPresencaCommand = new RelayCommand(ExecutarAbrirTelaPresenca);
        }

        private void ExecutarImportacao()
        {
            AlunosImportados = new List<Aluno>();

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
                    ResultadoImportacaoCadastro resultado = _importacaoService.Importacao(TurmaNome, dadosCsv);

                    AlunosImportados = resultado.Alunos;
                    MessageBox.Show(resultado.Mensagem);
                }
                catch (Exception ex)
                {
                    AlunosImportados = new List<Aluno>();
                    MessageBox.Show("Erro critico: " + ex.Message);
                }
            }
        }

        private void ExecutarAbrirTelaPresenca()
        {
            try
            {
                TelaPresenca telaPresenca = new TelaPresenca();
                telaPresenca.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir a tela de presenca: " + ex.Message);
            }
        }
    }
}

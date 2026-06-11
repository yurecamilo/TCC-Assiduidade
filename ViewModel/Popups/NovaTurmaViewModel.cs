using System;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class NovaTurmaViewModel : BaseViewModel
    {
        private readonly TurmaService _turmaService;
        private string _nomeNovaTurma;

        public string NomeNovaTurma
        {
            get => _nomeNovaTurma;
            set { _nomeNovaTurma = value; OnPropertyChanged(); }
        }

        public ICommand SalvarTurmaCommand { get; private set; }

        // O ViewModel recebe uma Action para fechar a janela, 
        // evitando que o ViewModel conheça a View diretamente (mantendo o padrão MVVM)
        private readonly Action _fecharJanela;

        public NovaTurmaViewModel(Action fecharJanela)
        {
            _turmaService = new TurmaService();
            _fecharJanela = fecharJanela;
            SalvarTurmaCommand = new RelayCommand(ExecutarSalvar);
        }

        private async void ExecutarSalvar()
        {
            if (string.IsNullOrWhiteSpace(NomeNovaTurma))
            {
                MessageBox.Show("Por favor, informe o nome da turma.");
                return;
            }

            try
            {
                _turmaService.Adicionar(NomeNovaTurma.Trim());
                await DataCacheService.ForçarAtualizacaoAsync();

                MessageBox.Show("Turma cadastrada com sucesso!");

                _fecharJanela?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar turma: " + ex.Message);
            }
        }
    }
}
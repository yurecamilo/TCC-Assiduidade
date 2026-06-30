using System;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class EditarTurmaViewModel : BaseViewModel
    {
        private readonly TurmaService _turmaService;
        private string _novoNomeTurma;

        public int TurmaId { get; set; }
        public string NovoNomeTurma
        {
            get => _novoNomeTurma;
            set { _novoNomeTurma = value; OnPropertyChanged(); }
        }

        public ICommand SalvarTurmaCommand { get; private set; }
        public ICommand FecharJanelaCommand { get; private set; }

        public EditarTurmaViewModel(Action fecharJanela, TurmaExibicaoDTO turma)
        {
            TurmaId = turma.TurmaOriginal.Id;
            _novoNomeTurma = turma.Nome;
            _turmaService = new TurmaService();
            FecharJanelaCommand = new RelayCommand(fecharJanela);
            SalvarTurmaCommand = new RelayCommand(ExecutarSalvar);
        }

        private async void ExecutarSalvar()
        {
            if (string.IsNullOrWhiteSpace(NovoNomeTurma))
            {
                MessageBox.Show("Por favor, informe o nome da turma.");
                return;
            }

            try
            {
                _turmaService.Atualizar(TurmaId, NovoNomeTurma.Trim());
                await DataCacheService.ForçarAtualizacaoAsync();

                MessageBox.Show("Turma atualizada com sucesso!");

                FecharJanelaCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel salvar a turma. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }
    }
}

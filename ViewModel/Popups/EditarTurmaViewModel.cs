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
                PopupMensagem.Show("Por favor, informe o nome da turma.");
                return;
            }

            try
            {
                _turmaService.Atualizar(TurmaId, NovoNomeTurma.Trim());

                // A gravação no banco já aconteceu; uma falha ao atualizar o cache não
                // deve ser apresentada como erro de salvamento.
                string avisoAtualizacao = null;
                try
                {
                    await DataCacheService.ForçarAtualizacaoAsync();
                }
                catch (Exception)
                {
                    avisoAtualizacao = " A turma foi gravada, mas a lista não conseguiu atualizar. Reabra a tela de turmas para ver os dados atualizados.";
                }

                PopupMensagem.Show(
                    "Turma atualizada com sucesso!" + avisoAtualizacao,
                    "Turma salva",
                    MessageBoxButton.OK,
                    avisoAtualizacao != null ? MessageBoxImage.Warning : MessageBoxImage.Information);

                FecharJanelaCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel salvar a turma. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }
    }
}

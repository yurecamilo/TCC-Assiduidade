using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Popups
{
    public class EditarAlunoViewModel : BaseViewModel
    {
        private readonly AlunoService _alunoService;

        private string _matricula;
        private string _nome;
        private string _email;

        public string Matricula
        {
            get => _matricula;
            set { _matricula = value; OnPropertyChanged(); }
        }

        public string Nome
        {
            get => _nome;
            set { _nome = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }


        public ICommand SalvarAlunoCommand { get; private set; }
        public ICommand FecharJanelaCommand { get; private set; }

        public EditarAlunoViewModel(Action fecharJanela, AlunoExibicaoDTO aluno)
        {
            _alunoService = new AlunoService();
            FecharJanelaCommand = new RelayCommand(fecharJanela);
            SalvarAlunoCommand = new RelayCommand(ExecutarSalvar);

            Matricula = aluno.Matricula;
            Nome = aluno.Nome;
            Email = aluno.Email;
        }

        private async void ExecutarSalvar()
        {
            try
            {
                var aluno = new Aluno
                {
                    Matricula = Matricula,
                    Nome = Nome,
                    Email = Email
                };

                _alunoService.Atualizar(aluno);

                // A gravação no banco já aconteceu; uma falha ao atualizar o cache não
                // deve ser apresentada como erro de edição.
                string avisoAtualizacao = null;
                try
                {
                    await DataCacheService.ForçarAtualizacaoAsync();
                }
                catch (Exception)
                {
                    avisoAtualizacao = " O aluno foi gravado, mas a lista não conseguiu atualizar. Reabra a tela de alunos para ver os dados atualizados.";
                }

                PopupMensagem.Show(
                    "Aluno editado com sucesso!" + avisoAtualizacao,
                    "Aluno salvo",
                    MessageBoxButton.OK,
                    avisoAtualizacao != null ? MessageBoxImage.Warning : MessageBoxImage.Information);
                FecharJanelaCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel editar o aluno. Verifique os dados informados e tente novamente.", ex);
            }
        }
    }
}

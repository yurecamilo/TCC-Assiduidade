using System.Windows.Input;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class InicioViewModel : BaseViewModel
    {
        public ICommand ImportarCadastroCommand { get; private set; }
        public ICommand ImportarChamadaCommand { get; private set; }
        public ICommand CadastrarTurmaCommand { get; private set; }
        public ICommand CadastrarAlunoCommand { get; private set; }

        public InicioViewModel()
        {
            ImportarCadastroCommand = new RelayCommand(ExecutarImportarCadastro);
            ImportarChamadaCommand = new RelayCommand(ExecutarImportarChamada);
            CadastrarTurmaCommand = new RelayCommand(ExecutarCadastrarTurma);
            CadastrarAlunoCommand = new RelayCommand(ExecutarCadastrarAluno);
        }

        private void ExecutarImportarCadastro() => WindowService.AbrirImportarCadastro();
        private void ExecutarImportarChamada(object obj) => WindowService.AbrirImportarPresenca();
        private void ExecutarCadastrarTurma(object obj) => WindowService.AbrirNovaTurma();
        private void ExecutarCadastrarAluno(object obj) => WindowService.AbrirNovoAluno();
    }
}

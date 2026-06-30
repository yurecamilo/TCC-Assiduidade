using System.Windows;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Lógica interna para EditarAluno.xaml
    /// </summary>
    public partial class EditarAluno : Window
    {
        private readonly EditarAlunoViewModel _viewmodel;
        public EditarAluno(AlunoExibicaoDTO aluno)
        {
            InitializeComponent();
            _viewmodel = new EditarAlunoViewModel(this.Close, aluno); 
            this.DataContext = _viewmodel;
        }
    }
}

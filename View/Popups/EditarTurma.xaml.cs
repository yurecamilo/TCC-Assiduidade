using System.Windows;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Lógica interna para EditarTurma.xaml
    /// </summary>
    public partial class EditarTurma : Window
    {
        private readonly EditarTurmaViewModel _viewModel;
        public EditarTurma(TurmaExibicaoDTO turma)
        {
            InitializeComponent();
            _viewModel = new EditarTurmaViewModel(this.Close, turma);
            DataContext = _viewModel;
        }
    }
}

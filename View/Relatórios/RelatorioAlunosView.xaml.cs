using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.View.Relatórios
{
    /// <summary>
    /// Interação lógica para RelatorioAlunosView.xam
    /// </summary>
    public partial class RelatorioAlunosView : UserControl
    {
        private readonly RelatorioAlunosViewModel _viewModel;
        public RelatorioAlunosView(RelatoriosViewModel pai)
        {
            InitializeComponent();
            _viewModel = new RelatorioAlunosViewModel(pai);
            DataContext = _viewModel;
        }
    }
}

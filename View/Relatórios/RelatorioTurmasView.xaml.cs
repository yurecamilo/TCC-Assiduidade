using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para RelatorioTurmasView.xam
    /// </summary>
    public partial class RelatorioTurmasView : UserControl
    {
        private readonly RelatorioTurmasViewModel _viewModel;
        public RelatorioTurmasView(RelatoriosViewModel pai)
        {
            InitializeComponent();
            _viewModel = new RelatorioTurmasViewModel(pai);
            DataContext = _viewModel;
        }
    }
}

using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para RelatoriosView.xam
    /// </summary>
    public partial class RelatoriosView : UserControl
    {
        private readonly RelatoriosViewModel _viewModel;
        public RelatoriosView()
        {
            InitializeComponent();
            _viewModel = new RelatoriosViewModel();
            DataContext = _viewModel;
        }
    }
}

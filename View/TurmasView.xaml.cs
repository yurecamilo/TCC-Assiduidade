using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para TurmasView.xam
    /// </summary>
    public partial class TurmasView : UserControl
    {
        private readonly TurmasViewModel _viewModel;
        public TurmasView()
        {
            InitializeComponent();
            _viewModel = new TurmasViewModel();
            this.DataContext = _viewModel;
        }
    }
}
